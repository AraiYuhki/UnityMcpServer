using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityMcp.Tools.Compile;
using UnityMcp.Tools.Console;
using UnityMcp.Tools.Scene;

namespace UnityMcp.Tools.Editor
{
    /// <summary>
    /// Unity EditorのPlayModeを開始するツール
    /// </summary>
    public class EnterPlayMode : IMcpTool
    {
        public string Name => "enter_play_mode";

        public string Description =>
            "Enter Play Mode in the Unity Editor. " +
            "The editor will start running the game. " +
            "Refuses to start when a scene has unsaved changes (which would pop a modal dialog MCP cannot " +
            "dismiss) or when compilation is unsettled or failing; pass autoSave/discard or force to override. " +
            "The result summarises console errors recorded since the previous checkpoint. " +
            "Note that some operations are not available during Play Mode.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"autoSave\":{\"type\":\"boolean\",\"description\":\"Save modified scenes before entering instead of refusing (default: false)\",\"default\":false}," +
            "\"discard\":{\"type\":\"boolean\",\"description\":\"Discard unsaved scene changes before entering instead of refusing (default: false)\",\"default\":false}," +
            "\"force\":{\"type\":\"boolean\",\"description\":\"Bypass the compile gate and enter even when compilation is unsettled (default: false)\",\"default\":false}" +
            "},\"required\":[]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);

            if (EditorApplication.isPlaying)
            {
                return Task.FromResult<object>(BuildAlreadyPlayingResult());
            }

            CompileGate.Ensure("enter_play_mode", parameters.Force);
            var dirtyResolution = SceneDirtyGuard.Resolve("enter_play_mode", parameters.AutoSave, parameters.Discard);
            var digest = ConsoleErrorDigest.Capture(ConsoleLogCache.Checkpoint());

            EditorApplication.isPlaying = true;

            return Task.FromResult<object>(new PlayModeResult
            {
                State = "playing",
                IsPlaying = true,
                IsPaused = false,
                Message = "Entered Play Mode. Entering Play Mode triggers a domain reload; " +
                          "poll 'get_play_mode_state' if you need to confirm it settled.",
                DirtySceneResolution = dirtyResolution,
                NewConsoleErrors = digest
            });
        }

        private static PlayModeResult BuildAlreadyPlayingResult()
        {
            return new PlayModeResult
            {
                State = "playing",
                IsPlaying = true,
                IsPaused = EditorApplication.isPaused,
                Message = "Already in Play Mode.",
                NewConsoleErrors = ConsoleErrorDigest.Capture(ConsoleLogCache.Checkpoint())
            };
        }

        private static EnterPlayModeArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new EnterPlayModeArgs();
            }

            return JsonConvert.DeserializeObject<EnterPlayModeArgs>(args) ?? new EnterPlayModeArgs();
        }
    }

    internal class EnterPlayModeArgs
    {
        [JsonProperty("autoSave")]
        public bool AutoSave { get; set; }

        [JsonProperty("discard")]
        public bool Discard { get; set; }

        [JsonProperty("force")]
        public bool Force { get; set; }
    }
}
