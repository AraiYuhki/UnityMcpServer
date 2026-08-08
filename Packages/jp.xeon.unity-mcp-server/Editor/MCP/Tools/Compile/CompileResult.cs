using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityMcp.Tools.Console;

namespace UnityMcp.Tools.Compile
{
    /// <summary>
    /// get_compile_errors / compile_and_wait ツールの戻り値モデル
    /// </summary>
    public class CompileResult
    {
        /// <summary>コンパイルの進行状態（"idle" / "pending" / "compiling" / "completed"）</summary>
        [JsonProperty("state")]
        public string State { get; private set; }

        /// <summary>結果が未確定で信用できないか。trueの間は「エラーなし」と判断してはならない。</summary>
        [JsonProperty("isStale")]
        public bool IsStale { get; private set; }

        /// <summary>コンパイルエラーが存在するか</summary>
        [JsonProperty("hasErrors")]
        public bool HasErrors { get; private set; }

        /// <summary>Unityがスクリプトのコンパイル失敗を検知しているか</summary>
        [JsonProperty("compilationFailed")]
        public bool CompilationFailed { get; private set; }

        /// <summary>エラー件数</summary>
        [JsonProperty("errorCount")]
        public int ErrorCount { get; private set; }

        /// <summary>警告件数</summary>
        [JsonProperty("warningCount")]
        public int WarningCount { get; private set; }

        /// <summary>直近のコンパイル完了時刻（UTC）。未完了ならnull。</summary>
        [JsonProperty("finishedAt")]
        public DateTime? FinishedAt { get; private set; }

        /// <summary>直近のコンパイル所要時間（ミリ秒）</summary>
        [JsonProperty("durationMs")]
        public int DurationMs { get; private set; }

        /// <summary>待機がタイムアウトしたか</summary>
        [JsonProperty("timedOut")]
        public bool TimedOut { get; private set; }

        /// <summary>クライアント向けの補足メッセージ</summary>
        [JsonProperty("message")]
        public string Message { get; private set; }

        /// <summary>メッセージ一覧</summary>
        [JsonProperty("messages")]
        public List<CompileMessage> Messages { get; private set; }

        /// <summary>操作中に新規発生したコンソールエラーの要約</summary>
        [JsonProperty("newConsoleErrors")]
        public ConsoleErrorDigest NewConsoleErrors { get; set; }

        public CompileResult(List<CompileMessage> messages, CompileState state, bool timedOut = false)
        {
            Messages = messages;
            ErrorCount = messages.Count(m => m.Type == "Error");
            WarningCount = messages.Count(m => m.Type == "Warning");
            HasErrors = ErrorCount > 0;
            CompilationFailed = CompilationCache.CompilationFailed;
            State = state.ToWireString();
            IsStale = state.IsStale();
            FinishedAt = CompilationCache.FinishedAt;
            DurationMs = CompilationCache.LastDurationMs;
            TimedOut = timedOut;
            Message = BuildMessage(state, timedOut);
        }

        private static string BuildMessage(CompileState state, bool timedOut)
        {
            if (timedOut)
            {
                return "Timed out before compilation finished. The result is not final; call compile_and_wait again.";
            }

            if (state.IsStale())
            {
                return $"Compilation is '{state.ToWireString()}'. These messages are from the previous compilation " +
                       "and must not be treated as the current result. Call 'compile_and_wait' to get a settled result.";
            }

            return "Compilation result is settled.";
        }
    }
}
