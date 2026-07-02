using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;

namespace UnityMcp
{
    /// <summary>
    /// テスト実行の進行状況・結果を SessionState 経由で保持する。
    /// PlayModeテストが伴うドメインリロードを跨いでも失われない。
    /// </summary>
    internal static class TestRunSessionState
    {
        private const string RunningKeyPrefix = "UnityMcp.TestRunner.Running.";
        private const string ResultKeyPrefix = "UnityMcp.TestRunner.Result.";

        public static bool IsRunning(TestMode testMode)
        {
            return SessionState.GetBool(RunningKeyPrefix + testMode, false);
        }

        public static void MarkRunning(TestMode testMode)
        {
            SessionState.SetBool(RunningKeyPrefix + testMode, true);
            SessionState.EraseString(ResultKeyPrefix + testMode);
        }

        public static void StoreResult(TestMode testMode, TestResultSummary summary)
        {
            SessionState.SetBool(RunningKeyPrefix + testMode, false);
            SessionState.SetString(ResultKeyPrefix + testMode, JsonConvert.SerializeObject(summary));
        }

        public static object GetStatus(TestMode testMode, string toolNameToStart)
        {
            if (IsRunning(testMode))
            {
                return new TestRunStatus
                {
                    Status = "running",
                    Message = $"{testMode} tests are still running. Poll this tool again shortly."
                };
            }

            var json = SessionState.GetString(ResultKeyPrefix + testMode, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return new TestRunStatus
                {
                    Status = "not_started",
                    Message = $"No {testMode} test run found. Call '{toolNameToStart}' first."
                };
            }

            var result = JObject.Parse(json);
            result["status"] = "completed";
            return result;
        }
    }
}
