# UnityMcpServer 修正方針書

作成日: 2026-07-02
対象リポジトリ: `github.com:AraiYuhki/UnityMcpServer.git` (`Packages/jp.xeon.unity-mcp-server`)
検証環境: CyberShooting プロジェクト (PackageCache `@bb38d1c43b92`)
関連レポート: [MCP_INPUT_EMULATION_REPORT.md](MCP_INPUT_EMULATION_REPORT.md)

---

## 修正 1: simulate_keyboard / simulate_mouse がゲームに届かない (必須)

### 対象ファイル

- `Editor/MCP/Tools/Input/SimulateKeyboard.cs`
- `Editor/MCP/Tools/Input/SimulateMouse.cs`

### 現状の問題

両ツールの `QueueState` が、イベントをキューした直後に `InputSystem.Update()` を
手動で呼んでいる。

```csharp
private static void QueueState(Keyboard keyboard, Key[] keys)
{
    InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
    InputSystem.Update();   // ← 問題
}
```

MCP のリクエスト処理は `McpDispatcher.Drain()` → `EditorApplication.update`、
つまり **エディタループ上** で実行される。このコンテキストでの
`InputSystem.Update()` はエディタ用入力ステートバッファへの更新になるため、
キューしたイベントはエディタ側に消費され、プレイモード側のデバイス状態には
一切反映されない (Input System はエディタ用とプレイモード用でステートバッファを
分離している)。

実測: プレイモード中に press を実行してもゲーム側では
`Keyboard.current.zKey.isPressed == false` のまま。マウス移動も反映されない。

### 修正方針

手動 `InputSystem.Update()` を削除し、キューだけ行って次のプレイヤーループに
処理させる。エディタがバックグラウンドでもフレームが確実に回るよう
`QueuePlayerLoopUpdate` を明示する。

```csharp
private static void QueueState(Keyboard keyboard, Key[] keys)
{
    InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));

    // エディタループから InputSystem.Update() を呼ぶとエディタ用バッファに
    // 消費されてしまうため、プレイヤーループに処理を委ねる。
    EditorApplication.QueuePlayerLoopUpdate();
}
```

`SimulateMouse.QueueState` も同一の修正。

### 追随して必要になる修正: tap / click の 2 段階化

現状の `tap` (keyboard) と `click` (mouse) は press と release を同一バッチで
キューしている。修正後は両イベントが **同じプレイヤーループ更新内** で処理され、
`IsPressed()` をポーリングしているゲームロジックには「押された瞬間」が観測されない
可能性が高い (interaction 系の `WasPressedThisFrame` は拾える)。

press をキュー → プレイヤーループを 1 回以上回るのを待つ → release をキュー、
の 2 段階に分ける。`IMcpTool.Execute` は `Task<object>` を返すため await で書ける:

```csharp
private static Task WaitForPlayerLoopFrameAsync()
{
    var tcs = new TaskCompletionSource<bool>();
    var startFrame = Time.frameCount;

    void OnUpdate()
    {
        if (Time.frameCount > startFrame)
        {
            EditorApplication.update -= OnUpdate;
            tcs.TrySetResult(true);
        }
        EditorApplication.QueuePlayerLoopUpdate();
    }

    EditorApplication.update += OnUpdate;
    EditorApplication.QueuePlayerLoopUpdate();
    return tcs.Task;
}

private static async Task<SimulateKeyboardResult> TapAsync(Keyboard keyboard, Key[] keys)
{
    QueueState(keyboard, keys);
    await WaitForPlayerLoopFrameAsync();
    QueueState(keyboard, Array.Empty<Key>());
    return BuildResult("tap", keys, $"Tapped {keys.Length} key(s).");
}
```

タイムアウト (例: 2 秒でフレームが進まなければ諦めてエラーを返す) を付けると、
エディタが完全に停止している場合にリクエストが無応答になるのを防げる。

### 前提となるプロジェクト設定

`InputSettings.editorInputBehaviorInPlayMode` が既定値
(`0: Pointers And Keyboards Respect Game View Focus`) だと、ゲームビューが
非フォーカスの時点でキーボード/ポインタ入力がゲームへルーティングされない。

- 利用側プロジェクトで `1: All Device Input Always Goes To Game View` を
  設定する必要がある (CyberShooting では設定変更済み)。
- サーバー側の対応として、ツール実行時に設定を検査し、既定値のままなら
  結果メッセージに警告を含めることを推奨:

