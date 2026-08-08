# UnityMcpServer 機能追加プラン（CLI 比較を踏まえて）

作成日: 2026-08-08
対象: `Packages/jp.xeon.unity-mcp-server`
背景資料: 同一仕様のゲームを **Unity CLI** と **本 MCP サーバー**で AI 実装し比較した結果
（`ArmoredTrainDefence/Docs/CLI_vs_MCP_比較.md` / `ArmoredTrainDefence2/Docs/04_開発環境レポート.md`）。

Unity CLI は公式パッケージのため手を入れられないが、**本サーバーは自作のため改善可能**。
比較で判明した MCP 側の弱点のうち、既存の `MCP_SERVER_FIX_PLAN.md`（入力シミュレート・テスト完了）で**対応済みの項目を除いた、未対処の弱点**を機能として追加する計画。

優先度: **P0=誤判定・停止を直接なくす / P1=CLI の弱点を MCP の強みに変える / P2=検証精度の底上げ**。

---

## P0-1. コンパイル結果の stale 問題を根絶する（実害最大）

### 背景（比較で判明した問題）
`import_asset(refreshAll)` 直後に `get_compile_errors` を呼ぶと、**コンパイル完了前のため前回結果を返す**。
実例では「エラーなし・テスト173件パス」と誤報告し、実際にはコンパイルエラーが残ったまま**古いアセンブリでテストが走っていた**。比較全体で最も手戻りが大きかった項目。

### 根因（コード確認済み）
- `GetCompileErrors.Execute` は `CompilationCache.GetMessages()` を**即時**返すだけで、コンパイルが「未開始／実行中／完了」のどれかという**状態を持たない**。
- `CompilationCache` は `compilationStarted`（クリア）と `assemblyCompilationFinished`（追加）を購読するのみ。リフレッシュ要求後~開始前の「保留」区間を表現できず、前回結果がそのまま stale で返る。

### 実装方針
1. **`CompilationCache` に状態機械を追加**
   - enum `CompileState { Idle, Pending, Compiling, Completed }` と `lastCompileFinishedAt` を保持。
   - `CompilationPipeline.compilationStarted` → `Compiling`、`compilationFinished`（アセンブリ個別ではなく全体完了）→ `Completed`。
   - アセットリフレッシュ／スクリプト変更を検知したら `Pending` に落とす。検知は `AssetPostprocessor.OnPostprocessAllAssets` か、後述の各ツール側でのフラグ設定で行う。
   - `EditorApplication.isCompiling` も併用してハングを防ぐ。
2. **`get_compile_errors` の戻り値に状態を必ず含める**
   - 追加フィールド: `state`（"pending"/"compiling"/"completed"）, `isStale`(bool), `finishedAt`。
   - `Pending`/`Compiling` の間は `isStale=true` を返し、クライアントが緑と誤認できないようにする。
3. **新ツール `compile_and_wait` を追加（最重要）**
   - 挙動: `AssetDatabase.Refresh()` → `CompilationPipeline.RequestScriptCompilation()` を行い、**`compilationFinished` まで非同期で待ってから**エラー一覧を返す。CLI の `recompile` → `recompile_status` 直列実行に相当。
   - ドメインリロードで `Task` が消える問題は、テストランナーと同じく **`SessionState` に途中状態を退避**し、リロード後の `[InitializeOnLoad]` で継続・完了を検出する方式にする（`TestRunSessionState` の実装を踏襲）。
   - タイムアウト（例: 120 秒）で「まだ完了せず」を返し、無応答を防ぐ。
4. **テスト実行前ゲート**
   - `run_editmode_tests` / `run_playmode_tests` の内部で、`state != Completed` または `isStale` の場合は**実行を拒否またはコンパイル完了を待ってから開始**し、古いアセンブリでの実行を封じる。

### 新ツール I/O 例
```
compile_and_wait(args: { timeoutSeconds?: number })
→ { state:"completed", hasErrors:bool, errorCount:int, warningCount:int, messages:[...], durationMs:int }
  / タイムアウト時 { state:"compiling", timedOut:true }
```

### 受け入れ基準
- リフレッシュ直後に `get_compile_errors` を呼ぶと `isStale=true` が返る（誤緑が起きない）。
- `compile_and_wait` はコンパイル完了後の**確定した**結果のみ返す。
- 故意にコンパイルエラーを残した状態でテスト開始を要求すると拒否される。

