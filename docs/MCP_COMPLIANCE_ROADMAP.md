# MCP仕様準拠 修正ロードマップ

## 現状の課題

現在の実装は独自HTTPプロトコルであり、MCP (Model Context Protocol) 仕様に準拠していない。
Claude Desktop、Cursor等のMCPクライアントから接続できない状態にある。

### 現在のアーキテクチャ

```
MCPクライアント ──(HTTP POST)──► http://localhost:7000/mcp/
                                  独自JSON形式で通信
```

### 現在のプロトコル（非準拠）

```jsonc
// リクエスト（独自形式）
{"tool": "check_status", "arguments": ""}

// レスポンス（独自形式）
{"ok": true, "result": "true", "error": ""}
```

### MCP仕様が要求するプロトコル

```jsonc
// リクエスト（JSON-RPC 2.0）
{"jsonrpc": "2.0", "id": 1, "method": "tools/call", "params": {"name": "check_status", "arguments": {}}}

// レスポンス（JSON-RPC 2.0）
{"jsonrpc": "2.0", "id": 1, "result": {"content": [{"type": "text", "text": "Server is running"}], "isError": false}}
```

---

## Phase 1: 基盤整備 — IMcpToolインターフェース修正とJSON処理の改善

**目的**: コンパイルを通し、後続Phaseで必要なデータ構造を整備する

### 1.1 IMcpToolインターフェース定義ファイルの作成

`IMcpTool`インターフェースの定義ファイルが存在しない。`CommonMcpTool.cs`、`RunTests.cs`が参照している。

**作業内容**:
- `Editor/MCP/IMcpTool.cs` を新規作成
- `inputSchema`プロパティを追加（Phase 4の`tools/list`で必要）

```csharp
namespace UnityMcp
{
    public interface IMcpTool
    {
        string Name { get; }
        string Description { get; }
        string InputSchema { get; } // JSON Schema文字列
        Task<object> Execute(string arguments);
    }
}
```

**影響ファイル**:
- `Editor/MCP/CommonMcpTool.cs` — `InputSchema`プロパティ追加
- `Editor/MCP/RunTests.cs` — `InputSchema`プロパティ追加

### 1.2 JSON-RPC 2.0 データモデルの作成

`McpProtocol.cs`の`McpRequest`/`McpResponse`は独自形式のため、JSON-RPC 2.0用のデータモデルを新規作成する。

> **注意**: `UnityEngine.JsonUtility`はジェネリクス・Dictionary・ネストしたobject型を扱えないため、
> `System.Text.Json`またはサードパーティ（Newtonsoft.Json等）の導入を検討する。
> Unity 6000.0には`Newtonsoft.Json`がパッケージとして含まれている（`com.unity.nuget.newtonsoft-json`）。

**作業内容**:
- `Editor/MCP/JsonRpc/` ディレクトリを新規作成
- `JsonRpcRequest.cs` — `jsonrpc`, `id`, `method`, `params`
- `JsonRpcResponse.cs` — `jsonrpc`, `id`, `result`, `error`
- `JsonRpcError.cs` — `code`, `message`, `data`
- `package.json` の `dependencies` に `com.unity.nuget.newtonsoft-json` を追加

**JSON-RPC 2.0エラーコード定数**:

| コード | 名前 | 用途 |
|--------|------|------|
| -32700 | Parse error | JSON解析失敗 |
| -32600 | Invalid request | リクエスト形式不正 |
| -32601 | Method not found | 未知のメソッド |
| -32602 | Invalid params | パラメータ不正 |
| -32603 | Internal error | 内部エラー |

---

## Phase 2: プロトコルライフサイクルの実装

**目的**: MCPクライアントとの接続確立を可能にする

### 2.1 `initialize` ハンドラの実装

MCPセッション開始時にクライアントが送信する最初のリクエスト。

**リクエスト例**:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "initialize",
  "params": {
    "protocolVersion": "2025-03-26",
    "capabilities": {},
    "clientInfo": { "name": "claude-desktop", "version": "1.0.0" }
  }
}
```

**レスポンス例**:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "protocolVersion": "2025-03-26",
    "capabilities": {
      "tools": { "listChanged": false }
    },
    "serverInfo": {
      "name": "unity-mcp-server",
      "version": "1.0.0"
    }
  }
}
```

**作業内容**:
- `Editor/MCP/McpMethodRouter.cs` を新規作成 — `method`文字列に基づくルーティング
  - `"initialize"` → InitializeHandler
  - `"notifications/initialized"` → InitializedHandler
  - `"ping"` → PingHandler
  - `"tools/list"` → ToolsListHandler
  - `"tools/call"` → ToolsCallHandler
- `Editor/MCP/Handlers/InitializeHandler.cs` — capabilities交渉、バージョンチェック
- 初期化完了前のリクエストを`ping`以外拒否するガード処理

