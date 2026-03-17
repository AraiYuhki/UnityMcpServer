using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace UnityMcp.Tools.Scene
{
    /// <summary>
    /// 指定パスのシーンをUnity Editorで開くツール
    /// </summary>
    public class OpenScene : IMcpTool
    {
        public string Name => "open_scene";

        public string Description =>
            "Open a scene file in the Unity Editor. " +
            "Specify the scene by its asset path (e.g. 'Assets/Scenes/Main.unity'). " +
            "Supports single mode (replaces current scene) and additive mode (adds to current scene). " +
            "Use get_asset_list with filter 't:Scene' to find available scenes.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"scenePath\":{\"type\":\"string\",\"description\":\"Asset path to the scene file (e.g. 'Assets/Scenes/Main.unity')\"}," +
            "\"additive\":{\"type\":\"boolean\",\"description\":\"If true, open the scene additively without closing the current scene (default: false)\",\"default\":false}" +
            "},\"required\":[\"scenePath\"]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);

            if (string.IsNullOrEmpty(parameters.ScenePath))
            {
                throw new InvalidOperationException("scenePath is required.");
            }

            if (!parameters.ScenePath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"File is not a scene: '{parameters.ScenePath}'");
            }

            var mode = parameters.Additive
                ? OpenSceneMode.Additive
                : OpenSceneMode.Single;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                throw new InvalidOperationException("Scene open cancelled: unsaved changes were not resolved.");
            }

            var scene = EditorSceneManager.OpenScene(parameters.ScenePath, mode);
            var result = new OpenSceneResult(scene.name, scene.path, parameters.Additive);
            return Task.FromResult<object>(result);
        }

        private static OpenSceneArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new OpenSceneArgs();
            }

            return JsonConvert.DeserializeObject<OpenSceneArgs>(args) ?? new OpenSceneArgs();
        }
    }

    internal class OpenSceneArgs
    {
        [JsonProperty("scenePath")]
        public string ScenePath { get; set; } = string.Empty;

        [JsonProperty("additive")]
        public bool Additive { get; set; } = false;
    }

    internal class OpenSceneResult
    {
        [JsonProperty("sceneName")]
        public string SceneName { get; private set; }

        [JsonProperty("scenePath")]
        public string ScenePath { get; private set; }

        [JsonProperty("additive")]
        public bool Additive { get; private set; }

        [JsonProperty("message")]
        public string Message { get; private set; }

        public OpenSceneResult(string sceneName, string scenePath, bool additive)
        {
            SceneName = sceneName;
            ScenePath = scenePath;
            Additive = additive;
            Message = additive
                ? $"Scene '{sceneName}' opened additively."
                : $"Scene '{sceneName}' opened.";
        }
    }
}
