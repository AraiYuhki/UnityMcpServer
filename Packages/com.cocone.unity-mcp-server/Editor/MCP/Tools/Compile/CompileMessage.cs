using Newtonsoft.Json;
using UnityEditor.Compilation;

namespace UnityMcp.Tools.Compile
{
    /// <summary>
    /// コンパイル時に発生した個別メッセージ（エラー・警告）のモデル
    /// </summary>
    public class CompileMessage
    {
        /// <summary>メッセージ種別（"Error" または "Warning"）</summary>
        [JsonProperty("type")]
        public string Type { get; private set; }

        /// <summary>メッセージ本文</summary>
        [JsonProperty("message")]
        public string Message { get; private set; }

        /// <summary>発生したファイルのパス</summary>
        [JsonProperty("file")]
        public string File { get; private set; }

        /// <summary>発生行番号</summary>
        [JsonProperty("line")]
        public int Line { get; private set; }

        /// <summary>発生列番号</summary>
        [JsonProperty("column")]
        public int Column { get; private set; }

        public static CompileMessage FromCompilerMessage(CompilerMessage source)
        {
            return new CompileMessage
            {
                Type = source.type == CompilerMessageType.Error ? "Error" : "Warning",
                Message = source.message,
                File = source.file,
                Line = source.line,
                Column = source.column
            };
        }
    }
}
