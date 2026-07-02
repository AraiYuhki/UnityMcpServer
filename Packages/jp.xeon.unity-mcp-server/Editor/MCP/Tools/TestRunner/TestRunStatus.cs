using Newtonsoft.Json;

namespace UnityMcp
{
    /// <summary>
    /// run_*_tests / get_*_test_results が返すテスト実行ステータス
    /// </summary>
    public class TestRunStatus
    {
        /// <summary>"started" | "running" | "not_started"</summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
