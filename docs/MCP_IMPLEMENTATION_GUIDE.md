# MCP実装ガイドライン

本ドキュメントは、Unity Editor MCPサーバー (`jp.xeon.unity-mcp-server`) の開発者向けリファレンスである。MCP (Model Context Protocol) の仕様に基づき、本プロジェクトにおける実装方針と具体的な実装パターンを記載する。

対象MCP仕様バージョン: **2025-03-26**

---

## 目次

1. [MCP概要](#1-mcp概要)
2. [プロトコルライフサイクル](#2-プロトコルライフサイクル)
3. [トランスポート層](#3-トランスポート層)
4. [JSON-RPC 2.0メッセージフォーマット](#4-json-rpc-20メッセージフォーマット)
5. [MCPメソッド一覧](#5-mcpメソッド一覧)
6. [ツールの実装ガイド](#6-ツールの実装ガイド)
7. [エラーハンドリング方針](#7-エラーハンドリング方針)
8. [Claude Desktop連携設定](#8-claude-desktop連携設定)
9. [MCP Inspectorでの検証方法](#9-mcp-inspectorでの検証方法)
10. [参考リンク](#10-参考リンク)

---

## 1. MCP概要

### MCPとは

MCP (Model Context Protocol) は、LLM（大規模言語モデル）アプリケーションと外部ツール・データソースを接続するための標準プロトコルである。Anthropic社が策定し、オープンスタンダードとして公開されている。

MCPの通信基盤は **JSON-RPC 2.0** であり、すべてのメッセージはJSON-RPC 2.0の形式でやり取りされる。

### クライアント/サーバーアーキテクチャ

MCPはクライアント/サーバーモデルを採用している。

```
┌─────────────────┐         JSON-RPC 2.0         ┌─────────────────┐
│   MCPクライアント   │ ◄─────────────────────────► │   MCPサーバー     │
│                 │    (Streamable HTTP等)       │                 │
│  - Claude Desktop│                              │  - 本プロジェクト   │
│  - Cursor       │                              │  (Unity Editor内) │
│  - その他AIツール  │                              │                 │
└─────────────────┘                              └─────────────────┘
```

- **MCPクライアント**: LLMアプリケーション側。ツール一覧の取得やツール実行のリクエストを送信する。
- **MCPサーバー**: ツールやリソースを提供する側。本プロジェクトではUnity Editor内でHTTPサーバーとして動作する。

### 主要なMCPクライアント

| クライアント | 開発元 | 備考 |
|---|---|---|
| Claude Desktop | Anthropic | MCPの主要クライアント。`claude_desktop_config.json` で接続先を設定する |
| Cursor | Cursor, Inc. | AIコードエディタ。MCP経由で外部ツールと連携可能 |
| Claude Code | Anthropic | CLI環境のAIコーディングツール |
| Windsurf | Codeium | AI搭載エディタ。MCP対応 |

本サーバーは標準MCPプロトコルに準拠しているため、上記すべてのクライアントから接続可能である。

---

## 2. プロトコルライフサイクル

### 3段階の初期化ハンドシェイク

MCPセッションの確立には3段階のハンドシェイクが必要である。

```
MCPクライアント                          MCPサーバー
     │                                      │
     │  1. initialize (request)             │
     │ ────────────────────────────────────► │
     │                                      │  サーバーはcapabilitiesを返す
     │  initialize (response)               │
     │ ◄──────────────────────────────────── │
     │                                      │
     │  2. notifications/initialized        │
     │ ────────────────────────────────────► │  クライアントが初期化完了を通知
     │                                      │  （notification: レスポンスなし）
     │                                      │
     │  ═══════ セッション確立 (Ready) ═══════ │
     │                                      │
     │  3. tools/list, tools/call 等        │
     │ ◄───────────────────────────────────► │  通常のMCPメソッドが利用可能
     │                                      │
```

### セッション状態遷移

```
  ┌──────────────┐    initialize    ┌──────────────┐   notifications/   ┌──────────────┐
  │Uninitialized │ ──────────────► │ Initializing │ ──initialized───► │    Ready     │
  │              │    (request)    │              │   (notification)  │              │
  └──────────────┘                 └──────────────┘                   └──────────────┘
        │                                                                    │
        │  許可: initialize, ping                                            │  許可: 全メソッド
        │                                                                    │
```

### 各フェーズで許可されるメソッド

| フェーズ | 許可されるメソッド |
|---|---|
| Uninitialized | `initialize`, `ping` のみ |
| Initializing | `ping` のみ（`notifications/initialized` 受信待ち） |
| Ready | すべてのメソッド (`tools/list`, `tools/call`, `ping` 等) |

初期化完了前に `tools/list` や `tools/call` を受信した場合は、エラーコード `-32600` (Invalid Request) で拒否する。

### initialize リクエスト/レスポンスの例

**クライアント → サーバー (リクエスト):**

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "initialize",
  "params": {
    "protocolVersion": "2025-03-26",
    "capabilities": {},
    "clientInfo": {
      "name": "claude-desktop",
      "version": "1.0.0"
    }
  }
}
```

**サーバー → クライアント (レスポンス):**

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "protocolVersion": "2025-03-26",
    "capabilities": {
      "tools": {
        "listChanged": false
      }
    },
    "serverInfo": {
      "name": "unity-mcp-server",
      "version": "1.0.0"
    }
  }
}
```

`capabilities.tools.listChanged` はツール一覧が動的に変更されるかどうかを示す。`false` の場合、サーバーは `notifications/tools/list_changed` 通知を送信しない。

### notifications/initialized の例

**クライアント → サーバー (通知):**

```json
{
  "jsonrpc": "2.0",
  "method": "notifications/initialized"
}
```

通知 (notification) は `id` フィールドを持たない。サーバーはこのメッセージに対してレスポンスを返してはならない。

---

## 3. トランスポート層

### なぜStreamable HTTPを採用したか

MCPは以下の2つのトランスポートを標準として定義している。

| トランスポート | 通信方式 | 適用場面 |
|---|---|---|
| stdio | 標準入出力 (stdin/stdout) | ローカルプロセスとして起動されるサーバー |
| Streamable HTTP | HTTP + SSE | リモート接続、またはstdioが使えない環境 |

**本プロジェクトでは Streamable HTTP を採用している。** 理由は以下の通り。

- Unity Editorはアプリケーションプロセスとして常時起動しており、MCPクライアントから子プロセスとして起動することはできない
- Unity Editorの標準入出力 (stdin/stdout) はEditorのログシステムが占有しており、JSON-RPC通信に使用できない
- Unity Editor内で `HttpListener` を使ったHTTPサーバーを起動することで、外部からのHTTPリクエストを受け付けられる

### Streamable HTTP仕様の要点

Streamable HTTPでは、サーバーは **単一のエンドポイント** (例: `http://localhost:7000/mcp/`) を公開し、HTTPメソッドによって動作を切り替える。

#### HTTPメソッドごとの役割

| HTTPメソッド | 用途 | 説明 |
|---|---|---|
| POST | JSON-RPCメッセージ送信 | クライアントからサーバーへのリクエスト・通知の送信 |
| GET | SSEストリーム開始 | サーバーからクライアントへの通知配信用ストリーム |
| DELETE | セッション終了 | 明示的なセッション切断 |

#### POST リクエストの処理フロー

```
クライアント                                    サーバー
     │                                            │
     │  POST /mcp/                                │
     │  Content-Type: application/json             │
     │  Accept: application/json, text/event-stream│
     │  Mcp-Session-Id: {session-id}              │
     │  Body: {"jsonrpc":"2.0", ...}              │
     │ ──────────────────────────────────────────► │
     │                                            │  JSON-RPCメッセージを処理
     │  200 OK                                    │
     │  Content-Type: application/json             │
     │  Body: {"jsonrpc":"2.0", "id":1, ...}      │
     │ ◄────────────────────────────────────────── │
     │                                            │
```

#### Acceptヘッダーによるレスポンス形式の切り替え

クライアントが送信する `Accept` ヘッダーによって、サーバーのレスポンス形式が決まる。

| Acceptヘッダー | レスポンス形式 | Content-Type |
|---|---|---|
| `application/json` | 単一のJSONレスポンス | `application/json` |
| `text/event-stream` | SSEストリーム | `text/event-stream` |
| `application/json, text/event-stream` | サーバーが選択 | いずれか |

通知 (`notifications/initialized` 等) のPOSTに対してはレスポンスボディなしの `202 Accepted` を返す。

#### Mcp-Session-Id によるセッション管理

- サーバーは `initialize` レスポンスの際に `Mcp-Session-Id` ヘッダーを発行する
- クライアントは後続のすべてのリクエストに `Mcp-Session-Id` ヘッダーを含める
- サーバーは不正なセッションIDのリクエストを `400 Bad Request` で拒否する

```
初回:
  クライアント → POST initialize
  サーバー ← 200 OK + Mcp-Session-Id: abc123

後続:
  クライアント → POST tools/list + Mcp-Session-Id: abc123
  サーバー ← 200 OK
```

#### SSEのイベントIDと再接続

SSEストリームでは、各イベントに `id` フィールドを付与する。クライアントが切断された場合、`Last-Event-ID` ヘッダーを付けてGETリクエストを再送することで、未受信のイベントから再開できる。

```
SSEイベント形式:
id: event-001
data: {"jsonrpc":"2.0","method":"notifications/tools/list_changed"}

id: event-002
data: {"jsonrpc":"2.0","method":"notifications/progress","params":{...}}
```

再接続時:
```
GET /mcp/
Accept: text/event-stream
Mcp-Session-Id: abc123
Last-Event-ID: event-001
```

---

## 4. JSON-RPC 2.0メッセージフォーマット

MCPのすべての通信は JSON-RPC 2.0 形式に従う。

### リクエスト形式

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "check_status",
    "arguments": {}
  }
}
```

| フィールド | 型 | 必須 | 説明 |
|---|---|---|---|
| `jsonrpc` | string | はい | 常に `"2.0"` |
| `id` | string \| number | はい | リクエスト識別子。レスポンスに同じ値を含める |
| `method` | string | はい | 呼び出すメソッド名 |
| `params` | object | いいえ | メソッドのパラメータ |

### レスポンス形式 (成功時)

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "content": [
      { "type": "text", "text": "Server is running" }
    ],
    "isError": false
  }
}
```

| フィールド | 型 | 必須 | 説明 |
|---|---|---|---|
| `jsonrpc` | string | はい | 常に `"2.0"` |
| `id` | string \| number | はい | 対応するリクエストの `id` と一致させる |
| `result` | object | はい | 成功時の結果 (`error` と排他) |

### レスポンス形式 (エラー時)

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "error": {
    "code": -32601,
    "message": "Method not found"
  }
}
```

| フィールド | 型 | 必須 | 説明 |
|---|---|---|---|
| `jsonrpc` | string | はい | 常に `"2.0"` |
| `id` | string \| number | はい | 対応するリクエストの `id` と一致させる |
| `error` | object | はい | エラー情報 (`result` と排他) |
| `error.code` | number | はい | エラーコード (下表参照) |
| `error.message` | string | はい | エラーメッセージ |
| `error.data` | any | いいえ | 追加のエラー情報 |

### 通知形式

```json
{
  "jsonrpc": "2.0",
  "method": "notifications/initialized"
}
```

通知はリクエストとは異なり、以下の特徴を持つ。

- `id` フィールドを **含まない**
- サーバーはレスポンスを **返してはならない**
- 送信側は配信の成否を確認できない（fire-and-forget）

### JSON-RPC 2.0エラーコード一覧

| コード | 名前 | 意味 | 使い分け |
|---|---|---|---|
| `-32700` | Parse error | JSON解析失敗 | リクエストボディが不正なJSONの場合 |
| `-32600` | Invalid Request | リクエスト形式不正 | `jsonrpc`、`method` フィールドが欠けている場合。初期化前のリクエスト拒否にも使用 |
| `-32601` | Method not found | 未知のメソッド | サーバーが実装していないメソッドが指定された場合 |
| `-32602` | Invalid params | パラメータ不正 | 必須パラメータの欠落、型の不一致 |
| `-32603` | Internal error | 内部エラー | サーバー内部の予期しないエラー |

---

## 5. MCPメソッド一覧

### メソッド一覧テーブル

| メソッド | 種別 | 方向 | 説明 |
|---|---|---|---|
| `initialize` | request | クライアント→サーバー | セッション開始。capabilities交渉を行う |
| `notifications/initialized` | notification | クライアント→サーバー | クライアントの初期化完了通知 |
| `ping` | request | 双方向 | ヘルスチェック。初期化前でも使用可能 |
| `tools/list` | request | クライアント→サーバー | サーバーが提供するツール一覧を取得 |
| `tools/call` | request | クライアント→サーバー | 指定したツールを実行 |

### 各メソッドの詳細

#### initialize

セッション開始時にクライアントが最初に送信するリクエスト。プロトコルバージョンの合意とcapabilities交渉を行う。

**リクエスト:**

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "initialize",
  "params": {
    "protocolVersion": "2025-03-26",
    "capabilities": {},
    "clientInfo": {
      "name": "claude-desktop",
      "version": "1.0.0"
    }
  }
}
```

**レスポンス:**

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "protocolVersion": "2025-03-26",
    "capabilities": {
      "tools": {
        "listChanged": false
      }
    },
    "serverInfo": {
      "name": "unity-mcp-server",
      "version": "1.0.0"
    }
  }
}
```

#### notifications/initialized

クライアントが `initialize` レスポンスを処理した後に送信する通知。サーバーはこの通知を受けてセッション状態を `Ready` に遷移させる。

**通知:**

```json
{
  "jsonrpc": "2.0",
  "method": "notifications/initialized"
}
```

レスポンスなし (HTTP `202 Accepted`)。

#### ping

ヘルスチェック用メソッド。初期化フェーズに関係なく、いつでも使用可能である。

**リクエスト:**

```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "method": "ping"
}
```

**レスポンス:**

```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "result": {}
}
```

#### tools/list

サーバーが提供するツールの一覧を取得する。各ツールには名前、説明、入力スキーマ (JSON Schema) が含まれる。

**リクエスト:**

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/list",
  "params": {}
}
```

**レスポンス:**

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "tools": [
      {
        "name": "check_status",
        "description": "Check if the MCP server is running",
        "inputSchema": {
          "type": "object",
          "properties": {},
          "required": []
        }
      },
      {
        "name": "run_editmode_tests",
        "description": "Run Test Runner in EditMode",
        "inputSchema": {
          "type": "object",
          "properties": {},
          "required": []
        }
      },
      {
        "name": "run_playmode_tests",
        "description": "Run Test Runner in PlayMode",
        "inputSchema": {
          "type": "object",
          "properties": {},
          "required": []
        }
      }
    ]
  }
}
```

#### tools/call

指定したツールを実行する。ツール名と引数を送信し、実行結果を受け取る。

**リクエスト:**

```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "tools/call",
  "params": {
    "name": "check_status",
    "arguments": {}
  }
}
```

**レスポンス (成功時):**

```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "Server is running"
      }
    ],
    "isError": false
  }
}
```

**レスポンス (ツール実行エラー時):**

```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "Error: Unknown tool: invalid_tool"
      }
    ],
    "isError": true
  }
}
```

ツール実行エラーは JSON-RPC の `error` フィールドではなく、`result.isError: true` で返す点に注意。詳細は [7. エラーハンドリング方針](#7-エラーハンドリング方針) を参照。

---

## 6. ツールの実装ガイド

### IMcpToolインターフェース

すべてのツールは `IMcpTool` インターフェースを実装する。

```csharp
// ファイル: Editor/MCP/IMcpTool.cs
namespace UnityMcp
{
    public interface IMcpTool
    {
        string Name { get; }
        string Description { get; }
        string InputSchema { get; }
        Task<object> Execute(string arguments);
    }
}
```

| プロパティ/メソッド | 型 | 説明 |
|---|---|---|
| `Name` | `string` | ツールの一意識別名。`tools/call` の `params.name` に対応する |
| `Description` | `string` | ツールの説明文。LLMがツール選択の判断に使用する |
| `InputSchema` | `string` | ツールの入力パラメータを定義するJSON Schema文字列 |
| `Execute` | `Task<object>` | ツールの実行ロジック。引数はJSON文字列、戻り値は結果オブジェクト |

### InputSchema (JSON Schema) の書き方

`InputSchema` プロパティは、ツールの入力パラメータをJSON Schema形式の文字列で返す。MCPクライアント（LLM）はこのスキーマを参照して正しいパラメータを構築する。

#### パラメータなしの場合

```csharp
public string InputSchema => @"{
  ""type"": ""object"",
  ""properties"": {},
  ""required"": []
}";
```

#### パラメータありの場合

```csharp
public string InputSchema => @"{
  ""type"": ""object"",
  ""properties"": {
    ""filePath"": {
      ""type"": ""string"",
      ""description"": ""対象ファイルのパス""
    },
    ""lineNumber"": {
      ""type"": ""integer"",
      ""description"": ""行番号（1始まり）""
    },
    ""dryRun"": {
      ""type"": ""boolean"",
      ""description"": ""trueの場合、実行せずプレビューのみ行う"",
      ""default"": false
    }
  },
  ""required"": [""filePath""]
}";
```

JSON Schemaの主要なプロパティ型:

| 型 | JSON Schema type | C#での受け取り型 |
|---|---|---|
| 文字列 | `"string"` | `string` |
| 整数 | `"integer"` | `int`, `long` |
| 数値 | `"number"` | `float`, `double` |
| 真偽値 | `"boolean"` | `bool` |
| 配列 | `"array"` | `List<T>`, `T[]` |
| オブジェクト | `"object"` | クラス, `Dictionary` |

### CallToolResultの返し方

`tools/call` のレスポンスには `content` 配列と `isError` フラグが含まれる。

#### 成功時のレスポンス構造

```json
{
  "content": [
    {
      "type": "text",
      "text": "実行結果のテキスト"
    }
  ],
  "isError": false
}
```

#### エラー時のレスポンス構造

```json
{
  "content": [
    {
      "type": "text",
      "text": "Error: ファイルが見つかりません: /path/to/file.cs"
    }
  ],
  "isError": true
}
```

`content` 配列の要素は `TextContent` 型を使用する。

```csharp
// TextContent の構造
{
  "type": "text",  // 常に "text"
  "text": "..."    // テキスト内容
}
```

複雑な結果（テスト結果等）はJSON文字列としてシリアライズし、`text` フィールドに格納する。

```csharp
// テスト結果の返却例
var summary = new TestResultSummary(result);
var json = JsonUtility.ToJson(summary);
// text: "{\"passCount\":5,\"failedCount\":0,\"skippedCount\":0,\"failedResults\":[]}"
```

### カスタムツールの登録方法

ツールの登録は `McpToolRouter` を通じて行う。2つの登録方法がある。

#### 方法1: IMcpTool実装クラスを登録

```csharp
// IMcpToolを実装したクラスを作成
public class MyCustomTool : IMcpTool
{
    public string Name => "my_custom_tool";
    public string Description => "カスタムツールの説明";
    public string InputSchema => @"{
        ""type"": ""object"",
        ""properties"": {
            ""message"": {
                ""type"": ""string"",
                ""description"": ""表示するメッセージ""
            }
        },
        ""required"": [""message""]
    }";

    public async Task<object> Execute(string arguments)
    {
        // argumentsはJSON文字列として渡される
        // パース処理と実行ロジックをここに記述
        return "実行結果";
    }
}

