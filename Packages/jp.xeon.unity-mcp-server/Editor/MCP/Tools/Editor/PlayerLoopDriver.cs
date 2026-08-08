using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Tools.Editor
{
    /// <summary>
    /// プレイヤーループを能動的に駆動するための共通処理。
    /// エディタが非フォーカスだとPlayが自動で進まないことがあるため、
    /// フレーム待ち・押しっぱなしの保持は必ずこのクラス経由で行う。
    /// </summary>
    public static class PlayerLoopDriver
    {
        private const int FrameTimeoutMs = 5000;

        /// <summary>
        /// プレイヤーループが次のフレームへ進むまで待機する
        /// </summary>
        public static async Task WaitForNextFrameAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            var startFrame = Time.frameCount;

            void OnUpdate()
            {
                if (Time.frameCount <= startFrame)
                {
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                EditorApplication.update -= OnUpdate;
                tcs.TrySetResult(true);
            }

            EditorApplication.update += OnUpdate;
            EditorApplication.QueuePlayerLoopUpdate();

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(FrameTimeoutMs));
            if (completed == tcs.Task)
            {
                return;
            }

            EditorApplication.update -= OnUpdate;
            throw new TimeoutException(
                "Timed out waiting for the player loop to advance. Is Play Mode running and the editor not throttled?");
        }

        /// <summary>
        /// 指定ミリ秒のあいだフレームを進め続ける。押しっぱなし入力の保持に使う。
        /// </summary>
        /// <param name="durationMs">保持する時間（ミリ秒）</param>
        public static async Task HoldAsync(int durationMs)
        {
            if (durationMs <= 0)
            {
                await WaitForNextFrameAsync();
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < durationMs)
            {
                await WaitForNextFrameAsync();
            }
        }
    }
}
