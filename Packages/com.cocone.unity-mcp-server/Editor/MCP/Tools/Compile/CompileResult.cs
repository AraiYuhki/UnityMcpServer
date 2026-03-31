using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace UnityMcp.Tools.Compile
{
    /// <summary>
    /// get_compile_errors ツールの戻り値モデル
    /// </summary>
    public class CompileResult
    {
        /// <summary>コンパイルエラーが存在するか</summary>
        [JsonProperty("hasErrors")]
        public bool HasErrors { get; private set; }

        /// <summary>エラー件数</summary>
        [JsonProperty("errorCount")]
        public int ErrorCount { get; private set; }

        /// <summary>警告件数</summary>
        [JsonProperty("warningCount")]
        public int WarningCount { get; private set; }

        /// <summary>メッセージ一覧</summary>
        [JsonProperty("messages")]
        public List<CompileMessage> Messages { get; private set; }

        public CompileResult(List<CompileMessage> messages)
        {
            Messages = messages;
            ErrorCount = messages.Count(m => m.Type == "Error");
            WarningCount = messages.Count(m => m.Type == "Warning");
            HasErrors = ErrorCount > 0;
        }
    }
}