// 登録
var tool = new MyCustomTool();
McpToolRouter.TryRegisterTool(tool);
```

#### 方法2: デリゲートで簡易登録

パラメータなしのシンプルなツールは `CommonMcpTool` を経由してデリゲートで登録できる。

```csharp
// デリゲートで登録（CommonMcpToolが内部で生成される）
McpToolRouter.TryRegisterTool("check_status", CheckStatus);

private static Task<object> CheckStatus(string _)
{
    return Task.FromResult<object>(true);
}
```

`TryRegisterTool` は同名のツールが既に登録されている場合に `false` を返す（上書きしない）。

### ツール実装例

#### 例1: パラメータなしのツール

```csharp
public class CheckStatusTool : IMcpTool
{
    public string Name => "check_status";
    public string Description => "Check if the MCP server is running";
    public string InputSchema => @"{
        ""type"": ""object"",
        ""properties"": {},
        ""required"": []
    }";

    public Task<object> Execute(string arguments)
    {
        return Task.FromResult<object>(true);
    }
}
```

#### 例2: パラメータありのツール

```csharp
[Serializable]
public class ExecuteMenuItemArgs
{
    public string menuPath;
}

public class ExecuteMenuItemTool : IMcpTool
{
    public string Name => "execute_menu_item";
    public string Description => "Unity Editorのメニューアイテムを実行する";
    public string InputSchema => @"{
        ""type"": ""object"",
        ""properties"": {
            ""menuPath"": {
                ""type"": ""string"",
                ""description"": ""メニューパス（例: Assets/Create/Folder）""
            }
        },
        ""required"": [""menuPath""]
    }";

    public Task<object> Execute(string arguments)
    {
        var args = JsonUtility.FromJson<ExecuteMenuItemArgs>(arguments);

        if (string.IsNullOrEmpty(args.menuPath))
            throw new ArgumentException("menuPath is required");

        var result = EditorApplication.ExecuteMenuItem(args.menuPath);
        if (!result)
            throw new InvalidOperationException($"Menu item not found: {args.menuPath}");

        return Task.FromResult<object>($"Executed: {args.menuPath}");
    }
}
```

### Unity APIとメインスレッドの制約

Unity Editorの多くのAPIはメインスレッドでのみ実行可能である。HTTPリクエストはバックグラウンドスレッドで受信されるため、`McpDispatcher` を経由してメインスレッドで実行する必要がある。

```
バックグラウンドスレッド                  メインスレッド
(HTTPリスナー)                          (EditorApplication.update)
      │                                       │
      │  McpDispatcher.Enqueue(action)        │
      │ ─────────────────────────────────────► │
      │                                       │  action() を実行
      │                                       │  (Unity API呼び出し可能)
