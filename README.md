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
| `get_compile_errors` | 直近のコンパイルエラー・警告を取得 |
| `get_scene_hierarchy` | 現在のシーンのGameObject階層を取得 |
| `get_component_info` | 指定GameObjectのコンポーネント詳細を取得 |
| `find_missing_references` | シーン・Prefab内のMissing Referenceを検索 |
| `get_asset_list` | 指定パス以下のアセット一覧を取得 |
| `get_console_logs` | Consoleのログ一覧を取得 |
| `simulate_keyboard` | キーボード入力をシミュレート（PlayMode・Input System必須） |
| `simulate_mouse` | マウス入力をシミュレート（PlayMode・Input System必須） |
| `simulate_ui_click` | uGUI要素のクリックをシミュレート（PlayMode・uGUI必須） |

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

`IMcpTool` を実装したクラスに `[McpTool]` 属性を付けると、サーバー起動時に自動で登録されます。
このパッケージ外のEditorアセンブリで定義したツールも対象です：

```csharp
using UnityMcp;
using System.Threading.Tasks;

[McpTool]
public class MyCustomTool : IMcpTool
{
    public string Name => "my_custom_tool";

    public string Description => "Say hello from the Unity Editor.";

    public string InputSchema => "{\"type\":\"object\",\"properties\":{},\"required\":[]}";

    public Task<object> Execute(string args)
    {
        var result = new { message = "Hello from Unity!" };
        return Task.FromResult<object>(result);
    }
}
```

`[McpTool]` を付けた型は `IMcpTool` を実装し、公開のパラメータなしコンストラクタを持つ必要があります。
名前が既存ツールと重複した場合は警告付きでスキップされます。
動的に生成するツールは `McpToolRouter.TryRegisterTool(IMcpTool)` でも登録できます。
詳細は [Documentation~/index.md](Packages/jp.xeon.unity-mcp-server/Documentation~/index.md) を参照してください。

## ライセンス

MIT OR Apache-2.0

## 作者

Xeon ([@AraiYuhki](https://github.com/AraiYuhki))
