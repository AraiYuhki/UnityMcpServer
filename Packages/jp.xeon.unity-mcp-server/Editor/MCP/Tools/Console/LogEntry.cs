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
        /// <summary>単調増加する通し番号。sinceToken による差分取得に使う。</summary>
        [JsonProperty("id")]
        public long Id { get; private set; }

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

        /// <summary>
        /// エラー・例外・アサートのいずれかであるか
        /// </summary>
        public bool IsError()
        {
            return Type == "Error" || Type == "Exception" || Type == "Assert";
        }

        /// <summary>
        /// 例外であるか
        /// </summary>
        public bool IsException()
        {
            return Type == "Exception";
        }

        public static LogEntry Create(long id, string condition, string stackTrace, LogType logType)
        {
            return new LogEntry
            {
                Id = id,
                Type = logType.ToString(),
                Message = condition,
                StackTrace = stackTrace,
                Timestamp = DateTime.Now
            };
        }
    }
}