```

ツールの `Execute` メソッドはメインスレッド上で実行されるため、ツール開発者がスレッドを意識する必要は通常ない。

---

## 7. エラーハンドリング方針

### プロトコルエラー vs ツール実行エラー

MCPでは2種類のエラーを明確に区別する。これはLLMが自己修正するために重要な設計判断である。

#### プロトコルエラー

JSON-RPCレベルのエラー。クライアント/サーバー間の通信上の問題を示す。

**特徴:**
- JSON-RPCの `error` フィールドで返す
- LLMは通常リトライしない
- プロトコル違反、未知のメソッド、不正なJSONなどが該当

**レスポンス形式:**

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "error": {
    "code": -32601,
    "message": "Method not found: unknown_method"
  }
}
```

#### ツール実行エラー

ツール自体は正常に呼び出されたが、実行中にエラーが発生した場合。

**特徴:**
- `result` フィールドの中で `isError: true` として返す
- LLMがエラー内容を読み取り、パラメータを修正して再試行できる
- ファイルが見つからない、引数が不正、タイムアウトなどが該当

**レスポンス形式:**

```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "Error: File not found: /path/to/missing/file.cs"
      }
    ],
    "isError": true
  }
}
```

### なぜ区別するのか

```
プロトコルエラーの場合:
  LLM: 「tools/callを呼んだが、-32601が返った。このメソッドは存在しないようだ」
  → リトライしても無意味。別の手段を検討する。

ツール実行エラーの場合:
  LLM: 「run_editmode_testsを呼んだが、isError=trueで"Timeout"と返った」
  → パラメータを変えて再試行、またはユーザーに状況を説明する。
```