### 2.2 `notifications/initialized` 通知の処理

クライアントが初期化完了を通知する。通知（notification）にはレスポンスを返さない。

**作業内容**:
- `Editor/MCP/Handlers/InitializedHandler.cs` — セッション状態を「初期化済み」に更新

### 2.3 `ping` メソッドの実装

初期化前後いつでも受け付け可能なヘルスチェック。

**レスポンス**:
```json
{"jsonrpc": "2.0", "id": 5, "result": {}}
```

### 2.4 セッション状態管理

**作業内容**:
- `Editor/MCP/McpSession.cs` を新規作成
  - セッション状態（`Uninitialized` / `Initializing` / `Ready`）
  - プロトコルバージョン
  - クライアント情報

---

## Phase 3: トランスポート層の再実装（Streamable HTTP）

**目的**: MCP仕様準拠のトランスポートに置き換える

Unity Editorの制約（プロセス内で動作、stdioが使えない）から、**Streamable HTTP**トランスポートを採用する。

### 3.1 HTTPハンドリングの改修

現在の`McpServer.cs`を改修し、Streamable HTTP仕様に準拠させる。

**必要な対応**:

| HTTPメソッド | 用途 | 現状 |
|---|---|---|
| POST | JSON-RPCメッセージ送信 | 部分的に実装済（プロトコルが違う） |
| GET | SSEストリーム開始 | 未実装 |
| DELETE | セッション終了 | 未実装 |

**作業内容**:
- `McpServer.cs` の `HandleRequest` を改修
  - `POST`: `Content-Type: application/json`、`Accept`ヘッダー検査
  - `GET`: SSEストリーム返却（`Content-Type: text/event-stream`）
  - `DELETE`: セッション終了処理
- リクエストボディのJSON-RPC 2.0パース処理
- レスポンスの`Content-Type`を`Accept`ヘッダーに応じて切り替え
  - `application/json`: 単一JSONレスポンス
  - `text/event-stream`: SSEストリーム

### 3.2 SSE (Server-Sent Events) 対応

サーバーからクライアントへの通知配信に使用する。

**作業内容**:
- `Editor/MCP/Transport/SseWriter.cs` を新規作成
  - `data: {json}\n\n` 形式での書き込み
  - イベントID付与（再接続時のリプレイ用）

### 3.3 セッションID管理

**作業内容**:
- `Mcp-Session-Id` レスポンスヘッダーの発行（`initialize`レスポンス時）
- 後続リクエストでの`Mcp-Session-Id`ヘッダー検証

---

## Phase 4: ツールシステムの MCP準拠

**目的**: `tools/list`と`tools/call`をMCP仕様通りに実装する

### 4.1 `tools/list` メソッドの実装

クライアントがサーバーの利用可能ツールを発見するためのメソッド。

**レスポンス例**:
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

**作業内容**:
- `Editor/MCP/Handlers/ToolsListHandler.cs` を新規作成
- `McpToolRouter.cs` にツール一覧取得メソッドを追加
- 各ツールの`InputSchema`（JSON Schema）を定義

### 4.2 `tools/call` メソッドの実装

**リクエスト例**:
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "tools/call",
  "params": {
    "name": "run_editmode_tests",
    "arguments": {}
  }
}
```

**レスポンス例（成功時）**:
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"passCount\":5,\"failedCount\":0,\"skippedCount\":0,\"failedResults\":[]}"
      }
    ],
    "isError": false
  }
}
```

**レスポンス例（ツール実行エラー時）**:
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "content": [
      { "type": "text", "text": "Error: Unknown tool: invalid_tool" }
    ],
    "isError": true
  }
}
```

**作業内容**:
- `Editor/MCP/Handlers/ToolsCallHandler.cs` を新規作成
- `McpToolRouter.Execute` の戻り値を `CallToolResult`（`content`配列 + `isError`）に変更
- ツール実行エラーをプロトコルエラーではなく`isError: true`で返す

### 4.3 旧プロトコルの削除

**作業内容**:
- `McpProtocol.cs` の `McpRequest` / `McpResponse` を削除（JSON-RPCデータモデルに置き換え済み）

---

## Phase 5: 既存ツールの MCP対応

**目的**: 既存の3ツールをMCP仕様に合わせて調整する

### 5.1 `check_status` ツール

- `inputSchema`を定義（パラメータなし）
- 戻り値を`TextContent`形式に変換

### 5.2 `run_editmode_tests` / `run_playmode_tests` ツール

- `inputSchema`を定義（パラメータなし）
- `TestResultSummary`をJSON文字列化し`TextContent`として返す

### 5.3 IMcpTool実装クラスの更新

- `CommonMcpTool.cs` — `InputSchema`プロパティ対応
- `RunTests.cs` — `InputSchema`プロパティ対応

---

## Phase 6: テストと検証

**目的**: MCPクライアントとの接続を実証する

### 6.1 手動接続テスト

`curl`を使用したStreamable HTTP接続テスト:

```bash
# 1. initialize
curl -X POST http://localhost:7000/mcp/ \
  -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}'

