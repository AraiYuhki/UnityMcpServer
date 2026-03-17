using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace UnityMcp.Tools.Editor
{
    /// <summary>
    /// Unityプロジェクトをビルドするツール
    /// </summary>
    public class BuildProject : IMcpTool
    {
        public string Name => "build_project";

        public string Description =>
            "Build the Unity project for the specified platform. " +
            "Returns build result with success/failure status and any error messages.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"outputPath\":{\"type\":\"string\",\"description\":\"Path for the build output (e.g. 'Builds/MyGame.exe')\"}," +
            "\"target\":{\"type\":\"string\",\"description\":\"Build target override. If omitted, uses current active target. Values: StandaloneWindows64, StandaloneOSX, StandaloneLinux64, iOS, Android, WebGL\"}," +
            "\"scenes\":{\"type\":\"array\",\"items\":{\"type\":\"string\"},\"description\":\"Scene paths to include. If omitted, uses scenes from Build Settings.\"}" +
            "},\"required\":[\"outputPath\"]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);

            if (string.IsNullOrEmpty(parameters.OutputPath))
            {
                throw new InvalidOperationException("outputPath is required.");
            }

            var buildTarget = ResolveBuildTarget(parameters.Target);
            var scenePaths = ResolveScenes(parameters.Scenes);
            EnsureOutputDirectory(parameters.OutputPath);

            var report = BuildPipeline.BuildPlayer(scenePaths, parameters.OutputPath, buildTarget, BuildOptions.None);
            var result = BuildResultFromReport(report, parameters.OutputPath, buildTarget);
            return Task.FromResult<object>(result);
        }

        private static BuildTarget ResolveBuildTarget(string target)
        {
            if (string.IsNullOrEmpty(target))
            {
                return EditorUserBuildSettings.activeBuildTarget;
            }

            if (Enum.TryParse<BuildTarget>(target, ignoreCase: true, out var parsed))
            {
                return parsed;
            }

            throw new InvalidOperationException(
                $"Invalid build target: '{target}'. " +
                "Valid values: StandaloneWindows64, StandaloneOSX, StandaloneLinux64, iOS, Android, WebGL.");
        }

        private static string[] ResolveScenes(List<string> scenes)
        {
            if (scenes != null && scenes.Count > 0)
            {
                return scenes.ToArray();
            }

            return EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();
        }

        private static void EnsureOutputDirectory(string outputPath)
        {
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private static BuildProjectResult BuildResultFromReport(BuildReport report, string outputPath, BuildTarget target)
        {
            var summary = report.summary;
            return new BuildProjectResult
            {
                OutputPath = outputPath,
                Target = target.ToString(),
                Result = summary.result.ToString(),
                TotalErrors = summary.totalErrors,
                TotalWarnings = summary.totalWarnings,
                Message = FormatMessage(summary)
            };
        }

        private static string FormatMessage(BuildSummary summary)
        {
            switch (summary.result)
            {
                case BuildResult.Succeeded:
                    return $"Build succeeded with {summary.totalWarnings} warning(s).";
                case BuildResult.Failed:
                    return $"Build failed with {summary.totalErrors} error(s) and {summary.totalWarnings} warning(s).";
                case BuildResult.Cancelled:
                    return "Build was cancelled.";
                default:
                    return $"Build finished with result: {summary.result}.";
            }
        }

        private static BuildProjectArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new BuildProjectArgs();
            }

            return JsonConvert.DeserializeObject<BuildProjectArgs>(args) ?? new BuildProjectArgs();
        }
    }

    internal class BuildProjectArgs
    {
        [JsonProperty("outputPath")]
        public string OutputPath { get; set; } = string.Empty;

        [JsonProperty("target")]
        public string Target { get; set; } = string.Empty;

        [JsonProperty("scenes")]
        public List<string> Scenes { get; set; }
    }

    internal class BuildProjectResult
    {
        [JsonProperty("outputPath")]
        public string OutputPath { get; set; }

        [JsonProperty("target")]
        public string Target { get; set; }

        [JsonProperty("result")]
        public string Result { get; set; }

        [JsonProperty("totalErrors")]
        public int TotalErrors { get; set; }

        [JsonProperty("totalWarnings")]
        public int TotalWarnings { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