### エラーコードの使い分け

| 状況 | エラー種別 | コード/フラグ |
|---|---|---|
| リクエストボディが不正なJSON | プロトコルエラー | `-32700` (Parse error) |
| `jsonrpc` や `method` が欠落 | プロトコルエラー | `-32600` (Invalid Request) |
| 初期化前に `tools/call` を受信 | プロトコルエラー | `-32600` (Invalid Request) |
| 存在しないメソッド名 | プロトコルエラー | `-32601` (Method not found) |
| `tools/call` の `params.name` が欠落 | プロトコルエラー | `-32602` (Invalid params) |
| サーバー内部の予期しない例外 | プロトコルエラー | `-32603` (Internal error) |
| ツール実行中のファイル未発見 | ツール実行エラー | `isError: true` |
| ツール実行中のタイムアウト | ツール実行エラー | `isError: true` |
| ツールに渡された引数値の不正 | ツール実行エラー | `isError: true` |
| 指定されたツール名が未登録 | ツール実行エラー | `isError: true` |

---

## 8. Claude Desktop連携設定

### claude_desktop_config.json の設定

Claude DesktopからUnity Editor MCPサーバーに接続するには、設定ファイルにStreamable HTTP接続先を追記する。

**設定ファイルの場所:**

| OS | パス |
|---|---|
| macOS | `~/Library/Application Support/Claude/claude_desktop_config.json` |
| Windows | `%APPDATA%\Claude\claude_desktop_config.json` |

