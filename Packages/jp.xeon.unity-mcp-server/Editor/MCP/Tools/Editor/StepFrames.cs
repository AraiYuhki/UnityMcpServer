using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityMcp.Tools.Console;

namespace UnityMcp.Tools.Editor
{
    /// <summary>
    /// プレイヤーループを能動的に駆動してフレームを進めるツール。
    /// エディタが非フォーカスでもPlayが停滞しないようにし、スクリーンショットや
    /// 状態観測の直前に呼ぶことで「静止した画面を観測してしまう」問題を避ける。
    /// </summary>
    public class StepFrames : IMcpTool
    {
        private const int DefaultCount = 1;
        private const int MaxCount = 600;

        public string Name => "step_frames";

        public string Description =>
            "Advance Play Mode by a given number of frames (PlayMode only). " +
            "Drives the player loop explicitly, so frames advance even when the Unity Editor window is not " +
            "focused. Call this before take_screenshot or before reading runtime state to make sure the game " +
            "actually progressed. The result reports the elapsed frames and any new console errors.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"count\":{\"type\":\"integer\",\"description\":\"Number of frames to advance (default: 1, max: 600)\",\"default\":1}," +
            "\"failOnNewError\":{\"type\":\"boolean\",\"description\":\"Fail the call if new console errors appear while stepping (default: false)\",\"default\":false}" +
            "},\"required\":[]}";

        public async Task<object> Execute(string args)
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("step_frames requires Play Mode. Enter Play Mode first.");
            }

            var parameters = ParseArgs(args);
            var count = ResolveCount(parameters.Count);
            var sinceToken = ConsoleLogCache.Checkpoint();
            var startFrame = Time.frameCount;

            await AdvanceFrames(count);

            var digest = ConsoleErrorDigest.Capture(sinceToken);
            FailOnNewErrors(parameters, digest);

            return BuildResult(startFrame, count, digest);
        }

        private static async Task AdvanceFrames(int count)
        {
            for (var i = 0; i < count; i++)
            {
                await PlayerLoopDriver.WaitForNextFrameAsync();
            }
        }

        private static StepFramesResult BuildResult(int startFrame, int requested, ConsoleErrorDigest digest)
        {
            var endFrame = Time.frameCount;
            return new StepFramesResult
            {
                Ok = true,
                RequestedFrames = requested,
                AdvancedFrames = endFrame - startFrame,
                FrameCount = endFrame,
                Time = UnityEngine.Time.time,
                NewConsoleErrors = digest,
                Message = $"Advanced {endFrame - startFrame} frame(s) to frame {endFrame}."
            };
        }

        private static void FailOnNewErrors(StepFramesArgs parameters, ConsoleErrorDigest digest)
        {
            if (!parameters.FailOnNewError || !digest.HasErrors())
            {
                return;
            }

            throw new InvalidOperationException(digest.BuildFailureMessage("step_frames"));
        }

        private static int ResolveCount(int requested)
        {
            if (requested <= 0)
            {
                return DefaultCount;
            }

            return requested > MaxCount ? MaxCount : requested;
        }

        private static StepFramesArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new StepFramesArgs();
            }

            return JsonConvert.DeserializeObject<StepFramesArgs>(args) ?? new StepFramesArgs();
        }
    }

    internal class StepFramesArgs
    {
        [JsonProperty("count")]
        public int Count { get; set; } = 1;

        [JsonProperty("failOnNewError")]
        public bool FailOnNewError { get; set; }
    }

    internal class StepFramesResult
    {
        [JsonProperty("ok")]
        public bool Ok { get; set; }

        [JsonProperty("requestedFrames")]
        public int RequestedFrames { get; set; }

        [JsonProperty("advancedFrames")]
        public int AdvancedFrames { get; set; }

        [JsonProperty("frameCount")]
        public int FrameCount { get; set; }

        [JsonProperty("time")]
        public float Time { get; set; }

        [JsonProperty("newConsoleErrors")]
        public ConsoleErrorDigest NewConsoleErrors { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
