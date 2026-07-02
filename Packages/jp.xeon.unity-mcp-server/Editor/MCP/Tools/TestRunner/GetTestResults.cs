using System.Threading.Tasks;
using UnityEditor.TestTools.TestRunner.Api;

namespace UnityMcp
{
    /// <summary>
    /// run_*_testsで開始したテストの進行状況・結果を取得するツール
    /// </summary>
    public class GetTestResults : IMcpTool
    {
        public string Name { get; }
        public string Description { get; }
        public string InputSchema { get; } = "{\"type\":\"object\",\"properties\":{},\"required\":[]}";

        private readonly TestMode testMode;
        private readonly string runToolName;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name">ツール名</param>
        /// <param name="description">ツールの説明</param>
        /// <param name="testMode">対象のテストモード</param>
        /// <param name="runToolName">未実行時に案内する開始ツール名</param>
        public GetTestResults(string name, string description, TestMode testMode, string runToolName)
        {
            Name = name;
            Description = description;
            this.testMode = testMode;
            this.runToolName = runToolName;
        }

        /// <summary>
        /// 実行中/未実行/完了のいずれかのステータスと、完了時は結果サマリーを返す
        /// </summary>
        public Task<object> Execute(string _)
        {
            return Task.FromResult(TestRunSessionState.GetStatus(testMode, runToolName));
        }
    }
}
