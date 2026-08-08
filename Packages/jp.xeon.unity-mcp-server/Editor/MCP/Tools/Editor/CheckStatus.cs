using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityMcp.Tools.Compile;

namespace UnityMcp.Tools.Editor
{
    /// <summary>
    /// Unity Editorとの疎通と、リクエストを処理できる状態かを返すツール。
    /// クライアントはこの結果で「待つべきか送ってよいか」を判断できる。
    /// </summary>
    public class CheckStatus : IMcpTool
    {
        public string Name => "check_status";

        public string Description =>
            "Check if the Unity Editor is running and the MCP server is responsive. " +
            "Also reports readiness: whether the editor is compiling scripts or reloading the domain, " +
            "the current compile state, and whether Play Mode is active. " +
            "When isReady is false, other tools return a retryable 'server busy' error (-32001); " +
            "wait briefly and resend, or call 'compile_and_wait'. " +
            "Call this before performing any Unity-related operations to verify connectivity.";

        public string InputSchema => "{\"type\":\"object\",\"properties\":{},\"required\":[]}";

        public Task<object> Execute(string args)
        {
            return Task.FromResult<object>(new CheckStatusResult
            {
                Ok = true,
                IsReady = EditorReadiness.IsReady,
                IsCompiling = EditorReadiness.IsCompiling,
                IsUpdating = EditorReadiness.IsUpdating,
                CompileState = CompilationCache.State.ToWireString(),
                CompilationFailed = CompilationCache.CompilationFailed,
                IsPlaying = EditorApplication.isPlaying,
                IsPaused = EditorApplication.isPaused,
                BusyReason = EditorReadiness.BusyReason,
                UnityVersion = UnityEngine.Application.unityVersion
            });
        }
    }

    internal class CheckStatusResult
    {
        /// <summary>サーバーが応答可能か（このレスポンスが返っている時点で常にtrue）</summary>
        [JsonProperty("ok")]
        public bool Ok { get; set; }

        /// <summary>通常のツール呼び出しを受け付けられる状態か</summary>
        [JsonProperty("isReady")]
        public bool IsReady { get; set; }

        /// <summary>スクリプトのコンパイル中か</summary>
        [JsonProperty("isCompiling")]
        public bool IsCompiling { get; set; }

        /// <summary>アセットデータベースの更新中（ドメインリロード直後を含む）か</summary>
        [JsonProperty("isUpdating")]
        public bool IsUpdating { get; set; }

        /// <summary>コンパイルの進行状態</summary>
        [JsonProperty("compileState")]
        public string CompileState { get; set; }

        /// <summary>Unityがスクリプトのコンパイル失敗を検知しているか</summary>
        [JsonProperty("compilationFailed")]
        public bool CompilationFailed { get; set; }

        /// <summary>PlayMode中か</summary>
        [JsonProperty("isPlaying")]
        public bool IsPlaying { get; set; }

        /// <summary>一時停止中か</summary>
        [JsonProperty("isPaused")]
        public bool IsPaused { get; set; }

        /// <summary>ビジーの理由。準備完了ならnull。</summary>
        [JsonProperty("busyReason")]
        public string BusyReason { get; set; }

        /// <summary>Unityのバージョン</summary>
        [JsonProperty("unityVersion")]
        public string UnityVersion { get; set; }
    }
}
