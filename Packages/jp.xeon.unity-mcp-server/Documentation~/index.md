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

EditModeテストを実行します。

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
  "result": "{\"passed\":5,\"failed\":0,\"skipped\":0}"
}
```

### run_playmode_tests

PlayModeテストを実行します。

**リクエスト例：**
```json
{
  "tool": "run_playmode_tests",
  "arguments": ""
}
```

## カスタムツールの実装

`McpToolRouter.TryRegisterTool` を使用してカスタムツールを登録できます。

### 基本的な実装

```csharp
using UnityEditor;
using UnityMcp;
using System.Threading.Tasks;

[InitializeOnLoad]
public static class MyCustomTools
{
    static MyCustomTools()
    {
        // サーバー初期化時に登録
        McpToolRouter.TryRegisterTool("my_tool", ExecuteMyTool);
    }

    private static Task<object> ExecuteMyTool(string arguments)
    {
        // 引数のパース
        var args = JsonUtility.FromJson<MyToolArgs>(arguments);

        // 処理の実行
        var result = new MyToolResult
        {
            success = true,
            message = $"Processed: {args.input}"
        };

        return Task.FromResult<object>(result);
    }
}

[System.Serializable]
public class MyToolArgs
{
    public string input;
}

[System.Serializable]
public class MyToolResult
{
    public bool success;
    public string message;
}
```

### 非同期処理

長時間かかる処理は非同期で実装できます：

```csharp
private static async Task<object> LongRunningTool(string arguments)
{
    // 非同期処理
    await Task.Delay(1000);

    return new { completed = true };
}
```

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