---

## P0-2. 未保存シーンのモーダル停止をなくす（自力復帰不能の解消）

### 背景（比較で判明した問題）
シーンに変更を加えたまま保存せず Play/テストへ移ると、Unity が**モーダルダイアログ**を出してエディタが 10 分以上応答不能になる。MCP は GUI を操作できず**自力で解除できない**。さらに `save_scene` 相当のツールが無く、`create_gameobject` 等の変更を永続化できない。

### 根因（コード確認済み）
- `Scene/` ツールに保存系が存在しない（`OpenScene` はあるが `SaveScene` が無い）。
- `EnterPlayMode.Execute` は `EditorApplication.isPlaying = true` にするだけで、**dirty 状態を確認していない**。未保存かつ設定次第でモーダルが出うる。

### 実装方針
1. **`save_scene` / `save_scene_as` ツールを追加**
   - `EditorSceneManager.SaveScene(activeScene)` / `SaveOpenScenes()`。`save_scene_as` はパス指定。
2. **モーダルを出さない事前ガードを共通化**
   - Play 入場・テスト実行・`open_scene` の前に `activeScene.isDirty` を検査。
   - 方針（どちらかを引数で選択可能に）:
     - `autoSave=true`: 事前に自動保存してから進む。
     - `autoSave=false`（既定）: **モーダルを出す API を呼ばず**、`{ error:"scene is dirty", hint:"call save_scene or set discard=true" }` を返して停止させる。
   - `discard=true` 指定時は `EditorSceneManager.OpenScene(current, Single)` 等で明示破棄。
   - **重要**: `SaveCurrentModifiedScenesIfUserWantsTo` のような**プロンプトを伴う API は使わない**。保存/破棄は必ず明示 API で行い、モーダルの発生源そのものを断つ。
3. **`EnterPlayMode` の修正**
   - dirty 検査を追加し、上記ガードに従う。

### 受け入れ基準
- 未保存のまま Play/テスト/シーン切替を要求しても**モーダルが出ず**、保存・破棄・エラー返却のいずれかで**必ず即応**する。
- `save_scene` で変更が永続化され、Play 往復で消えない。

---

## P1-1. 実行時・コンソールエラーを埋もれさせない（CLI の弱点を強みに）

### 背景（比較で判明した非対称性）
CLI は**シーン移動時などに大量のエラーを出力**し、その中で対処すべきエラーが**検知されず放置され実害に至る**ことがあった。MCP は逆に、表示・実行時のエラーをその場で検知・修正できていた（＝MCP の潜在的な強み）。これを**明示的な機能**にして取りこぼしをゼロに近づける。

### 実装方針（既存 `ConsoleLogCache` / `GetConsoleLogs` を拡張）
1. **チェックポイント差分の取得**
   - `get_console_logs` に `sinceToken`（前回取得位置）と `onlyErrors`/`onlyExceptions` フィルタを追加。以後は「前回以降に新規発生したエラーだけ」を取れる。
2. **状態遷移ツールに新規エラー要約を自動同梱**
   - `enter_play_mode` / `exit_play_mode` / `open_scene` / `compile_and_wait` の戻り値に、その操作**中に新規発生したエラー/例外の件数と先頭数件**を含める。シーン移動時のエラーが結果に必ず現れるようにする。
3. **（任意）エラー監視ガード**
   - `failOnNewError=true` を指定すると、操作中に新規例外が出た場合に操作結果を失敗扱いにできる。

### 受け入れ基準
- シーン遷移や Play 入退場で新規に出た例外が、当該ツールの戻り値に**必ず要約表示**される（埋もれない）。
- `onlyExceptions + sinceToken` で「直近操作で増えた例外だけ」を取得できる。

---

## P1-2. トランスポートの堅牢化（接続エラーの低減）

### 背景（比較で判明した問題）
`Unable to connect` / `Streamable HTTP error: Unexpected content type: null` / timeout が断続的に発生。**1回目失敗→2回目成功**が多く、多くはドメインリロード中のリクエストが原因と推測される。

### 実装方針
1. **リロード/コンパイル中の応答を正規化**
   - サーバーがビジー（ドメインリロード直後・コンパイル中）でも、**Content-Type を常に正しく設定**し、`null` ボディを返さない。JSON-RPC の構造化エラー（例: `-32001 server busy: domain reloading`）を返す。
