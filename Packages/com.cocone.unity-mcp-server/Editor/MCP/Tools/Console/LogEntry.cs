using System;
using Newtonsoft.Json;
using UnityEngine;

namespace UnityMcp.Tools.Console
{
    /// <summary>
    /// Consoleのログエントリ1件を表すモデル
    /// </summary>
    public class LogEntry
    {
        /// <summary>ログ種別（"Log", "Warning", "Error", "Assert", "Exception"）</summary>
        [JsonProperty("type")]
        public string Type { get; private set; }

        /// <summary>ログメッセージ本文</summary>
        [JsonProperty("message")]
        public string Message { get; private set; }

        /// <summary>スタックトレース</summary>
        [JsonProperty("stackTrace")]
        public string StackTrace { get; private set; }

        /// <summary>受信日時</summary>
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; private set; }

        public static LogEntry Create(string condition, string stackTrace, LogType logType)
        {
            return new LogEntry
            {
                Type = logType.ToString(),
                Message = condition,
                StackTrace = stackTrace,
                Timestamp = DateTime.Now
            };
        }
    }
}
