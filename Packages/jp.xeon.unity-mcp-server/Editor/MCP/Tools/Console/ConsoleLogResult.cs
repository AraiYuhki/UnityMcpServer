using System.Collections.Generic;
using Newtonsoft.Json;

namespace UnityMcp.Tools.Console
{
    /// <summary>
    /// get_console_logs ツールの戻り値モデル
    /// </summary>
    public class ConsoleLogResult
    {
        /// <summary>フィルタリング前の対象ログ総数</summary>
        [JsonProperty("totalCount")]
        public int TotalCount { get; private set; }

        /// <summary>実際に返したログ件数</summary>
        [JsonProperty("returnedCount")]
        public int ReturnedCount { get; private set; }

        /// <summary>次回 sinceToken に指定すると、この応答以降の新規ログだけを取得できる</summary>
        [JsonProperty("nextToken")]
        public long NextToken { get; private set; }

        /// <summary>指定した sinceToken 以降のログの一部が失われているか</summary>
        [JsonProperty("truncated")]
        public bool Truncated { get; private set; }

        /// <summary>返却されたログエントリ一覧</summary>
        [JsonProperty("logs")]
        public List<LogEntry> Logs { get; private set; }

        public ConsoleLogResult(int totalCount, List<LogEntry> logs, long nextToken, bool truncated)
        {
            TotalCount = totalCount;
            ReturnedCount = logs.Count;
            NextToken = nextToken;
            Truncated = truncated;
            Logs = logs;
        }
    }
}
