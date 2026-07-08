# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.4.0] - 2026-07-09

### Added

- `take_screenshot` に `includeUi` オプションを追加
  - `view: "game"` と併用すると、Screen Space - Overlay の Canvas UIを一時的に `Screen Space - Camera` モードへ
    切り替えてカメラで一緒にレンダリングし、撮影後は元のレンダーモード・カメラ・planeDistanceに復元する
  - 従来の `camera.Render()` だけではScreen Space - Overlayで描画されるUIが映らなかった問題に対応
  - Edit Mode / Play Mode どちらでも利用可能

## [1.3.0] - 2026-07-03

### Fixed

- `simulate_keyboard` / `simulate_mouse` の入力がPlayMode中のゲームに反映されない問題を修正
  - `QueueState` 内で手動 `InputSystem.Update()` を呼んでいたため、エディタ用の入力ステートバッファに
    消費されプレイモード側に届いていなかった。`EditorApplication.QueuePlayerLoopUpdate()` に変更
  - `tap` / `click` を press → 1フレーム待機（タイムアウト付き）→ release の2段階に変更し、
    `IsPressed` ポーリングでも押下を検出できるようにした
  - `editorInputBehaviorInPlayMode` が既定値（ゲームビュー非フォーカス時に入力が届かない設定）の
    ままなら結果メッセージに警告を追加
- `run_editmode_tests` / `run_playmode_tests` が完了を返せず5分でタイムアウトする問題を修正
  - `new TestRunnerApi()` を `ScriptableObject.CreateInstance<TestRunnerApi>()` に修正
  - PlayModeテストのドメインリロードで完了通知が失われる問題を解消するため、非同期開始+ポーリング方式に変更。
    `[InitializeOnLoad]` でコールバックをドメインリロード後も再登録し、結果は `SessionState` 経由で保存する

### Added

- `get_editmode_test_results` / `get_playmode_test_results` ツールを追加
  （`run_editmode_tests` / `run_playmode_tests` が開始した実行の進行状況・結果をポーリングする）

### Changed

- `run_editmode_tests` / `run_playmode_tests` は結果を待たず、開始したことのみを即時返すようになった
  （破壊的変更。結果は上記の新ツールでポーリングする）

## [1.2.1] - 2026-07-02

### Fixed

- `SimulateUiClick` のコンパイルエラーを修正（`Scene` 型が `UnityEditor.SceneManagement.Scene` と曖昧になる問題を `UnityEngine.SceneManagement.Scene` に明示して解消）

## [1.2.0] - 2026-07-01

### Added

- PlayMode 中にゲーム/UI へ入力を注入する MCP ツールを追加
  - `simulate_keyboard`: Input System の低レベル API でキー入力を注入（InputAction 向け）
  - `simulate_mouse`: 座標移動・ボタン・スクロールを注入
  - `simulate_ui_click`: uGUI の ExecuteEvents で UI 要素を直接クリック
  - Input System / uGUI が未導入のプロジェクトでも壊れないよう、asmdef の versionDefines（MCP_INPUT_SYSTEM / MCP_UGUI）で条件コンパイル

## [1.1.1] - 2026-06-28

### Fixed

- Unity 6 の AssetImportWorker でも `[InitializeOnLoad]` が実行されポート競合が起きる問題を修正（`McpServer`・`McpDispatcher`・`CompilationCache`・`ConsoleLogCache` に `Application.isBatchMode` チェックを追加）
- サーバー停止後に `McpDispatcher` のキューにアクションが残留する問題を修正（`StopListener` 呼び出し時にキューをクリア）
- `RunTests` 実行中にドメインリロードが発生した場合、`async void WaitTimeout` が 5 分間孤立する問題を修正（`Task.WhenAny` に変更し、テスト完了時に即座に停止するよう改善）
- `RunTests` 実行中のドメインリロードで `completionSource` がキャンセルされなかった問題を修正（`AssemblyReloadEvents.beforeAssemblyReload` でキャンセルを登録）
- PlayMode テスト実行中に Play Mode 移行のドメインリロードでテストがキャンセルされる回帰を修正
- SSE GET 接続保持中に `StopListener` が同時に実行されると `WaitWhileListening` で `NullReferenceException` が発生しうる競合を修正

## [1.0.0] - 2026-02-04

### Added

- MCPサーバー機能（HTTPリクエストの待ち受け）
- `check_status` ツール（サーバー稼働確認）
- `run_editmode_tests` ツール（EditModeテスト実行）
- `run_playmode_tests` ツール（PlayModeテスト実行）
- カスタムツール登録機能（`McpToolRouter.TryRegisterTool`）
- ポート番号設定機能（`McpServerSetting`）
