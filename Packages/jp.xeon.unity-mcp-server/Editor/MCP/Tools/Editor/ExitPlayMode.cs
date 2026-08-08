using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityMcp.Tools.Console;

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
            "The editor will stop running the game and return to edit mode. " +
            "The result summarises the console errors and exceptions recorded since the previous checkpoint, " +
            "i.e. the ones raised while the game was running, so runtime errors cannot go unnoticed. " +
            "Pass failOnNewError=true to make the call fail when new errors were raised.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"failOnNewError\":{\"type\":\"boolean\",\"description\":\"Fail the call if new console errors were recorded during Play Mode (default: false)\",\"default\":false}" +
            "},\"required\":[]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);
            var digest = ConsoleErrorDigest.Capture(ConsoleLogCache.Checkpoint());

            if (!EditorApplication.isPlaying)
            {
                return Task.FromResult<object>(BuildResult("Already in Edit Mode.", digest));
            }

            EditorApplication.isPlaying = false;
            FailOnNewErrors(parameters, digest);

            return Task.FromResult<object>(BuildResult("Exited Play Mode.", digest));
        }

        private static PlayModeResult BuildResult(string message, ConsoleErrorDigest digest)
        {
            return new PlayModeResult
            {
                State = "editing",
                IsPlaying = false,
                IsPaused = false,
                Message = message,
                NewConsoleErrors = digest
            };
        }

        private static void FailOnNewErrors(ExitPlayModeArgs parameters, ConsoleErrorDigest digest)
        {
            if (!parameters.FailOnNewError || !digest.HasErrors())
            {
                return;
            }

            throw new InvalidOperationException(digest.BuildFailureMessage("exit_play_mode"));
        }

        private static ExitPlayModeArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new ExitPlayModeArgs();
            }

            return JsonConvert.DeserializeObject<ExitPlayModeArgs>(args) ?? new ExitPlayModeArgs();
        }
    }

    internal class ExitPlayModeArgs
    {
        [JsonProperty("failOnNewError")]
        public bool FailOnNewError { get; set; }
    }
}
