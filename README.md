# Unity MCP Server

## 概要

Unity Editor上で動作するMCP（Model Context Protocol）サーバー。
AIエージェント（Claude Code, Claude Desktop 等）からHTTPリクエストを通じてUnity Editorのシーン編集・アセット管理・ビルド等の機能を直接制御できるようにすることで、AI駆動のUnity開発ワークフローを実現する。

## 提供Tool一覧

全46種のToolを提供する。

### ステータス・診断

| Tool名 | 説明 | アクセスリソース | 読み取り/書き込み |
|--------|------|-----------------|------------------|
| `check_status` | サーバーの稼働状態を確認 | MCPサーバー | 読み取り専用 |
| `get_compile_errors` | 直近のコンパイルエラー・警告を取得 | コンパイルキャッシュ | 読み取り専用 |
| `get_console_logs` | Consoleのログ一覧を取得（フィルタ対応） | コンソールログ | 読み取り専用 |

### シーン操作

| Tool名 | 説明 | アクセスリソース | 読み取り/書き込み |
|--------|------|-----------------|------------------|
| `get_scene_hierarchy` | 現在のシーンのGameObject階層を取得 | シーンヒエラルキー | 読み取り専用 |
| `get_component_info` | 指定GameObjectのコンポーネント詳細を取得 | シーンコンポーネント | 読み取り専用 |
| `set_component_property` | コンポーネントのシリアライズプロパティを変更 | シーンコンポーネント | 読み書き |
| `add_component` | GameObjectにコンポーネントを追加 | シーンコンポーネント | 読み書き |
| `remove_component` | GameObjectからコンポーネントを削除 | シーンコンポーネント | 読み書き |
| `create_gameobject` | 新規GameObjectを作成 | シーンヒエラルキー | 読み書き |
| `delete_gameobject` | GameObjectを削除 | シーンヒエラルキー | 読み書き |
| `move_gameobject` | GameObjectを別の親に移動 | シーンヒエラルキー | 読み書き |
| `duplicate_gameobject` | GameObjectを複製 | シーンヒエラルキー | 読み書き |
| `open_scene` | シーンファイルを開く | シーンファイル | 読み書き |
| `take_screenshot` | Scene/Game Viewのスクリーンショットを取得 | Scene/Game View | 読み取り専用 |
| `get_canvas_info` | Canvas・RectTransformの情報を取得 | UI Canvas | 読み取り専用 |
| `set_rect_transform` | RectTransformのプロパティを設定 | UI Canvas | 読み書き |
| `raycast` | 物理レイキャストを実行 | 物理演算 | 読み取り専用 |
| `bake_navmesh` | NavMeshをベイク | NavMesh | 読み書き |

### プレハブ操作

| Tool名 | 説明 | アクセスリソース | 読み取り/書き込み |
|--------|------|-----------------|------------------|
| `get_prefab_hierarchy` | プレハブアセットのGameObject階層を取得 | プレハブアセット | 読み取り専用 |
| `get_prefab_component_info` | プレハブ内のコンポーネント詳細情報を取得 | プレハブアセット | 読み取り専用 |
| `set_prefab_component_property` | プレハブ内のコンポーネントプロパティを変更 | プレハブアセット | 読み書き |
| `create_prefab` | シーン上のGameObjectからプレハブを作成 | プレハブアセット | 読み書き |

### アセット操作

| Tool名 | 説明 | アクセスリソース | 読み取り/書き込み |
|--------|------|-----------------|------------------|
| `get_asset_list` | 指定パス以下のアセット一覧を取得（フィルタ対応） | AssetDatabase | 読み取り専用 |
| `create_script` | C#スクリプトを新規作成 | `Assets/` 配下 | 読み書き |
| `read_script` | C#スクリプト/テキストアセットの内容を読み取り | `Assets/` 配下 | 読み取り専用 |
| `create_asset` | ScriptableObjectアセットを作成 | `Assets/` 配下 | 読み書き |
| `delete_asset` | アセットを削除（OSゴミ箱へ移動） | `Assets/` 配下 | 読み書き |
| `move_asset` | アセットを移動・リネーム | `Assets/` 配下 | 読み書き |
| `import_asset` | アセットの再インポート / AssetDatabase更新 | AssetDatabase | 読み書き |
| `find_missing_references` | シーン・Prefab内のMissing Referenceを検索 | シーン / プレハブ | 読み取り専用 |

### マテリアル操作

| Tool名 | 説明 | アクセスリソース | 読み取り/書き込み |
|--------|------|-----------------|------------------|
| `get_material_properties` | マテリアルのシェーダープロパティを全取得 | マテリアルアセット | 読み取り専用 |
| `set_material_property` | マテリアルのシェーダープロパティを変更 | マテリアルアセット | 読み書き |

### アニメーション

| Tool名 | 説明 | アクセスリソース | 読み取り/書き込み |
|--------|------|-----------------|------------------|
| `get_animator_parameters` | Animator Controllerのパラメータ一覧を取得 | Animatorアセット | 読み取り専用 |
| `get_animation_clips` | AnimationClip一覧を取得 | Animatorアセット | 読み取り専用 |