# 2. initialized notification
curl -X POST http://localhost:7000/mcp/ \
  -H "Content-Type: application/json" \
  -H "Mcp-Session-Id: {session-id}" \
  -d '{"jsonrpc":"2.0","method":"notifications/initialized"}'

# 3. tools/list
curl -X POST http://localhost:7000/mcp/ \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -H "Mcp-Session-Id: {session-id}" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}'

# 4. tools/call
curl -X POST http://localhost:7000/mcp/ \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -H "Mcp-Session-Id: {session-id}" \
  -d '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"check_status","arguments":{}}}'
```

### 6.2 MCPクライアント接続テスト

Claude Desktopの設定ファイル（`claude_desktop_config.json`）でStreamable HTTP接続を構成:

```json
{
  "mcpServers": {
    "unity": {
      "url": "http://localhost:7000/mcp/"
    }
  }
}
```

### 6.3 MCP Inspector での検証

[MCP Inspector](https://github.com/modelcontextprotocol/inspector) を使用してプロトコル準拠を確認する。

---

## 新規ファイル一覧

| ファイル | Phase | 説明 |
|---------|-------|------|
| `Editor/MCP/IMcpTool.cs` | 1 | ツールインターフェース定義 |
| `Editor/MCP/JsonRpc/JsonRpcRequest.cs` | 1 | JSON-RPCリクエスト |
| `Editor/MCP/JsonRpc/JsonRpcResponse.cs` | 1 | JSON-RPCレスポンス |
| `Editor/MCP/JsonRpc/JsonRpcError.cs` | 1 | JSON-RPCエラー |
| `Editor/MCP/McpMethodRouter.cs` | 2 | メソッドルーティング |
| `Editor/MCP/McpSession.cs` | 2 | セッション状態管理 |
| `Editor/MCP/Handlers/InitializeHandler.cs` | 2 | 初期化ハンドラ |
| `Editor/MCP/Handlers/InitializedHandler.cs` | 2 | 初期化完了通知ハンドラ |
| `Editor/MCP/Handlers/ToolsListHandler.cs` | 4 | ツール一覧ハンドラ |
| `Editor/MCP/Handlers/ToolsCallHandler.cs` | 4 | ツール実行ハンドラ |
| `Editor/MCP/Transport/SseWriter.cs` | 3 | SSEストリーム書き込み |

## 改修ファイル一覧

| ファイル | Phase | 変更内容 |
|---------|-------|---------|
| `Editor/MCP/McpServer.cs` | 3 | HTTP処理をStreamable HTTP準拠に改修 |
| `Editor/MCP/McpToolRouter.cs` | 4 | ツール一覧API追加、戻り値型変更 |
| `Editor/MCP/McpProtocol.cs` | 4 | 削除（JSON-RPCデータモデルに置き換え） |
| `Editor/MCP/CommonMcpTool.cs` | 5 | InputSchema対応 |
| `Editor/MCP/RunTests.cs` | 5 | InputSchema対応 |
| `package.json` | 1 | Newtonsoft.Json依存追加 |

## 削除ファイル一覧

| ファイル | Phase | 理由 |
|---------|-------|------|
| `Editor/MCP/McpProtocol.cs` | 4 | JSON-RPCデータモデルに置き換え |

---

## 実装スケジュール目安

| Phase | 内容 | 依存関係 |
|-------|------|---------|
| Phase 1 | 基盤整備 | なし |
| Phase 2 | ライフサイクル | Phase 1 |
| Phase 3 | トランスポート | Phase 1, 2 |
| Phase 4 | ツールシステム | Phase 1, 2, 3 |
| Phase 5 | 既存ツール対応 | Phase 1, 4 |
| Phase 6 | テスト・検証 | Phase 1〜5 |

```
Phase 1 ─► Phase 2 ─► Phase 3 ─► Phase 4 ─► Phase 5 ─► Phase 6
基盤整備    ライフ     トランス    ツール     既存ツール   テスト
            サイクル   ポート      システム   対応        検証
```

## 参考資料

- [MCP Specification (2025-03-26)](https://modelcontextprotocol.io/specification/2025-03-26)
- [MCP Specification — Lifecycle](https://modelcontextprotocol.io/specification/2025-03-26/basic/lifecycle)
- [MCP Specification — Transports](https://spec.modelcontextprotocol.io/specification/2025-03-26/basic/transports/)
- [MCP Specification — Tools](https://modelcontextprotocol.io/specification/2025-06-18/server/tools)
- [JSON-RPC 2.0 Specification](https://www.jsonrpc.org/specification)
- [MCP Inspector](https://github.com/modelcontextprotocol/inspector)
