# Unity MCP Server

Unity Editor上で動作するMCP（Model Context Protocol）サーバーです。
外部アプリケーションからHTTPリクエストを通じてUnity Editorの機能を呼び出すことができます。

## 機能

- Unity Editor起動時に自動でHTTPサーバーを開始
- メニューからサーバーのリスタートが可能
- テストの実行（EditMode / PlayMode）
- カスタムツールの登録・実行

## 動作環境

- Unity 6000.0 以上

## インストール

### Unity Package Manager（Git URL）

1. Unity Editorで `Window` > `Package Manager` を開く
2. 左上の `+` ボタンをクリック
3. `Add package from git URL...` を選択
4. 以下のURLを入力して `Add` をクリック

```
https://github.com/AraiYuhki/UnityMcpServer.git?path=Packages/jp.xeon.unity-mcp-server
```

### manifest.json を直接編集

`Packages/manifest.json` の `dependencies` に以下を追加：

```json
{
  "dependencies": {
    "jp.xeon.unity-mcp-server": "https://github.com/AraiYuhki/UnityMcpServer.git?path=Packages/jp.xeon.unity-mcp-server"
  }
}
```

## 使い方

### 基本的な使い方

パッケージをインストールすると、Unity Editor起動時に自動でMCPサーバーが起動します。
デフォルトでは `http://localhost:7000/mcp/` でリクエストを待ち受けます。

### ポート番号の変更

1. Projectウィンドウで右クリック
2. `Create` > `MCPServerSetting` を選択
3. 作成されたアセットでポート番号を設定

### サーバーのリスタート

ポート番号を変更した場合など、サーバーを再起動したい場合は：

1. メニューから `Tools` > `Restart MCP Server` を選択

### 組み込みツール

| ツール名 | 説明 |
|---------|------|
| `check_status` | サーバーの稼働状態を確認 |
| `run_editmode_tests` | EditModeテストを実行 |
| `run_playmode_tests` | PlayModeテストを実行 |

### リクエスト形式

```json
{
  "tool": "ツール名",
  "arguments": "引数（JSON文字列）"
}
```

### レスポンス形式

```json
{
  "ok": true,
  "result": "結果（JSON文字列）",
  "error": "エラーメッセージ（失敗時のみ）"
}
```

### カスタムツールの登録

`McpToolRouter.TryRegisterTool` を使用してカスタムツールを登録できます：

```csharp
using UnityMcp;
using System.Threading.Tasks;

[InitializeOnLoad]
public static class MyCustomTools
{
    static MyCustomTools()
    {
        McpToolRouter.TryRegisterTool("my_custom_tool", MyCustomTool);
    }

    private static Task<object> MyCustomTool(string arguments)
    {
        // 引数をパースして処理を実行
        var result = new { message = "Hello from Unity!" };
        return Task.FromResult<object>(result);
    }
}
```

## ライセンス

MIT OR Apache-2.0

## 作者

Xeon ([@AraiYuhki](https://github.com/AraiYuhki))
