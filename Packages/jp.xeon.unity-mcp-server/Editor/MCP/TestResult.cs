using System;
using System.Collections.Generic;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace UnityMcp
{
    /// <summary>
    /// 個別のテスト結果を表すデータクラス
    /// 失敗したテストの詳細情報を保持する
    /// </summary>
    [Serializable]
    public class TestResult
    {
        /// <summary>
        /// テストの完全修飾名（名前空間.クラス名.メソッド名）
        /// </summary>
        [SerializeField] private string fullName;

        /// <summary>
        /// 失敗時のエラーメッセージ
        /// </summary>
        [SerializeField] private string message;

        /// <summary>
        /// 失敗時のスタックトレース
        /// </summary>
        [SerializeField] private string stackTrace;

        /// <summary>
        /// テスト実行時の出力
        /// </summary>
        [SerializeField] private string output;

        /// <summary>
        /// 子テストの失敗結果（再帰的に収集）
        /// </summary>
        [SerializeField] private List<TestResult> failedResults = new();

        /// <summary>
        /// ITestResultAdaptorからTestResultを生成する
        /// 子テストがある場合は再帰的に失敗結果を収集する
        /// </summary>
        public TestResult(ITestResultAdaptor result)
        {
            fullName = result.FullName;
            message = result.Message;
            stackTrace = result.StackTrace;
            output = result.Output;

            // 子テストの失敗を再帰的に収集
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