**設定内容:**

```json
{
  "mcpServers": {
    "unity": {
      "url": "http://localhost:7000/mcp/"
    }
  }
}
```

`url` フィールドにStreamable HTTPエンドポイントを指定する。stdioトランスポートの `command` / `args` フィールドは不要である。

### 接続確認手順

1. Unity Editorを起動し、MCPサーバーが起動していることを確認する（Consoleに `[MCP] Server started on http://localhost:7000/mcp/` と表示される）
2. Claude Desktopを起動（または再起動）する
3. Claude Desktopのチャット画面で、ツールアイコンにUnity MCPサーバーのツールが表示されることを確認する

### ポート番号の変更

デフォルトのポート番号は `7000` である。変更するには以下の手順を実施する。

1. Unity Editorで `Assets/Create/MCPServerSetting` からScriptableObjectを作成する
2. 作成した `McpServerSetting` アセットの `Port` フィールドを編集する
3. `Tools/Restart MCP Server` メニューでサーバーを再起動する
4. `claude_desktop_config.json` の `url` も合わせて変更する

---

## 9. MCP Inspectorでの検証方法

### MCP Inspectorとは

[MCP Inspector](https://github.com/modelcontextprotocol/inspector) は、MCPサーバーの動作を対話的にテスト・検証するための公式開発ツールである。ブラウザベースのUIを提供し、MCPプロトコルの各メソッドを手動で実行して結果を確認できる。

### インストールと起動

```bash
npx @modelcontextprotocol/inspector
```

ブラウザが自動的に開き、Inspector UIが表示される。

### Streamable HTTPサーバーへの接続

1. Inspector UIの接続設定で、Transport Typeに **Streamable HTTP** を選択する
2. URLに `http://localhost:7000/mcp/` を入力する
3. 「Connect」をクリックする

### 検証手順

Inspector UIでは以下の検証を順に実施できる。

**1. 接続テスト:**
- 「Connect」ボタンをクリックし、`initialize` ハンドシェイクが成功することを確認する
- サーバーのcapabilitiesとserverInfoが表示されることを確認する

**2. ツール一覧の確認:**
- 「Tools」タブを開き、登録されているツール一覧が表示されることを確認する
- 各ツールの `name`、`description`、`inputSchema` が正しいことを確認する

**3. ツール実行テスト:**
- ツール一覧から任意のツールを選択する
- パラメータを入力（必要な場合）し、「Run」をクリックする
- レスポンスの `content` と `isError` を確認する

**4. エラーハンドリングの確認:**
- 存在しないツール名を指定して実行し、適切なエラーが返ることを確認する
- 不正なパラメータを送信し、エラーハンドリングが正しく動作することを確認する

### curlによる手動検証

Inspector以外にも、`curl` コマンドで直接プロトコルレベルの検証が可能である。

```bash
# 1. initialize
curl -X POST http://localhost:7000/mcp/ \
  -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "initialize",
    "params": {
      "protocolVersion": "2025-03-26",
      "capabilities": {},
      "clientInfo": {"name": "curl-test", "version": "1.0"}
    }
  }'

# レスポンスのMcp-Session-Idヘッダーを記録する

# 2. notifications/initialized
curl -X POST http://localhost:7000/mcp/ \
  -H "Content-Type: application/json" \
  -H "Mcp-Session-Id: {上記で取得したセッションID}" \
  -d '{"jsonrpc": "2.0", "method": "notifications/initialized"}'

# 3. tools/list
curl -X POST http://localhost:7000/mcp/ \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -H "Mcp-Session-Id: {セッションID}" \
  -d '{"jsonrpc": "2.0", "id": 2, "method": "tools/list", "params": {}}'

# 4. tools/call
curl -X POST http://localhost:7000/mcp/ \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -H "Mcp-Session-Id: {セッションID}" \
  -d '{
    "jsonrpc": "2.0",
    "id": 3,
    "method": "tools/call",
    "params": {"name": "check_status", "arguments": {}}
  }'

# 5. ping
curl -X POST http://localhost:7000/mcp/ \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -H "Mcp-Session-Id: {セッションID}" \
  -d '{"jsonrpc": "2.0", "id": 4, "method": "ping"}'

# 6. セッション終了
curl -X DELETE http://localhost:7000/mcp/ \
  -H "Mcp-Session-Id: {セッションID}"
```

---

## 10. 参考リンク

| リソース | URL |
|---|---|
| MCP Specification (2025-03-26) | https://modelcontextprotocol.io/specification/2025-03-26 |
| MCP Specification - Lifecycle | https://modelcontextprotocol.io/specification/2025-03-26/basic/lifecycle |
| MCP Specification - Transports | https://spec.modelcontextprotocol.io/specification/2025-03-26/basic/transports/ |
| MCP Specification - Tools | https://modelcontextprotocol.io/specification/2025-06-18/server/tools |
| JSON-RPC 2.0 Specification | https://www.jsonrpc.org/specification |
| MCP Inspector (GitHub) | https://github.com/modelcontextprotocol/inspector |
| 本プロジェクト修正ロードマップ | [docs/MCP_COMPLIANCE_ROADMAP.md](./MCP_COMPLIANCE_ROADMAP.md) |
