using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor.TestTools.TestRunner.Api;

namespace UnityMcp
{
    /// <summary>
    /// テスト実行結果のサマリー
    /// AIが一目で結果を把握できるフラットな構造で返す
    /// </summary>
    public class TestResultSummary
    {
        [JsonProperty("summary")]
        public string Summary { get; private set; }

        [JsonProperty("totalCount")]
        public int TotalCount { get; private set; }

        [JsonProperty("passCount")]
        public int PassCount { get; private set; }

        [JsonProperty("failCount")]
        public int FailCount { get; private set; }

        [JsonProperty("skipCount")]
        public int SkipCount { get; private set; }

        [JsonProperty("allPassed")]
        public bool AllPassed { get; private set; }

        [JsonProperty("failures")]
        public List<TestFailure> Failures { get; private set; } = new();

        /// <summary>
        /// スキップされたテストの完全修飾名。onlyFailures指定での再実行対象の特定に使う。
        /// </summary>
        [JsonProperty("skippedTests")]
        public List<string> SkippedTests { get; private set; } = new();

        public TestResultSummary(ITestResultAdaptor result)
        {
            PassCount = result.PassCount;
            FailCount = result.FailCount;
            SkipCount = result.SkipCount;
            TotalCount = PassCount + FailCount + SkipCount;
            AllPassed = FailCount == 0;
            Summary = $"{PassCount} passed, {FailCount} failed, {SkipCount} skipped ({TotalCount} total)";

            CollectResults(result);
        }

        /// <summary>
        /// テスト結果ツリーを再帰的に走査し、末端の失敗・スキップテストをフラットに収集する
        /// </summary>
        private void CollectResults(ITestResultAdaptor result)
        {
            if (!result.HasChildren)
            {
                if (result.TestStatus == TestStatus.Failed)
                {
                    Failures.Add(TestFailure.FromResult(result));
                }
                else if (result.TestStatus == TestStatus.Skipped)
                {
                    SkippedTests.Add(result.FullName);
                }
                return;
            }

            foreach (var child in result.Children)
            {
                CollectResults(child);
            }
        }
    }
}
