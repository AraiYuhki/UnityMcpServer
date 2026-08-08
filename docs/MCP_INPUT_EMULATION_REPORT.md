# MCP サーバー入力エミュレーション機能 検証レポート

検証日: 2026-07-02
対象: `jp.xeon.unity-mcp-server` (PackageCache: `@bb38d1c43b92`)
検証ツール: `simulate_keyboard` / `simulate_mouse` / `simulate_ui_click`

## 結論

| ツール | API 応答 | 実際のゲームへの入力反映 | 判定 |
|---|---|---|---|
| `simulate_keyboard` | 正常 (`ok: true`) | **反映されない** | **NG** |
| `simulate_mouse` | 正常 (`ok: true`) | **反映されない** | **NG** |
| `simulate_ui_click` | 未検証 | 未検証 (シーンに uGUI 要素が無いため) | 保留 |

## 検証方法

1. クリーンな SampleScene でプレイモードに入る。
2. シーンに検証用プローブ (`Assets/Runtime/Scripts/Debug/InputProbeDebug.cs`) を配置。
   `Keyboard.current` / `Mouse.current` のデバイス生状態と、`InputSystem_Actions`
   経由のアクション値 (`Player.Move` / `Player.MainShot`) を 0.5 秒間隔でログ出力する。
3. MCP 経由で `simulate_keyboard` (Z + LeftArrow を press) と
   `simulate_mouse` (move to 400,300) を実行し、ログを確認。

### 結果ログ (抜粋)

```
[PROBE] t=22.2 zKey=False leftArrow=False MainShot=False Move=(0.00, 0.00) mousePos=(0.00, 0.00) mouseLeft=False
```

press 実行後もデバイス状態・アクション値ともに一切変化しなかった。

## 注意: 検証中の誤検知について

当初「Z 押下でビームが発射された」ように見えたが、これは失敗した PlayMode テスト
ランの残骸 (`AutoFireDebug` GameObject がシーンに保存されていた) による自動発射で、
キー入力とは無関係だった。シーンファイルからは除去済み。

## 原因分析

`SimulateKeyboard.cs` の実装:

```csharp
private static void QueueState(Keyboard keyboard, Key[] keys)
{
    InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
    InputSystem.Update();   // ← ここが問題
}
```

MCP サーバーのリクエスト処理は `EditorApplication.update` (エディタループ) 上で
実行される。そのコンテキストで `InputSystem.Update()` を手動呼び出しすると、
キューしたイベントは **エディタ用の入力ステートバッファ** に消費され、
プレイモード (ゲームビュー) 側のステートには反映されない。
Input System はエディタ用とプレイモード用でステートバッファを分離しているため、
ゲーム側の `Keyboard.current.zKey.isPressed` は false のままになる。

また、当初プロジェクト設定が `m_EditorInputBehaviorInPlayMode: 0`
(Pointers And Keyboards Respect Game View Focus) だったため、ゲームビューが
非フォーカスの場合はそもそもキーボード/ポインタ入力がゲームへルーティングされない
問題もあった (検証中に `1: All Device Input Always Goes To Game View` へ変更済み。
この設定だけでは解決しないことも確認済み)。

## 修正の推奨

`SimulateKeyboard` / `SimulateMouse` の `QueueState` から
**手動の `InputSystem.Update()` 呼び出しを削除**し、イベントをキューだけして
次のプレイヤーループに処理させる:

```csharp
private static void QueueState(Keyboard keyboard, Key[] keys)
{
    InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
    // 手動 Update はエディタバッファに消費されるため呼ばない。
    // エディタがバックグラウンドでも次フレームが確実に回るよう明示的に要求する。
    UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
}
```

補足の推奨事項:

1. **`m_EditorInputBehaviorInPlayMode: 1` を維持する** (変更済み)。
   0 のままだとゲームビュー非フォーカス時に入力がゲームへ届かない。
2. `tap` アクションは press と release を同一フレームでキューしているため、
   修正後は「押した状態」が 1 プレイヤーループ内で相殺され、
   `WasPressedThisFrame` にしか反応しない可能性がある。
   press → 1 フレーム待機 → release の 2 段階に分けるのが安全。
3. エディタがバックグラウンドの場合、プレイモードのフレーム進行自体が
   停止・スロットリングされることがある (検証中に `Time.time` が 30 秒以上
   0.00 のまま進まない現象を確認)。入力検証を自動化する場合は
   PlayMode テスト経由 (Test Runner がフレームを駆動する) が確実。
4. `simulate_ui_click` は `ExecuteEvents` 直叩きのため入力バックエンドに
   依存せず、理論上は上記の問題の影響を受けない。uGUI 要素のあるシーンで
   別途検証を推奨。

## 検証に使ったプローブ

`Assets/Runtime/Scripts/Debug/InputProbeDebug.cs` を残してある。
シーンに空 GameObject を作って本コンポーネントを付け、プレイモード中に
MCP から入力を送って Console の `[PROBE]` ログを見れば、修正後の再検証が
そのまま行える。検証が済んだら削除してよい。
