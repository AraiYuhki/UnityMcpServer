using System.Collections.Generic;
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

        /// <summary>
        /// 直近に保存された結果から、失敗またはスキップされたテストの完全修飾名を取得する。
        /// 保存された結果が無い場合は空リストを返す。
        /// </summary>
        public static List<string> GetLastNotPassedTestNames(TestMode testMode)
        {
            var names = new List<string>();
            var json = SessionState.GetString(ResultKeyPrefix + testMode, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return names;
            }

            var result = JObject.Parse(json);
            AppendStringValues(result["failures"] as JArray, names, "testName");
            AppendStringValues(result["skippedTests"] as JArray, names, null);
            return names;
        }

        private static void AppendStringValues(JArray array, List<string> destination, string fieldName)
        {
            if (array == null)
            {
                return;
            }

            foreach (var item in array)
            {
                var value = fieldName == null ? item.ToString() : item[fieldName]?.ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    destination.Add(value);
                }
            }
        }
    }
}
