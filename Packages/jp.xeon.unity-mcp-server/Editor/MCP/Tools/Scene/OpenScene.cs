using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor.SceneManagement;
using UnityMcp.Tools.Console;

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
            "If the current scene has unsaved changes the call is refused (no modal dialog is ever shown); " +
            "pass autoSave=true to save first or discard=true to drop the changes. " +
            "The result summarises console errors raised while loading the scene, so errors during scene " +
            "transitions cannot go unnoticed. " +
            "Use get_asset_list with filter 't:Scene' to find available scenes.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"scenePath\":{\"type\":\"string\",\"description\":\"Asset path to the scene file (e.g. 'Assets/Scenes/Main.unity')\"}," +
            "\"additive\":{\"type\":\"boolean\",\"description\":\"If true, open the scene additively without closing the current scene (default: false)\",\"default\":false}," +
            "\"autoSave\":{\"type\":\"boolean\",\"description\":\"Save modified scenes before opening instead of refusing (default: false)\",\"default\":false}," +
            "\"discard\":{\"type\":\"boolean\",\"description\":\"Discard unsaved changes before opening instead of refusing (default: false)\",\"default\":false}," +
            "\"failOnNewError\":{\"type\":\"boolean\",\"description\":\"Fail the call if new console errors appear while opening the scene (default: false)\",\"default\":false}" +
            "},\"required\":[\"scenePath\"]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);
            ValidateArgs(parameters);

            var dirtyResolution = SceneDirtyGuard.Resolve("open_scene", parameters.AutoSave, parameters.Discard);
            var sinceToken = ConsoleLogCache.Checkpoint();

            var mode = parameters.Additive ? OpenSceneMode.Additive : OpenSceneMode.Single;
            var scene = EditorSceneManager.OpenScene(parameters.ScenePath, mode);

            var digest = ConsoleErrorDigest.Capture(sinceToken);
            FailOnNewErrors(parameters, digest);

            var result = new OpenSceneResult(scene.name, scene.path, parameters.Additive)
            {
                DirtySceneResolution = dirtyResolution,
                NewConsoleErrors = digest
            };
            return Task.FromResult<object>(result);
        }

        private static void ValidateArgs(OpenSceneArgs parameters)
        {
            if (string.IsNullOrEmpty(parameters.ScenePath))
            {
                throw new InvalidOperationException("scenePath is required.");
            }

            if (!parameters.ScenePath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"File is not a scene: '{parameters.ScenePath}'");
            }
        }

        private static void FailOnNewErrors(OpenSceneArgs parameters, ConsoleErrorDigest digest)
        {
            if (!parameters.FailOnNewError || !digest.HasErrors())
            {
                return;
            }

            throw new InvalidOperationException(digest.BuildFailureMessage("open_scene"));
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

        [JsonProperty("autoSave")]
        public bool AutoSave { get; set; } = false;

        [JsonProperty("discard")]
        public bool Discard { get; set; } = false;

        [JsonProperty("failOnNewError")]
        public bool FailOnNewError { get; set; } = false;
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

        /// <summary>未保存の変更に対して行った処理（保存／破棄）。何も無ければnull。</summary>
        [JsonProperty("dirtySceneResolution")]
        public string DirtySceneResolution { get; set; }

        /// <summary>シーンを開く間に新規発生したコンソールエラーの要約</summary>
        [JsonProperty("newConsoleErrors")]
        public ConsoleErrorDigest NewConsoleErrors { get; set; }

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
