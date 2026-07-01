# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
