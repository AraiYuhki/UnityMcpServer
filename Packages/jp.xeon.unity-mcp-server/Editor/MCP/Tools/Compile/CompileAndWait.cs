using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Compilation;
using UnityMcp.Tools.Console;

namespace UnityMcp.Tools.Compile
{
    /// <summary>
    /// アセットをリフレッシュしてコンパイル完了まで待機し、確定した結果のみを返すツール。
    /// get_compile_errors の stale 問題（コンパイル前の前回結果を緑と誤認する）を根絶する。
    /// </summary>
    public class CompileAndWait : IMcpTool
    {
        private const int DefaultTimeoutSeconds = 120;
        private const int MaxTimeoutSeconds = 600;

        public string Name => "compile_and_wait";

        public string Description =>
            "Refresh the asset database and wait until script compilation has finished, then return the settled " +
            "compile errors and warnings. Use this instead of 'import_asset' + 'get_compile_errors', which can " +
            "return the previous (stale) result. Also reports console errors that appeared during the operation. " +
            "If compilation succeeds a domain reload may drop the HTTP response; simply call this tool again - " +
            "it is idempotent and will return the settled result immediately.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"timeoutSeconds\":{\"type\":\"integer\",\"description\":\"Maximum seconds to wait for compilation (default: 120, max: 600)\",\"default\":120}," +
            "\"refresh\":{\"type\":\"boolean\",\"description\":\"Refresh the asset database before waiting (default: true)\",\"default\":true}," +
            "\"force\":{\"type\":\"boolean\",\"description\":\"Force a full script recompilation even when nothing changed (default: false)\",\"default\":false}," +
            "\"includeWarnings\":{\"type\":\"boolean\",\"description\":\"Include warnings in addition to errors (default: true)\",\"default\":true}" +
            "},\"required\":[]}";

        public async Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);
            var sinceToken = ConsoleLogCache.Checkpoint();

            RequestCompilation(parameters);

            var timeoutMs = ResolveTimeoutSeconds(parameters.TimeoutSeconds) * 1000;
            var settled = await WaitUntilSettled(timeoutMs);

            return BuildResult(parameters, settled, sinceToken);
        }

        private static void RequestCompilation(CompileAndWaitArgs parameters)
        {
            if (parameters.Refresh)
            {
                AssetDatabase.Refresh();
            }

            if (!parameters.Force)
            {
                return;
            }

            CompilationCache.MarkPending();
            CompilationPipeline.RequestScriptCompilation();
        }

        /// <summary>
        /// コンパイルが確定するまで待機する。タイムアウトした場合はfalseを返す。
        /// </summary>
        private static async Task<bool> WaitUntilSettled(int timeoutMs)
        {
            var tcs = new TaskCompletionSource<bool>();

            // 状態の判定は必ずメインスレッドで行う必要があるため EditorApplication.update で監視する
            void OnUpdate()
            {
                if (CompilationCache.IsBusy)
                {
                    return;
                }

                EditorApplication.update -= OnUpdate;
                tcs.TrySetResult(true);
            }

            EditorApplication.update += OnUpdate;

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
            if (completed == tcs.Task)
            {
                return true;
            }

            EditorApplication.update -= OnUpdate;
            return false;
        }

        private static CompileResult BuildResult(CompileAndWaitArgs parameters, bool settled, long sinceToken)
        {
            var state = CompilationCache.State;
            var messages = FilterMessages(CompilationCache.GetMessages(), parameters.IncludeWarnings);
            var result = new CompileResult(messages, state, !settled)
            {
                NewConsoleErrors = ConsoleErrorDigest.Capture(sinceToken)
            };
            return result;
        }

        private static int ResolveTimeoutSeconds(int requested)
        {
            if (requested <= 0)
            {
                return DefaultTimeoutSeconds;
            }

            return requested > MaxTimeoutSeconds ? MaxTimeoutSeconds : requested;
        }

        private static List<CompileMessage> FilterMessages(IReadOnlyList<CompileMessage> messages, bool includeWarnings)
        {
            if (includeWarnings)
            {
                return new List<CompileMessage>(messages);
            }

            return messages.Where(m => m.Type == "Error").ToList();
        }

        private static CompileAndWaitArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new CompileAndWaitArgs();
            }

            return JsonConvert.DeserializeObject<CompileAndWaitArgs>(args) ?? new CompileAndWaitArgs();
        }
    }

    internal class CompileAndWaitArgs
    {
        [JsonProperty("timeoutSeconds")]
        public int TimeoutSeconds { get; set; } = 120;

        [JsonProperty("refresh")]
        public bool Refresh { get; set; } = true;

        [JsonProperty("force")]
        public bool Force { get; set; } = false;

        [JsonProperty("includeWarnings")]
        public bool IncludeWarnings { get; set; } = true;
    }
}
