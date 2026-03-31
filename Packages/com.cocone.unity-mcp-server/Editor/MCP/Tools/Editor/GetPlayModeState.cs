using System.Threading.Tasks;
using UnityEditor;

namespace UnityMcp.Tools.Editor
{
    /// <summary>
    /// Unity Editorの現在のPlayMode状態を取得するツール
    /// </summary>
    public class GetPlayModeState : IMcpTool
    {
        public string Name => "get_play_mode_state";

        public string Description =>
            "Get the current Play Mode state of the Unity Editor. " +
            "Returns whether the editor is playing, paused, or in edit mode.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{},\"required\":[]}";

        public Task<object> Execute(string args)
        {
            var isPlaying = EditorApplication.isPlaying;
            var isPaused = EditorApplication.isPaused;
            var state = DetermineState(isPlaying, isPaused);

            return Task.FromResult<object>(new PlayModeResult
            {
                State = state,
                IsPlaying = isPlaying,
                IsPaused = isPaused,
                Message = $"Editor is currently in {state} state."
            });
        }

        private static string DetermineState(bool isPlaying, bool isPaused)
        {
            if (isPlaying && isPaused)
            {
                return "paused";
            }

            if (isPlaying)
            {
                return "playing";
            }

            return "editing";
        }
    }
}