2. **readiness の明示**
   - 既存 `check_status`/ping に「準備完了か（コンパイル中/リロード中でないか）」を含め、クライアントが待機判断できるようにする。
3. **（サーバー側）冪等な短時間リトライの受容**
   - ビジー応答は**同一リクエストの安全な再送**が可能である旨をレスポンスに明記。

### 受け入れ基準
- ビジー時でも `Unexpected content type: null` ではなく、**構造化されたビジーエラー**が返る。
- `check_status` でコンパイル中/リロード中を判別できる。

---

## P2-1. 押しっぱなし・ドラッグ入力（検証カバレッジの拡張）

### 背景
`simulate_ui_click` は単発クリックのみで、**押しっぱなし**や**ドラッグ**ができない。移動入力を要するゲームプレイの自動検証に不足がある。
（※比較レポートの「バランス誤診断」はこちらの指示不足が原因のため要因からは除外。ただし機能ギャップ自体は残る）

### 実装方針
1. `simulate_ui_click` に `holdMs` を追加、または `simulate_ui_press` / `simulate_ui_release` を分離。
2. ポインタのドラッグ（press→move→release）を1ツールで表現できる `simulate_ui_drag` を追加。
3. キーボードは既存の `tap` 2 段階化（`MCP_SERVER_FIX_PLAN.md` の修正）を踏まえ、`hold(durationMs)` を提供。

### 受け入れ基準
- 前進ボタンを一定時間押し続ける／UI をドラッグする操作が MCP から再現できる。

---

## P2-2. フォーカス非依存のフレーム前進（決定的検証）

### 背景
エディタ非フォーカス時に Play が進まない問題は**2回目以降は解消**したが、決定的な検証のため能動的にフレームを進められると安定する。

### 実装方針
- `step_frames(count)` ツールを追加し、`EditorApplication.QueuePlayerLoopUpdate()` を指定回数駆動して Play を確実に前進させる（`MCP_SERVER_FIX_PLAN.md` の `WaitForPlayerLoopFrameAsync` を流用）。
- スクリーンショットや状態観測の直前に呼ぶことで、静止画問題を回避。

### 受け入れ基準
- 非フォーカスでも `step_frames` で `Time.frameCount` が確実に進む。

---

## 実装順序（推奨）

1. **P0-1 コンパイル stale 根絶**（`CompilationCache` 状態機械 + `compile_and_wait` + テスト前ゲート）— 誤完了報告を止める最優先。
2. **P0-2 未保存シーンのモーダル停止解消**（`save_scene` + dirty ガード）— 停止からの人手依存を減らす。
3. **P1-1 エラー差分の自動同梱** — CLI に対する明確な優位を機能化。
4. **P1-2 トランスポート堅牢化** — ノイズ低減。
5. **P2-1 / P2-2** — 検証カバレッジの底上げ。

## 各機能が対応する比較上の弱点

| 追加機能 | 解消する弱点 |
|---|---|
| P0-1 compile_and_wait / 状態付き get_compile_errors | 「古いコンパイル結果で緑と誤認」（実害最大） |
| P0-2 save_scene + dirty ガード | 未保存モーダルで 10 分停止・シーン保存手段なし |
| P1-1 コンソールエラー差分 | （CLI 側の弱点）シーン移動時エラーの検知漏れを MCP 側で確実に拾う |
| P1-2 トランスポート堅牢化 | `Unexpected content type: null` / 接続エラー |
| P2-1 hold/drag 入力 | 押しっぱなし不可による操作検証の穴 |
| P2-2 step_frames | フォーカス依存のフレーム停止（残存リスク） |

## 実装上の共通ルール（本サーバーの既存様式に合わせる）
- 各ツールは `IMcpTool`（`Name`/`Description`/`InputSchema`/`Execute`）を実装し、`Editor/MCP/Tools/<Category>/` に配置。
- ドメインリロードを跨ぐ状態は `SessionState`（`TestRunSessionState` 参照）に退避する。
- 非同期完了は `Task<object>` + コールバック購読で表現し、必ずタイムアウトを設ける。
- 破壊的・プロンプトを伴う Editor API（モーダル発生源）は使わず、明示 API で保存/破棄する。
