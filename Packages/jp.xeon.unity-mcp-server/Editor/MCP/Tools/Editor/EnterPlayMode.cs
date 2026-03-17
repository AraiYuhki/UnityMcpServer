using System.Threading.Tasks;
using UnityEditor;

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
            "Note that some operations are not available during Play Mode.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{},\"required\":[]}";

        public Task<object> Execute(string args)
        {
            if (EditorApplication.isPlaying)
            {
                return Task.FromResult<object>(new PlayModeResult
                {
                    State = "playing",
                    IsPlaying = true,
                    IsPaused = EditorApplication.isPaused,
                    Message = "Already in Play Mode."
                });
            }

            EditorApplication.isPlaying = true;

            return Task.FromResult<object>(new PlayModeResult
            {
                State = "playing",
                IsPlaying = true,
                IsPaused = false,
                Message = "Entered Play Mode."
            });
        }
    }
}
