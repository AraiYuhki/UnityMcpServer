using System.Threading.Tasks;
using UnityEditor.TestTools.TestRunner.Api;

namespace UnityMcp
{
    /// <summary>
    /// Unity Test Runnerでテストを開始するツール
    /// PlayModeテストはPlay Mode移行時にドメインリロードが発生し、開始要求元のawaitが
    /// 消滅してしまうため、このツールはテストを開始した事実のみを即時返す。
    /// 結果は対応するget_*_test_resultsツールでポーリングする。
    /// </summary>
    public class RunTests : IMcpTool
    {
        public string Name { get; }
        public string Description { get; }
        public string InputSchema { get; } = "{\"type\":\"object\",\"properties\":{},\"required\":[]}";

        private readonly TestMode testMode;

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
        /// 指定されたモードでテストを開始する。完了は待たない。
        /// </summary>
        /// <returns>開始できたかどうかを示すステータス</returns>
        public Task<object> Execute(string _)
        {
            if (TestRunSessionState.IsRunning(testMode))
            {
                return Task.FromResult<object>(new TestRunStatus
                {
                    Status = "running",
                    Message = $"{testMode} tests are already running. Poll the corresponding get_*_test_results tool."
                });
            }

            TestRunSessionState.MarkRunning(testMode);
            TestRunCallbackRegistrar.Api.Execute(new ExecutionSettings
            {
                filters = new[]
                {
                    new Filter { testMode = testMode }
                }
            });

            return Task.FromResult<object>(new TestRunStatus
            {
                Status = "started",
                Message = $"{testMode} tests started. Poll the corresponding get_*_test_results tool for the outcome."
            });
        }
    }
}
