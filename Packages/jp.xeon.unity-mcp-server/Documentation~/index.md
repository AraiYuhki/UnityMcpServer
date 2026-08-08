# Unity MCP Server

Unity Editor上で動作するMCP（Model Context Protocol）サーバーパッケージです。

## 概要

Unity MCP Serverは、外部アプリケーションからHTTPリクエストを通じてUnity Editorの機能を呼び出すことができるサーバーです。
AI アシスタントやビルドシステムなど、外部ツールとの連携を可能にします。

## インストール

### Package Manager UI

1. `Window` > `Package Manager` を開く
2. `+` > `Add package from git URL...` を選択
3. 以下のURLを入力：
   ```
   https://github.com/AraiYuhki/UnityMcpServer.git?path=Packages/jp.xeon.unity-mcp-server
   ```

### manifest.json

```json
{
  "dependencies": {
    "jp.xeon.unity-mcp-server": "https://github.com/AraiYuhki/UnityMcpServer.git?path=Packages/jp.xeon.unity-mcp-server"
  }
}
```

## 設定

### ポート番号の変更

デフォルトではポート7000で待ち受けます。変更するには：

1. `Create` > `MCPServerSetting` でアセットを作成
2. インスペクタでポート番号を設定

サーバーは設定アセットを自動検出します。

## API リファレンス

### エンドポイント

```
POST http://localhost:{port}/mcp/
Content-Type: application/json
```

### リクエスト

```json
{
  "tool": "string",      // 実行するツール名
  "arguments": "string"  // 引数（JSON文字列）
}
```

### レスポンス

```json
{
  "ok": true,            // 成功/失敗
  "result": "string",    // 結果（JSON文字列）
  "error": "string"      // エラーメッセージ（失敗時）
}
```

## 組み込みツール

### check_status

サーバーの稼働状態を確認します。

**リクエスト例：**
```json
{
  "tool": "check_status",
  "arguments": ""
}
```

**レスポンス例：**
```json
{
  "ok": true,
  "result": "true"
}
```

### run_editmode_tests

EditModeテストの実行を開始します。テスト完了は待たず、開始したことだけを即座に返すため、
結果は `get_editmode_test_results` でポーリングしてください。

**リクエスト例：**
```json
{
  "tool": "run_editmode_tests",
  "arguments": ""
}
```

**レスポンス例：**
```json
{
  "ok": true,
  "result": "{\"status\":\"started\",\"message\":\"EditMode tests started. Poll the corresponding get_*_test_results tool for the outcome.\"}"
}
```

### get_editmode_test_results

`run_editmode_tests` で開始したテストの進行状況・結果を取得します。

**レスポンス例（完了時）：**
```json
{
  "ok": true,
  "result": "{\"status\":\"completed\",\"summary\":\"5 passed, 0 failed, 0 skipped (5 total)\",\"totalCount\":5,\"passCount\":5,\"failCount\":0,\"skipCount\":0,\"allPassed\":true,\"failures\":[]}"
}
```

### run_playmode_tests

PlayModeテストの実行を開始します。PlayMode移行時にドメインリロードが発生し
開始要求元のawaitが失われるため、`run_editmode_tests` と同様に開始したことだけを
即座に返します。結果は `get_playmode_test_results` でポーリングしてください。

**リクエスト例：**
```json
{
  "tool": "run_playmode_tests",
  "arguments": ""
}
```

### get_playmode_test_results

`run_playmode_tests` で開始したテストの進行状況・結果を取得します。
レスポンス形式は `get_editmode_test_results` と同じです。

## カスタムツールの実装

`IMcpTool` を実装したクラスに `[McpTool]` 属性を付けると、サーバー起動時に自動で登録されます。
このパッケージ外のアセンブリ（自作パッケージやプロジェクト内のEditorアセンブリ）で定義したツールも対象です。

### 基本的な実装

