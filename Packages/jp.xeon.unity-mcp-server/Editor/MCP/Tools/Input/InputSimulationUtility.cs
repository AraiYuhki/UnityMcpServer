#if MCP_INPUT_SYSTEM
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityMcp.Tools.InputSimulation
{
    /// <summary>
    /// simulate_keyboard / simulate_mouse で共有する入力シミュレーション補助処理。
    /// </summary>
    internal static class InputSimulationUtility
    {
        private const int FrameWaitTimeoutMs = 2000;

        /// <summary>
        /// プレイヤーループが次のフレームへ進むまで待機する。
        /// エディタがバックグラウンドでもフレームが進むよう明示的に要求し続ける。
        /// </summary>
        public static async Task WaitForNextPlayerLoopFrameAsync()
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
                else
                {
                    EditorApplication.QueuePlayerLoopUpdate();
                }
            }

            EditorApplication.update += OnUpdate;
            EditorApplication.QueuePlayerLoopUpdate();

            var timeoutTask = Task.Delay(FrameWaitTimeoutMs);
            var completed = await Task.WhenAny(tcs.Task, timeoutTask);
            if (completed != tcs.Task)
            {
                EditorApplication.update -= OnUpdate;
                throw new TimeoutException(
                    "Timed out waiting for the player loop to advance. Is Play Mode running and the editor not throttled?");
            }
        }

        /// <summary>
        /// ゲームビュー非フォーカス時に入力が届かない設定のままなら警告文を返す。問題無ければnull。
        /// </summary>
        public static string CheckEditorInputBehaviorWarning()
        {
            if (InputSystem.settings.editorInputBehaviorInPlayMode ==
                InputSettings.EditorInputBehavior.PointersAndKeyboardsRespectGameViewFocus)
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
