using Newtonsoft.Json;
using UnityEditor.TestTools.TestRunner.Api;

namespace UnityMcp
{
    /// <summary>
    /// 失敗した個別テストの情報
    /// AIが修正箇所を特定しやすいようテスト名とメッセージのみ保持する
    /// </summary>
    public class TestFailure
    {
        /// <summary>
        /// テストの完全修飾名（名前空間.クラス名.メソッド名）
        /// </summary>
        [JsonProperty("testName")]
        public string TestName { get; private set; }

        /// <summary>
        /// アサーション失敗時のメッセージ（Expected/But was等を含む）
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; private set; }

        public static TestFailure FromResult(ITestResultAdaptor result)
        {
            return new TestFailure
            {
                TestName = result.FullName,
                Message = result.Message
            };
        }
    }
}
