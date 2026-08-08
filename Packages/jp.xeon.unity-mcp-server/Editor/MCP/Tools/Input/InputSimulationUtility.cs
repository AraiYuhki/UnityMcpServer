#if MCP_INPUT_SYSTEM
using System.Threading.Tasks;
using UnityEngine.InputSystem;
using UnityMcp.Tools.Editor;

namespace UnityMcp.Tools.InputSimulation
{
    /// <summary>
    /// simulate_keyboard / simulate_mouse で共有する入力シミュレーション補助処理。
    /// </summary>
    internal static class InputSimulationUtility
    {
        /// <summary>
        /// プレイヤーループが次のフレームへ進むまで待機する。
        /// エディタがバックグラウンドでもフレームが進むよう明示的に要求し続ける。
        /// </summary>
        public static Task WaitForNextPlayerLoopFrameAsync()
        {
            return PlayerLoopDriver.WaitForNextFrameAsync();
        }

        /// <summary>
        /// 指定ミリ秒のあいだフレームを進め続け、入力を押しっぱなしにする。
        /// </summary>
        public static Task HoldAsync(int durationMs)
        {
            return PlayerLoopDriver.HoldAsync(durationMs);
        }

        /// <summary>
        /// ゲームビュー非フォーカス時に入力が届かない設定のままなら警告文を返す。問題無ければnull。
        /// </summary>
        public static string CheckEditorInputBehaviorWarning()
        {
            if (InputSystem.settings.editorInputBehaviorInPlayMode ==
                InputSettings.EditorInputBehaviorInPlayMode.PointersAndKeyboardsRespectGameViewFocus)
            {
                return "Warning: Edit > Project Settings > Input System Package > " +
                       "'Play Mode Input Behavior' is set to respect Game View focus. " +
                       "Simulated input may not reach the game unless the Game View is focused. " +
                       "Consider switching it to 'All Device Input Always Goes To Game View'.";
            }

            return null;
        }
    }
}
#endif
