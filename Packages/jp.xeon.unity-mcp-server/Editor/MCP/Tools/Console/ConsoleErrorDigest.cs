using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace UnityMcp.Tools.Console
{
    /// <summary>
    /// 指定トークン以降に新規発生したコンソールエラー／例外の要約。
    /// 状態遷移ツール（Play入退場・シーン切替・コンパイル）の戻り値へ同梱し、
    /// 大量のログに紛れて対処すべきエラーが埋もれるのを防ぐ。
    /// </summary>
    public class ConsoleErrorDigest
    {
        /// <summary>要約対象区間の開始トークン</summary>
        [JsonProperty("sinceToken")]
        public long SinceToken { get; private set; }

        /// <summary>要約対象区間の終了トークン。次回の sinceToken に使える。</summary>
        [JsonProperty("nextToken")]
        public long NextToken { get; private set; }

        /// <summary>区間内に発生したエラー・例外・アサートの件数</summary>
        [JsonProperty("errorCount")]
        public int ErrorCount { get; private set; }

        /// <summary>区間内に発生した例外の件数</summary>
        [JsonProperty("exceptionCount")]
        public int ExceptionCount { get; private set; }

        /// <summary>キャッシュ溢れやドメインリロードで一部のログを取りこぼしたか</summary>
        [JsonProperty("truncated")]
        public bool Truncated { get; private set; }

        /// <summary>先頭数件のサンプル</summary>
        [JsonProperty("samples")]
        public List<LogEntry> Samples { get; private set; }

        /// <summary>人が読むための要約文</summary>
        [JsonProperty("message")]
        public string Message { get; private set; }

        private const int MaxSamples = 5;

        /// <summary>
        /// 指定トークン以降の新規エラーを集計する
        /// </summary>
        public static ConsoleErrorDigest Capture(long sinceToken)
        {
            var errors = CollectErrors(sinceToken);
            var exceptionCount = CountExceptions(errors);

            return new ConsoleErrorDigest
            {
                SinceToken = sinceToken,
                NextToken = ConsoleLogCache.CurrentToken,
                ErrorCount = errors.Count,
                ExceptionCount = exceptionCount,
                Truncated = ConsoleLogCache.OldestToken > sinceToken && ConsoleLogCache.Count > 0,
                Samples = Take(errors, MaxSamples),
                Message = BuildMessage(errors.Count, exceptionCount)
            };
        }

        /// <summary>
        /// 新規エラーが1件でもあるか
        /// </summary>
        public bool HasErrors()
        {
            return ErrorCount > 0;
        }

        /// <summary>
        /// failOnNewError 指定時に投げる例外メッセージを組み立てる
        /// </summary>
        public string BuildFailureMessage(string operation)
        {
            var head = Samples.Count > 0 ? Samples[0].Message : "(no sample available)";
            return $"{operation}: {ErrorCount} new console error(s) detected, " +
                   $"{ExceptionCount} of them exception(s). First: {head}";
        }

        private static List<LogEntry> CollectErrors(long sinceToken)
        {
            var result = new List<LogEntry>();
            foreach (var entry in ConsoleLogCache.GetEntriesSince(sinceToken))
            {
                if (entry.IsError())
                {
                    result.Add(entry);
                }
            }

            return result;
        }

        private static int CountExceptions(List<LogEntry> errors)
        {
            var count = 0;
            foreach (var entry in errors)
            {
                count += entry.IsException() ? 1 : 0;
            }

            return count;
        }

        private static List<LogEntry> Take(List<LogEntry> source, int maxCount)
        {
            var count = Math.Min(source.Count, maxCount);
            return source.GetRange(0, count);
        }

        private static string BuildMessage(int errorCount, int exceptionCount)
        {
            if (errorCount == 0)
            {
                return "No new console errors since the previous checkpoint.";
            }

            return $"{errorCount} new console error(s) since the previous checkpoint ({exceptionCount} exception(s)). " +
                   "Use get_console_logs with the returned nextToken to inspect the rest.";
        }
    }
}