```csharp
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityMcp;

[McpTool]
public class MyTool : IMcpTool
{
    public string Name => "my_tool";

    public string Description => "Do something useful in the Unity Editor.";

    public string InputSchema =>
        "{\"type\":\"object\",\"properties\":{" +
        "\"input\":{\"type\":\"string\",\"description\":\"Text to process\"}" +
        "},\"required\":[\"input\"]}";

    public Task<object> Execute(string args)
    {
        var parsed = JsonConvert.DeserializeObject<MyToolArgs>(args);
        var result = new MyToolResult
        {
            Success = true,
            Message = $"Processed: {parsed.Input}"
        };
        return Task.FromResult<object>(result);
    }
}

public class MyToolArgs
{
    [JsonProperty("input")]
    public string Input { get; set; }
}

public class MyToolResult
{
    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }
}
```

### 自動登録の要件

`[McpTool]` を付けた型は、次の条件をすべて満たす必要があります。満たさない場合はConsoleに警告を出してスキップされます。

- `IMcpTool` を実装している
- 抽象クラス・ジェネリック型定義ではない
- 公開のパラメータなしコンストラクタを持つ

また、`Name` が組み込みツールや他のカスタムツールと重複した場合、先に登録されたものが優先され、後続は警告付きでスキップされます。

### 外部アセンブリから利用する場合

対象のasmdefから `jp.xeon.unity-mcp-server` を参照してください（`autoReferenced` が有効なため、asmdefを持たないEditorスクリプトからは参照設定なしで利用できます）。
このパッケージは `includePlatforms: ["Editor"]` のため、カスタムツールもEditor専用アセンブリ（`Editor` フォルダ配下、または `includePlatforms` にEditorのみを指定したasmdef）に置く必要があります。
自動登録はサーバー初期化時に `TypeCache` で走査されるため、`[InitializeOnLoad]` を書く必要はありません。

### 非同期処理

長時間かかる処理は `Execute` を `async` にして実装できます：

```csharp
public async Task<object> Execute(string args)
{
    await Task.Delay(1000);
    return new { completed = true };
}
```

### 明示的に登録する

動的に生成したツールなど、属性で宣言できないものは `McpToolRouter.TryRegisterTool(IMcpTool)` で登録できます。
デリゲートをツール化する `CommonMcpTool` も利用できます。

```csharp
McpToolRouter.TryRegisterTool(new CommonMcpTool(
    "my_dynamic_tool",
    args => Task.FromResult<object>(new { ok = true }),
    "A dynamically registered tool."));
```

ただし `McpToolRouter.Initialize()` は登録済みツールをクリアしてから組み込みツールを登録するため、
サーバー起動やドメインリロードのタイミングによっては登録内容が失われます。
恒久的に有効にしたいツールには `[McpTool]` による自動登録を使用してください。

## アーキテクチャ

```
┌─────────────────┐     HTTP      ┌─────────────────┐
│  External App   │ ───────────►  │   McpServer     │
│  (AI Assistant) │               │  (HttpListener) │
└─────────────────┘               └────────┬────────┘
                                           │
                                           ▼
                                  ┌─────────────────┐
                                  │  McpDispatcher  │
                                  │  (Main Thread)  │
                                  └────────┬────────┘
                                           │
                                           ▼
                                  ┌─────────────────┐
                                  │  McpToolRouter  │
                                  │  (Tool Exec)    │
                                  └─────────────────┘
```

- **McpServer**: HTTPリクエストをバックグラウンドスレッドで待ち受け
- **McpDispatcher**: Unity APIを呼び出すためメインスレッドに処理をキューイング
- **McpToolRouter**: 登録されたツールを検索・実行

## トラブルシューティング

### サーバーが起動しない

- 指定したポートが他のアプリケーションで使用されていないか確認
- Consoleウィンドウでエラーログを確認

### ツールが見つからない

- ツール名のスペルを確認
- カスタムツールが正しく登録されているか確認
- `[InitializeOnLoad]` 属性が付与されているか確認

### レスポンスが返ってこない

- Unity Editorがフリーズしていないか確認
- 非同期処理が正しく完了しているか確認
