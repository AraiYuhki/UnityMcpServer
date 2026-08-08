using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor.TestTools.TestRunner.Api;
using UnityMcp.Tools.Compile;
using UnityMcp.Tools.Scene;

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

        public string InputSchema { get; } =
            "{\"type\":\"object\",\"properties\":{" +
            "\"autoSave\":{\"type\":\"boolean\",\"description\":\"Save modified scenes before running instead of refusing (default: false)\",\"default\":false}," +
            "\"discard\":{\"type\":\"boolean\",\"description\":\"Discard unsaved scene changes before running instead of refusing (default: false)\",\"default\":false}," +
            "\"force\":{\"type\":\"boolean\",\"description\":\"Bypass the compile gate and run even when compilation is unsettled or failing (default: false)\",\"default\":false}" +
            "},\"required\":[]}";

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
        /// 古いアセンブリでの実行と未保存シーンのモーダル停止を事前に封じる。
        /// </summary>
        /// <returns>開始できたかどうかを示すステータス</returns>
        public Task<object> Execute(string args)
        {
            if (TestRunSessionState.IsRunning(testMode))
            {
                return Task.FromResult<object>(new TestRunStatus
                {
                    Status = "running",
                    Message = $"{testMode} tests are already running. Poll the corresponding get_*_test_results tool."
                });
            }

            var parameters = ParseArgs(args);
            CompileGate.Ensure(Name, parameters.Force);
            SceneDirtyGuard.Resolve(Name, parameters.AutoSave, parameters.Discard);

            StartRun();

            return Task.FromResult<object>(new TestRunStatus
            {
                Status = "started",
                Message = $"{testMode} tests started. Poll the corresponding get_*_test_results tool for the outcome."
            });
        }

        private void StartRun()
        {
            TestRunSessionState.MarkRunning(testMode);
            TestRunCallbackRegistrar.Api.Execute(new ExecutionSettings
            {
                filters = new[]
                {
                    new Filter { testMode = testMode }
                }
            });
        }

        private static RunTestsArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new RunTestsArgs();
            }

            return JsonConvert.DeserializeObject<RunTestsArgs>(args) ?? new RunTestsArgs();
        }
    }

    internal class RunTestsArgs
    {
        [JsonProperty("autoSave")]
        public bool AutoSave { get; set; }

        [JsonProperty("discard")]
        public bool Discard { get; set; }

        [JsonProperty("force")]
        public bool Force { get; set; }
    }
}
