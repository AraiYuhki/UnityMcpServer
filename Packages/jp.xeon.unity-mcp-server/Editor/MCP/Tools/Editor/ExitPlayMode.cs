using System.Threading.Tasks;
using UnityEditor;

namespace UnityMcp.Tools.Editor
{
    /// <summary>
    /// Unity EditorのPlayModeを終了するツール
    /// </summary>
    public class ExitPlayMode : IMcpTool
    {
        public string Name => "exit_play_mode";

        public string Description =>
            "Exit Play Mode in the Unity Editor. " +
            "The editor will stop running the game and return to edit mode.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{},\"required\":[]}";

        public Task<object> Execute(string args)
        {
            if (!EditorApplication.isPlaying)
            {
                return Task.FromResult<object>(new PlayModeResult
                {
                    State = "editing",
                    IsPlaying = false,
                    IsPaused = false,
                    Message = "Already in Edit Mode."
                });
            }

            EditorApplication.isPlaying = false;

            return Task.FromResult<object>(new PlayModeResult
            {
                State = "editing",
                IsPlaying = false,
                IsPaused = false,
                Message = "Exited Play Mode."
            });
        }
    }
}
