using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace UnityMcp.Tools.Scene
{
    /// <summary>
    /// アクティブなシーンを指定パスへ保存するツール。
    /// 一度も保存されていないシーンの永続化や、シーンの複製に使う。
    /// </summary>
    public class SaveSceneAs : IMcpTool
    {
        public string Name => "save_scene_as";

        public string Description =>
            "Save the active scene to the specified asset path without showing any dialog. " +
            "Use this for a scene that has never been saved, or to save a copy under a new path. " +
            "The path must start with 'Assets/' and end with '.unity'.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"scenePath\":{\"type\":\"string\",\"description\":\"Destination asset path (e.g. 'Assets/Scenes/Main.unity')\"}" +
            "},\"required\":[\"scenePath\"]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);
            ValidatePath(parameters.ScenePath);
            EnsureDirectoryExists(parameters.ScenePath);

            var scene = SceneManager.GetActiveScene();
            if (!EditorSceneManager.SaveScene(scene, parameters.ScenePath))
            {
                throw new InvalidOperationException(
                    $"Failed to save scene '{scene.name}' to '{parameters.ScenePath}'.");
            }

            return Task.FromResult<object>(new SaveSceneResult(new List<string> { parameters.ScenePath }));
        }

        private static void ValidatePath(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
            {
                throw new InvalidOperationException("scenePath is required.");
            }

            if (!scenePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"scenePath must start with 'Assets/': '{scenePath}'");
            }

            if (!scenePath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"scenePath must end with '.unity': '{scenePath}'");
            }
        }

        private static void EnsureDirectoryExists(string scenePath)
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(scenePath));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private static SaveSceneAsArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new SaveSceneAsArgs();
            }

            return JsonConvert.DeserializeObject<SaveSceneAsArgs>(args) ?? new SaveSceneAsArgs();
        }
    }

    internal class SaveSceneAsArgs
    {
        [JsonProperty("scenePath")]
        public string ScenePath { get; set; } = string.Empty;
    }
}
