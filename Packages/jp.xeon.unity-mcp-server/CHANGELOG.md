# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.7.0] - 2026-08-21

### Added

- `run_editmode_tests` / `run_playmode_tests` にテストの絞り込みオプションを追加
  - `testNames` / `groupNames`（正規表現） / `categoryNames` / `assemblyNames` で対象を明示指定できる
  - `onlyFailures: true` で、そのテストモードの直近の完了実行で失敗・スキップされたテストのみ再実行できる
    - `TestResultSummary` に `skippedTests`（スキップされたテストの完全修飾名一覧）を追加し、再実行対象の特定に使用
  - `changedFilesOnly: true`（+ 任意の `gitRef`、既定 `HEAD`）で、git差分のある `.cs` ファイルが属するアセンブリと、
    それに依存するアセンブリ（テストアセンブリ等）のみを実行できる
    - asmdefの参照（名前参照・GUID参照の両方）を解決して依存グラフを構築する `GitChangedAssemblyResolver` を追加
  - 絞り込み条件に該当するテストが無い場合はフルスイートへフォールバックせず、
    `status: "not_started"` とその理由を返す

## [1.6.0] - 2026-08-08

### Added

- `[McpTool]` 属性によるカスタムツールの自動登録に対応
  - `TypeCache` でプロジェクト全体を走査するため、パッケージ外のアセンブリで定義したツールも登録される
  - `[InitializeOnLoad]` での手動登録が `McpToolRouter.Initialize()` のクリア処理と競合し、
    ドメインリロードのタイミングによってツールが失われる問題を回避できる
  - 要件を満たさない型・名前が重複した型は警告を出してスキップする

### Fixed

- README / `Documentation~/index.md` / `ImplementationGuid.md` のカスタムツール登録サンプルが、
  存在しない `TryRegisterTool(string, Func<string, Task<object>>)` を使用していたのを修正

## [1.5.0] - 2026-08-08

### Added

- `compile_and_wait` ツールを追加
  - `AssetDatabase.Refresh()` 後、コンパイル完了まで待機して**確定した**エラー・警告のみを返す
  - `import_asset` + `get_compile_errors` の「コンパイル前の前回結果を緑と誤認する」問題を根絶する
  - タイムアウト（既定120秒）付き。ドメインリロードで応答が失われた場合も冪等に再送できる
- `save_scene` / `save_scene_as` ツールを追加
  - `EditorSceneManager.SaveScene` / `SaveOpenScenes` による明示保存。プロンプトを伴うAPIは使わない
- `step_frames` ツールを追加
  - `EditorApplication.QueuePlayerLoopUpdate()` を駆動し、エディタ非フォーカスでもフレームを確実に進める
  - スクリーンショットや状態観測の直前に呼ぶことで静止画問題を回避できる
- `simulate_ui_drag` ツールを追加（press → move → release のドラッグを1呼び出しで再現）
- `simulate_ui_click` に `action`（click / hold / press / release）と `holdMs` を追加
- `simulate_keyboard` に `hold` アクションと `durationMs` を追加（押しっぱなし入力）
- `get_console_logs` に `sinceToken` / `onlyErrors` / `onlyExceptions` を追加し、
  戻り値に `nextToken` / `truncated` を追加（前回以降の新規エラーだけを取得できる）

### Changed

- 動作確認済みのUnityバージョンを 6000.5.5f1 に更新（対応バージョンは引き続き 6000.0 以上）
- `get_compile_errors` の戻り値に `state`（idle/pending/compiling/completed）、`isStale`、
  `finishedAt`、`durationMs`、`compilationFailed` を追加
  - `CompilationCache` に状態機械を実装し、状態と結果を `SessionState` へ退避してドメインリロードを跨いで保持する
  - スクリプト・asmdefの変更を `AssetPostprocessor` で検知して `pending` へ落とす
- `enter_play_mode` / `open_scene` / `run_editmode_tests` / `run_playmode_tests` に
  未保存シーンのガードを追加（`autoSave` / `discard` で挙動を選択、既定は拒否）
  - モーダルダイアログでエディタが応答不能になる事象を防ぐ
- `run_editmode_tests` / `run_playmode_tests` / `enter_play_mode` にコンパイルゲートを追加
  - コンパイルが未確定またはエラー保持中なら実行を拒否する（`force` で回避可能）
- `enter_play_mode` / `exit_play_mode` / `open_scene` / `compile_and_wait` / `step_frames` の戻り値に
  新規コンソールエラーの要約 `newConsoleErrors` を同梱（`failOnNewError` で失敗扱いにもできる）
- `check_status` が readiness を返すよう変更
  （`isReady` / `isCompiling` / `isUpdating` / `compileState` / `isPlaying` / `busyReason`）
- ビジー時・セッション不正時・メソッド不許可時のHTTP応答を必ず JSON-RPC エラーとして返すよう変更
  - すべての経路で `Content-Type: application/json` を設定し、`Unexpected content type: null` を防ぐ
  - コンパイル中・ドメインリロード中は `-32001 server busy`（再送可能）を返す
  - ドメインリロードで打ち切られる実行待ち・実行中のリクエストにもビジーエラーを返し、無応答をなくす
    （応答権を `TryRemove` で排他的に取得し、二重応答を防ぐ）

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
