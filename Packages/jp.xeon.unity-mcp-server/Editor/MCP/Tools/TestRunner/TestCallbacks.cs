using UnityEditor.TestTools.TestRunner.Api;

namespace UnityMcp
{
    /// <summary>
    /// Test Runnerのコールバックを受け取り、結果をSessionStateへ保存するクラス。
    /// PlayModeテストのドメインリロードでインスタンスが失われても、
    /// [InitializeOnLoad]で再登録されるため結果保存を継続できる。
    /// </summary>
    internal class TestCallbacks : ICallbacks
    {
        /// <summary>
        /// テスト実行開始時に呼び出される
        /// </summary>
        public void RunStarted(ITestAdaptor testsToRun)
        {
        }

        /// <summary>
        /// 全テスト実行完了時に呼び出される
        /// 結果をSessionStateに保存し、get_*_test_resultsからポーリングできるようにする
        /// </summary>
        public void RunFinished(ITestResultAdaptor result)
        {
            TestRunSessionState.StoreResult(result.Test.TestMode, new TestResultSummary(result));
        }

        /// <summary>
        /// 個別テスト開始時に呼び出される
        /// </summary>
        public void TestStarted(ITestAdaptor test)
        {
        }

        /// <summary>
        /// 個別テスト完了時に呼び出される
        /// </summary>
        public void TestFinished(ITestResultAdaptor result)
        {
        }
    }
}
