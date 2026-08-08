using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace UnityMcp.Tools.Compile
{
    /// <summary>
    /// 直近のコンパイル結果からエラー・警告を取得するツール
    /// </summary>
    public class GetCompileErrors : IMcpTool
    {
        public string Name => "get_compile_errors";

        public string Description =>
            "Get compile errors and warnings from the last compilation. " +
            "Returns error/warning counts and detailed messages with file paths and line numbers, " +
            "plus 'state' (idle/pending/compiling/completed) and 'isStale'. " +
            "IMPORTANT: when isStale is true the messages are from the PREVIOUS compilation and must not be " +
            "treated as the current result; call 'compile_and_wait' instead to get a settled result.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"includeWarnings\":{\"type\":\"boolean\",\"description\":\"Include warnings in addition to errors (default: true)\"}" +
            "},\"required\":[]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);
            var state = CompilationCache.State;
            var allMessages = CompilationCache.GetMessages();
            var filtered = FilterMessages(allMessages, parameters.IncludeWarnings);
            return Task.FromResult<object>(new CompileResult(filtered, state));
        }

        private static GetCompileErrorsArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new GetCompileErrorsArgs();
            }

            return JsonConvert.DeserializeObject<GetCompileErrorsArgs>(args) ?? new GetCompileErrorsArgs();
        }

        private static List<CompileMessage> FilterMessages(IReadOnlyList<CompileMessage> messages, bool includeWarnings)
        {
            if (includeWarnings)
            {
                return new List<CompileMessage>(messages);
            }

            return messages.Where(m => m.Type == "Error").ToList();
        }
    }

    internal class GetCompileErrorsArgs
    {
        [JsonProperty("includeWarnings")]
        public bool IncludeWarnings { get; set; } = true;
    }
}
