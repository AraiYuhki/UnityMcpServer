using System.Threading.Tasks;
using UnityEditor.TestTools.TestRunner.Api;

namespace UnityMcp
{
    /// <summary>
    /// Test Runnerのコールバックを受け取り、TaskCompletionSourceに結果を設定するクラス
    /// コールバックベースのTest Runner APIをTask/awaitパターンに変換する
    /// </summary>
    public class TestCallbacks : ICallbacks
    {
        private TaskCompletionSource<object> completionSource;

        public TestCallbacks(TaskCompletionSource<object> completionSource)
        {
            this.completionSource = completionSource;
        }

        /// <summary>
        /// テスト実行開始時に呼び出される
        /// </summary>
        public void RunStarted(ITestAdaptor testsToRun)
        {
        }

        /// <summary>
        /// 全テスト実行完了時に呼び出される
        /// 結果をTaskCompletionSourceに設定してawait側に通知する
        /// </summary>
        public void RunFinished(ITestResultAdaptor result)
        {
            completionSource.TrySetResult(new TestResultSummary(result));
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