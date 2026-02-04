using System;
using System.Collections.Generic;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace UnityMcp
{
    /// <summary>
    /// テスト実行結果のサマリーを表すデータクラス
    /// 成功・失敗・スキップの件数と、失敗したテストの詳細を保持する
    /// </summary>
    [Serializable]
    public class TestResultSummary
    {
        /// <summary>
        /// 成功したテストの件数
        /// </summary>
        [SerializeField] private int passCount;

        /// <summary>
        /// 失敗したテストの件数
        /// </summary>
        [SerializeField] private int failedCount;

        /// <summary>
        /// スキップされたテストの件数
        /// </summary>
        [SerializeField] private int skippedCount;

        /// <summary>
        /// 失敗したテストの詳細リスト
        /// </summary>
        [SerializeField] private List<TestResult> failedResults = new();

        /// <summary>
        /// ITestResultAdaptorからTestResultSummaryを生成する
        /// </summary>
        public TestResultSummary(ITestResultAdaptor result)
        {
            passCount = result.PassCount;
            failedCount = result.FailCount;
            skippedCount = result.SkipCount;

            // 直接の子テストから失敗結果を収集
            // 各TestResultが再帰的に子の失敗を収集する
            if (result.HasChildren)
            {
                foreach (var child in result.Children)
                {
                    if (child.TestStatus is TestStatus.Failed)
                        failedResults.Add(new TestResult(child));
                }
            }
        }
    }
}