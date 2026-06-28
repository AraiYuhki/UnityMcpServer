using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace UnityMcp
{
    /// <summary>
    /// Unity Test Runnerを使用してテストを実行するツール
    /// </summary>
    public class RunTests : IMcpTool
    {
        public string Name { get; }
        public string Description { get; }
        public string InputSchema { get; } = "{\"type\":\"object\",\"properties\":{},\"required\":[]}";

        private TestMode testMode;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name">ツール名</param>
        /// <param name="description">ツールの説明</param>
        /// <param name="testMode">実行するテストモード</param>
        public RunTests(string name, string description, TestMode testMode)
        {
            Name = name;
            Description = description;
            this.testMode = testMode;
        }

        /// <summary>
        /// 指定されたモードでテストを実行する
        /// </summary>
        /// <returns>テスト結果のサマリー</returns>
        public async Task<object> Execute(string _)
        {
            var completionSource = new TaskCompletionSource<object>();

            Application.exitCancellationToken.Register(() => completionSource.TrySetCanceled());

            void OnBeforeAssemblyReload() => completionSource.TrySetCanceled();
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;

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

                WaitTimeout(completionSource);

                return await completionSource.Task;
            }
            finally
            {
                AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
                api.UnregisterCallbacks(callbacks);
            }
        }

        /// <summary>
        /// タイムアウトを監視する。テスト完了・アプリ終了・ドメインリロードのいずれかで停止する
        /// </summary>
        private async void WaitTimeout(TaskCompletionSource<object> completionSource)
        {
            await Task.WhenAny(
                completionSource.Task,
                Task.Delay(TimeSpan.FromMinutes(5), Application.exitCancellationToken)
            );
            completionSource.TrySetException(new TimeoutException("Task was timeout (5 minutes)"));
        }
    }
}