```csharp
if (InputSystem.settings.editorInputBehaviorInPlayMode
    == InputSettings.EditorInputBehavior.PointersAndKeyboardsRespectGameViewFocus)
{
    // result.Message に警告を追記
}
```

### 検証手順

1. 利用側プロジェクトの `Assets/Runtime/Scripts/Debug/InputProbeDebug.cs`
   (CyberShooting に配置済みの検証プローブ) を空 GameObject に付ける。
2. プレイモードに入り、MCP から `simulate_keyboard` press / `simulate_mouse` move
   を実行する。
3. Console の `[PROBE]` ログで `zKey=True` やマウス座標の変化を確認する。
4. `tap` 実行時に `MainShot` アクション (`IsPressed` ポーリング) が
   1 フレーム以上 true になることも確認する。

---

## 修正 2: run_playmode_tests / run_editmode_tests が完了を返せない (必須)

### 対象ファイル

- `Editor/MCP/Tools/TestRunner/RunTests.cs`

### 現状の問題 (2 件)

**(a) TestRunnerApi の生成方法が不正**

```csharp
var api = new TestRunnerApi();   // 54 行目付近
```

Unity が警告を出す通り、`TestRunnerApi` は `ScriptableObject` であり
`ScriptableObject.CreateInstance<TestRunnerApi>()` で生成しなければならない。
`new` で生成したインスタンスはコールバック (`RegisterCallbacks`) が正しく
機能しない場合があり、`completionSource` が完了せずクライアント側の
タイムアウト (実測 5 分) まで無応答になる。

**(b) PlayMode テストはドメインリロードで await が消滅する**

PlayMode テストの実行はプレイモード遷移を伴い、既定設定では
ドメインリロードが発生する。リロード時点で `TaskCompletionSource` を含む
マネージド状態がすべて破棄されるため、`await completionSource.Task` が
永遠に返らない。`beforeAssemblyReload` でのキャンセル除外guard は
この問題を解決しない (リロード後に応答する主体が存在しない)。

実測: PlayMode テスト自体は約 40〜60 秒で正常完了しているのに、
MCP クライアントへの応答は毎回 5 分タイムアウトになった。

### 修正方針

案 A (推奨・小規模): **非同期開始 + ポーリング分離**

- `run_playmode_tests` は「テストを開始した」ことだけを即時返す。
- テスト結果は `TestCallbacks.RunFinished` で
  `SessionState.SetString` (ドメインリロードを跨いで生存する) に JSON で保存する。
- 新ツール `get_test_results` を追加し、保存済み結果 (または「実行中」) を返す。
- クライアントは開始 → ポーリングの 2 段階で利用する。

案 B (大規模): HTTP リクエストをドメインリロード跨ぎで復元する仕組みを
サーバー基盤に追加する。コストが高いため、テスト実行以外にも同種の
ツール (プレイモード遷移を伴うもの) が増えるまでは案 A で十分。

いずれの案でも `new TestRunnerApi()` → `ScriptableObject.CreateInstance<TestRunnerApi>()`
の修正は必須。

---

## 修正 3: 運用上の注意点 (ドキュメント化推奨)

### エディタがバックグラウンドだとプレイモードのフレームが進まない

エディタ非フォーカス時、プレイモードの `Time.time` が 30 秒以上まったく
進まない現象を確認した (Interaction Mode のスロットリング)。この状態では
入力を正しくキューしてもゲームロジックが動かないため観測できない。

- 修正 1 の `QueuePlayerLoopUpdate` である程度緩和される。
- 確実な自動検証が必要な場合は PlayMode テスト経由 (Test Runner が
  フレームを駆動する) を推奨、とツール Description に明記する。

### simulate_ui_click は影響を受けない (未検証)

`ExecuteEvents` を直接叩く実装のため入力バックエンド非依存であり、
修正 1 の問題の影響は受けないはず。uGUI 要素のあるシーンでの動作検証は未実施。

---

## 修正チェックリスト

- [ ] SimulateKeyboard: 手動 `InputSystem.Update()` 削除 + `QueuePlayerLoopUpdate`
- [ ] SimulateMouse: 同上
- [ ] tap / click を press → 1 フレーム待機 → release の 2 段階に変更
- [ ] フレーム進行待ちにタイムアウトを付与
- [ ] `editorInputBehaviorInPlayMode` の検査と警告
- [ ] RunTests: `ScriptableObject.CreateInstance<TestRunnerApi>()` へ変更
- [ ] RunTests: 非同期開始 + `get_test_results` ポーリングへ分離 (案 A)
- [ ] InputProbeDebug で修正後の再検証 (キーボード / マウス / tap)
