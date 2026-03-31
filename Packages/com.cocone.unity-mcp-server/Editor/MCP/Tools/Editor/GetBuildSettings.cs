using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;

namespace UnityMcp.Tools.Editor
{
    /// <summary>
    /// Unityプロジェクトの現在のビルド設定を取得するツール
    /// </summary>
    public class GetBuildSettings : IMcpTool
    {
        public string Name => "get_build_settings";

        public string Description =>
            "Get the current build settings of the Unity project. " +
            "Returns target platform, scenes in build, scripting backend, and other build configuration.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{},\"required\":[]}";

        public Task<object> Execute(string args)
        {
            var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var scriptingBackend = PlayerSettings.GetScriptingBackend(targetGroup);
            var scenes = CollectBuildScenes();

            var result = new BuildSettingsResult
            {
                ProductName = PlayerSettings.productName,
                CompanyName = PlayerSettings.companyName,
                Version = PlayerSettings.bundleVersion,
                BuildTarget = buildTarget.ToString(),
                BuildTargetGroup = targetGroup.ToString(),
                ScriptingBackend = scriptingBackend.ToString(),
                Scenes = scenes
            };
            return Task.FromResult<object>(result);
        }

        private static List<BuildSceneInfo> CollectBuildScenes()
        {
            var scenes = new List<BuildSceneInfo>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                scenes.Add(new BuildSceneInfo
                {
                    Path = scene.path,
                    Enabled = scene.enabled
                });
            }

            return scenes;
        }
    }

    internal class BuildSettingsResult
    {
        [JsonProperty("productName")]
        public string ProductName { get; set; }

        [JsonProperty("companyName")]
        public string CompanyName { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("buildTarget")]
        public string BuildTarget { get; set; }

        [JsonProperty("buildTargetGroup")]
        public string BuildTargetGroup { get; set; }

        [JsonProperty("scriptingBackend")]
        public string ScriptingBackend { get; set; }

        [JsonProperty("scenes")]
        public List<BuildSceneInfo> Scenes { get; set; }
    }

    internal class BuildSceneInfo
    {
        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }
    }
}
