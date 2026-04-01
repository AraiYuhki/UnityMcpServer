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

### AIクライアントの設定

MCPクライアント（Claude Code, Claude Desktop 等）の設定ファイルにサーバーを追加します。

**Claude Code（`~/.claude.json` または `.mcp.json`）:**

```json
{
  "mcpServers": {
    "unity-mcp": {
      "url": "http://localhost:7000/mcp/"
    }
  }
}
```

**Claude Desktop（`claude_desktop_config.json`）:**

```json
{
  "mcpServers": {
    "unity-mcp": {
      "url": "http://localhost:7000/mcp/"
    }
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

### 組み込みツール（全46種）

#### ステータス・診断

| ツール名 | 説明 |
|---------|------|
| `check_status` | サーバーの稼働状態を確認 |
| `get_compile_errors` | 直近のコンパイルエラー・警告を取得 |
| `get_console_logs` | Consoleのログ一覧を取得（フィルタ対応） |

#### シーン操作

| ツール名 | 説明 |
|---------|------|
| `get_scene_hierarchy` | 現在のシーンのGameObject階層を取得 |
| `get_component_info` | 指定GameObjectのコンポーネント詳細を取得 |
| `set_component_property` | コンポーネントのシリアライズプロパティを変更 |
| `add_component` | GameObjectにコンポーネントを追加 |
| `remove_component` | GameObjectからコンポーネントを削除 |
| `create_gameobject` | 新規GameObjectを作成 |
| `delete_gameobject` | GameObjectを削除 |
| `move_gameobject` | GameObjectを別の親に移動 |
| `duplicate_gameobject` | GameObjectを複製 |
| `open_scene` | シーンファイルを開く |
| `take_screenshot` | Scene/Game Viewのスクリーンショットを取得 |
| `get_canvas_info` | Canvas・RectTransformの情報を取得 |
| `set_rect_transform` | RectTransformのプロパティを設定 |
| `raycast` | 物理レイキャストを実行 |
| `bake_navmesh` | NavMeshをベイク |

#### プレハブ操作

| ツール名 | 説明 |
|---------|------|
| `get_prefab_hierarchy` | プレハブアセットのGameObject階層を取得 |
| `get_prefab_component_info` | プレハブ内のコンポーネント詳細情報を取得 |
| `set_prefab_component_property` | プレハブ内のコンポーネントプロパティを変更 |
| `create_prefab` | シーン上のGameObjectからプレハブを作成 |

#### アセット操作

| ツール名 | 説明 |
|---------|------|
| `get_asset_list` | 指定パス以下のアセット一覧を取得（フィルタ対応） |
| `create_script` | C#スクリプトを新規作成 |
| `read_script` | C#スクリプト/テキストアセットの内容を読み取り |
| `create_asset` | ScriptableObjectアセットを作成 |
| `delete_asset` | アセットを削除（OSゴミ箱へ移動） |
| `move_asset` | アセットを移動・リネーム |
| `import_asset` | アセットの再インポート / AssetDatabase更新 |
| `find_missing_references` | シーン・Prefab内のMissing Referenceを検索 |

#### マテリアル操作

| ツール名 | 説明 |
|---------|------|
| `get_material_properties` | マテリアルのシェーダープロパティを全取得 |
| `set_material_property` | マテリアルのシェーダープロパティを変更 |

#### アニメーション

| ツール名 | 説明 |
|---------|------|
| `get_animator_parameters` | Animator Controllerのパラメータ一覧を取得 |
| `get_animation_clips` | AnimationClip一覧を取得 |

#### エディタ操作

| ツール名 | 説明 |
|---------|------|
| `enter_play_mode` | Play Modeに入る |
| `exit_play_mode` | Play Modeを終了する |
| `get_play_mode_state` | 現在のPlay Mode状態を取得 |
| `undo` | 直前の操作を元に戻す |
| `redo` | 元に戻した操作をやり直す |
| `get_build_settings` | ビルド設定を取得 |
| `build_project` | プロジェクトをビルド |

#### テスト実行

| ツール名 | 説明 |
|---------|------|
| `run_editmode_tests` | EditModeテストを全件実行 |
| `run_playmode_tests` | PlayModeテストを全件実行 |

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

## アクセスリソース範囲

MCPサーバーがアクセスする Unity Editor のリソース範囲です。

| カテゴリ | 読み取り | 書き込み | 対象 |
|---------|:-------:|:-------:|------|
| シーンヒエラルキー | o | o | GameObjectの取得・作成・削除・移動・複製 |
| コンポーネント | o | o | コンポーネントの情報取得・追加・削除・プロパティ変更 |
| プレハブ | o | o | プレハブの階層取得・作成・プロパティ変更 |
| アセット（スクリプト） | o | o | C#スクリプトの読み取り・新規作成 |
| アセット（一般） | o | o | アセットの一覧取得・作成・削除・移動・再インポート |
| マテリアル | o | o | シェーダープロパティの取得・変更 |
| アニメーション | o | - | Animator パラメータ・AnimationClip の取得 |
| 物理演算 | o | o | Raycast 実行・NavMesh ベイク |
| UI（Canvas） | o | o | Canvas情報取得・RectTransform変更 |
| エディタ状態 | o | o | Play Mode 制御・Undo/Redo |
| ビルド | o | o | ビルド設定取得・プロジェクトビルド実行 |
| コンパイル・コンソール | o | - | コンパイルエラー・コンソールログの取得 |
| テスト | o | - | EditMode/PlayMode テストの実行と結果取得 |
| スクリーンショット | o | - | Scene/Game View のキャプチャ |

> **注意**: 書き込み操作（GameObject作成・アセット削除・ビルド等）はUnity Editorの状態を変更します。`undo` ツールで直前の操作を取り消すことができます。

## セキュリティ

### 通信

- サーバーは **`localhost` のみ** でリッスンします（外部ネットワークからはアクセスできません）
- 通信プロトコルは **HTTP**（TLS非対応）です
- セッション管理は `Mcp-Session-Id` ヘッダーで行われます

### 認証

- 現時点では **認証機構はありません**
- 同一マシン上の任意のプロセスからアクセス可能です
- 信頼できない環境で使用する場合はファイアウォール等で `localhost:7000` へのアクセスを制限してください

### 破壊的操作

- `delete_asset` はファイルを **OSのゴミ箱に移動** します（完全削除ではありません）
- `delete_gameobject`、`remove_component` 等のシーン変更は `undo` で元に戻せます
- `build_project` はプロジェクトのビルドを実行します。ビルド先のパスに注意してください

### 推奨事項

- 本プラグインは **開発環境専用** です。本番・ステージング環境にはインストールしないでください
- バージョン管理下で使用し、意図しない変更はgitで復元してください
- ポート番号はデフォルト `7000` ですが、`McpServerSetting` アセットで変更可能です

## ライセンス

MIT OR Apache-2.0

## 作者

Xeon ([@AraiYuhki](https://github.com/AraiYuhki))