### エディタ操作

| Tool名 | 説明 | アクセスリソース | 読み取り/書き込み |
|--------|------|-----------------|------------------|
| `enter_play_mode` | Play Modeに入る | エディタ状態 | 読み書き |
| `exit_play_mode` | Play Modeを終了する | エディタ状態 | 読み書き |
| `get_play_mode_state` | 現在のPlay Mode状態を取得 | エディタ状態 | 読み取り専用 |
| `undo` | 直前の操作を元に戻す | エディタ操作履歴 | 読み書き |
| `redo` | 元に戻した操作をやり直す | エディタ操作履歴 | 読み書き |
| `get_build_settings` | ビルド設定を取得 | ビルド設定 | 読み取り専用 |
| `build_project` | プロジェクトをビルド | ビルドパイプライン | 読み書き |

### テスト実行

| Tool名 | 説明 | アクセスリソース | 読み取り/書き込み |
|--------|------|-----------------|------------------|
| `run_editmode_tests` | EditModeテストを全件実行 | テストランナー | 読み取り専用 |
| `run_playmode_tests` | PlayModeテストを全件実行 | テストランナー | 読み取り専用 |

## アクセスリソース範囲

このMCPサーバーはUnity Editorプロセス内で動作し、以下のリソースにアクセスする。

- **許可範囲**:
  - Unity Editorが管理する全アセット（`Assets/` 配下）
  - 現在開いているシーンのヒエラルキーとコンポーネント
  - プロジェクト内のプレハブ・マテリアル・アニメーションアセット
  - Unity EditorのPlay Mode状態、Undo履歴、ビルドパイプライン
  - コンパイルエラー・コンソールログのキャッシュ（メモリ上）
- **アクセスしないリソース**:
  - `Assets/` 外のファイルシステム（OS上の任意ファイルへのアクセスは不可）
  - 環境変数、認証情報ファイル（`.env`、`*.key`、`.ssh/` 等）
  - ネットワーク・外部API（サーバー自体はlocalhostでのリクエスト待受のみ）
  - `Packages/`、`ProjectSettings/` への直接書き込み

> **注意**: 書き込み操作（GameObject作成・アセット削除・ビルド等）はUnity Editorの状態を変更する。`undo` ツールで直前の操作を取り消し可能。`delete_asset` はOSのゴミ箱に移動するため完全削除ではない。

## インストールおよび実行方法

### 動作環境

- Unity 6000.0 以上

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

### 実行

パッケージをインストールすると、Unity Editor起動時に自動でMCPサーバーが起動する。
デフォルトでは `http://localhost:7000/mcp/` でリクエストを待ち受ける。

- **ポート番号の変更**: Projectウィンドウで右クリック → `Create` > `MCPServerSetting` → アセットでポート番号を設定
- **サーバーの再起動**: メニューから `Tools` > `Restart MCP Server` を選択

## mcp.json設定例

AIクライアントの設定ファイルにサーバーを追加する。

**Claude Code（`.mcp.json`）:**

```json
{
  "mcpServers": {
    "unity-mcp": {
      "type": "url",
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

設定後、AIクライアントから `check_status` ツールを呼び出し、`true` が返れば接続成功。

## セキュリティ情報

### 通信・認証

- サーバーは **`localhost` のみ** でリッスン（外部ネットワークからはアクセス不可）
- 通信プロトコルは **HTTP**（TLS非対応）
- セッション管理は `Mcp-Session-Id` ヘッダーで行う
- **認証機構なし** — 同一マシン上の任意のプロセスからアクセス可能

### 破壊的操作の安全策

- `delete_asset` はファイルを **OSのゴミ箱に移動**（完全削除ではない）
- `delete_gameobject`、`remove_component` 等のシーン変更は `undo` で復元可能
- `build_project` はプロジェクトのビルドを実行するため、ビルド先パスに注意

### 推奨事項

- 本プラグインは **開発環境専用** — 本番・ステージング環境にはインストールしないこと
- バージョン管理下で使用し、意図しない変更はgitで復元すること
- 信頼できない環境ではファイアウォール等で `localhost:7000` へのアクセスを制限すること

### セキュリティスキャン

- Snyk Organization：`arai_yuki`
- 最終セキュリティスキャン日：2026-04-02

| スキャン種類 | 結果 | Critical | High | Medium | Low |
|-------------|------|:--------:|:----:|:------:|:---:|
| Snyk Open Source（`snyk test`） | 脆弱性なし（25プロジェクトスキャン済） | 0 | 0 | 0 | 0 |
| Snyk Code（`snyk code test`） | 問題なし | 0 | 0 | 0 | 0 |

## ライセンス

MIT OR Apache-2.0（自社開発）

## 開発者

- 開発/保守：Arai Yuhki（xeon.lagunas@gmail.com）
- リポジトリ：`git@github.com:cocone-development/UnityMcpServer.git`
- 問い合わせチャネル：<!-- TODO: Slackチャネルを記入 -->
