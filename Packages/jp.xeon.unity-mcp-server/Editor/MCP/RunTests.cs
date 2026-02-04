using System;
using System.Threading.Tasks;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace UnityMcp
{
    /// <summary>
    /// Unity Test Runnerを使用してテストを実行するツール
    /// </summary>
    public static class RunTests
    {
        /// <summary>
        /// 指定されたモードでテストを実行する
        /// </summary>
        /// <param name="testMode">テストモード（EditMode/PlayMode）</param>
        /// <returns>テスト結果のサマリー</returns>
        public static async Task<object> Execute(TestMode testMode)
        {
            var completionSource = new TaskCompletionSource<object>();

            // アプリケーション終了時にタスクをキャンセル
            Application.exitCancellationToken.Register(() => completionSource.TrySetCanceled());

            var api = new TestRunnerApi();
            var callbacks = new TestCallbacks(completionSource);
            api.RegisterCallbacks(callbacks);

            try
            {
                api.Execute(new ExecutionSettings
                {
                    filters = new[]
                    {
                        new Filter { testMode = testMode }
                    }
                });

                // タイムアウト監視を開始
                WaitTimeout(completionSource);

                return await completionSource.Task;
            }
            finally
            {
                api.UnregisterCallbacks(callbacks);
            }
        }

        /// <summary>
        /// タイムアウトを監視し、5分経過後に例外を設定する
        /// </summary>
        private static async void WaitTimeout(TaskCompletionSource<object> completionSource)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), Application.exitCancellationToken);
            completionSource.TrySetException(new TimeoutException("Task was timeout (5 minutes)"));
        }
    }
}