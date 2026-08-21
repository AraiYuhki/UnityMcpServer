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

- Unity 6000.0 以上（動作確認: 6000.5.5f1）
- com.unity.nuget.newtonsoft-json 3.2.1 以上
- 任意: com.unity.inputsystem（`simulate_keyboard` / `simulate_mouse`）、com.unity.ugui（`simulate_ui_click` / `simulate_ui_drag`）

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
| `run_editmode_tests` | EditModeテストの実行を開始（結果は`get_editmode_test_results`でポーリング）。`testNames`/`groupNames`/`categoryNames`/`assemblyNames`で絞り込み、`onlyFailures`で前回の失敗・未実行のみ再実行、`changedFilesOnly`でgit差分に関連するアセンブリのみ実行できる |
| `run_playmode_tests` | PlayModeテストの実行を開始（結果は`get_playmode_test_results`でポーリング）。フィルタオプションは`run_editmode_tests`と同じ |
| `get_editmode_test_results` | `run_editmode_tests`の結果を取得（実行中/未実行/完了） |
| `get_playmode_test_results` | `run_playmode_tests`の結果を取得（実行中/未実行/完了） |
| `get_compile_errors` | 直近のコンパイルエラー・警告を取得（`state` / `isStale` 付き） |
| `compile_and_wait` | リフレッシュ後、コンパイル完了まで待って確定した結果を返す |
| `save_scene` | 開いているシーンを保存（ダイアログを出さない） |
| `save_scene_as` | アクティブなシーンを指定パスへ保存 |
| `step_frames` | PlayModeのフレームを指定数だけ確実に進める |
| `get_scene_hierarchy` | 現在のシーンのGameObject階層を取得 |
| `get_component_info` | 指定GameObjectのコンポーネント詳細を取得 |
| `find_missing_references` | シーン・Prefab内のMissing Referenceを検索 |
| `get_asset_list` | 指定パス以下のアセット一覧を取得 |
| `get_console_logs` | Consoleのログ一覧を取得（`sinceToken`で差分取得可） |

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
        "text": "{\"status\":\"started\",\"message\":\"EditMode tests started. Poll the corresponding get_*_test_results tool for the outcome.\"}"
      }
    ],
    "isError": false
  }
}
```

`get_editmode_test_results` / `get_playmode_test_results` を呼ぶと、実行中は
`{"status":"running", ...}`、未実行時は `{"status":"not_started", ...}`、
完了後は `{"status":"completed","summary":"70 passed, 0 failed, 0 skipped (70 total)", ...}`
が返る。

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

`IMcpTool` を実装したクラスに `[McpTool]` 属性を付けると、サーバー起動時に自動で登録されます。
このパッケージ外のアセンブリで定義したツールも対象です：

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
動的に生成するツールは `McpToolRouter.TryRegisterTool(IMcpTool)` でも登録できます。詳細は `Documentation~/index.md` を参照してください。

## ライセンス

MIT OR Apache-2.0

## 作者

Xeon ([@AraiYuhki](https://github.com/AraiYuhki))
