# Unity MCP Server

Unity Editor上で動作するMCP（Model Context Protocol）サーバーです。
外部アプリケーションからHTTPリクエストを通じてUnity Editorの機能を呼び出すことができます。

## 機能

- MCP 2025-03-26 仕様準拠（Streamable HTTP + JSON-RPC 2.0）
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

### プロトコル

MCP 2025-03-26 仕様に準拠した Streamable HTTP トランスポートを使用します。
すべてのリクエストは JSON-RPC 2.0 形式で `POST /mcp/` に送信します。

#### 対応メソッド

| メソッド | 説明 |
|---------|------|
| `initialize` | セッションの初期化（ハンドシェイク） |
| `notifications/initialized` | 初期化完了通知 |
| `ping` | 疎通確認 |
| `tools/list` | 登録済みツール一覧の取得 |
| `tools/call` | ツールの実行 |

#### 接続フロー

```
1. POST initialize        → セッションID取得
2. POST notifications/initialized  → 初期化完了を通知（レスポンスなし）
3. POST tools/call         → ツール実行（以降、Mcp-Session-Id ヘッダー必須）
```

#### リクエスト例（tools/call）

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "run_editmode_tests",
    "arguments": {}
  }
}
```

#### レスポンス例

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"summary\":\"70 passed, 0 failed, 0 skipped (70 total)\", ...}"
      }
    ],
    "isError": false
  }
}
```

### Claude Codeとの接続

Unityプロジェクトのルートに `.mcp.json` を作成してください：

```json
{
  "mcpServers": {
    "unity-mcp": {
      "type": "http",
      "url": "http://localhost:7000/mcp/"
    }
  }
}
```

Claude Codeを起動（または再起動）すると、自動的にMCPサーバーへ接続されます。

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
