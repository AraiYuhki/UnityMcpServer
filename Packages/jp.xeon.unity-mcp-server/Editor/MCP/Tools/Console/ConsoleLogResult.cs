using System.Collections.Generic;
using Newtonsoft.Json;

namespace UnityMcp.Tools.Console
{
    /// <summary>
    /// get_console_logs ツールの戻り値モデル
    /// </summary>
    public class ConsoleLogResult
    {
        /// <summary>フィルタリング前のキャッシュ内ログ総数</summary>
        [JsonProperty("totalCount")]
        public int TotalCount { get; private set; }

        /// <summary>実際に返したログ件数</summary>
        [JsonProperty("returnedCount")]
        public int ReturnedCount { get; private set; }

        /// <summary>返却されたログエントリ一覧</summary>
        [JsonProperty("logs")]
        public List<LogEntry> Logs { get; private set; }

        public ConsoleLogResult(int totalCount, List<LogEntry> logs)
        {
            TotalCount = totalCount;
            ReturnedCount = logs.Count;
            Logs = logs;
        }
    }
}
