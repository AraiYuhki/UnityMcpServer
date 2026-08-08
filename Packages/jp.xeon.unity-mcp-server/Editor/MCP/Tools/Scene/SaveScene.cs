using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using SceneRef = UnityEngine.SceneManagement.Scene;

namespace UnityMcp.Tools.Scene
{
    /// <summary>
    /// 開いているシーンをディスクへ保存するツール。
    /// プロンプトを伴うAPIを使わないため、モーダルダイアログでエディタが停止することはない。
    /// </summary>
    public class SaveScene : IMcpTool
    {
        public string Name => "save_scene";

        public string Description =>
            "Save the currently open scene(s) to disk without showing any dialog. " +
            "Call this after modifying the scene with tools such as create_gameobject or set_component_property, " +
            "otherwise the changes are lost when entering Play Mode or switching scenes. " +
            "Set allScenes to true to save every open scene in a multi-scene setup. " +
            "For a scene that has never been saved, use 'save_scene_as' instead.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"allScenes\":{\"type\":\"boolean\",\"description\":\"Save every open scene instead of only the active one (default: false)\",\"default\":false}" +
            "},\"required\":[]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);

            if (parameters.AllScenes)
            {
                return Task.FromResult<object>(SaveAllOpenScenes());
            }

            return Task.FromResult<object>(SaveActiveScene());
        }

        private static SaveSceneResult SaveActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            RequirePath(scene);

            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Failed to save scene '{scene.name}'.");
            }

            return new SaveSceneResult(new List<string> { scene.path });
        }

        private static SaveSceneResult SaveAllOpenScenes()
        {
            var paths = CollectOpenScenePaths();

            if (!EditorSceneManager.SaveOpenScenes())
            {
                throw new InvalidOperationException("Failed to save the open scenes.");
            }

            return new SaveSceneResult(paths);
        }

        private static List<string> CollectOpenScenePaths()
        {
            var paths = new List<string>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                RequirePath(scene);
                paths.Add(scene.path);
            }

            return paths;
        }

        private static void RequirePath(SceneRef scene)
        {
            if (string.IsNullOrEmpty(scene.path))
            {
                throw new InvalidOperationException(
                    "The scene has never been saved and has no asset path. " +
                    "Call 'save_scene_as' with a scenePath instead.");
            }
        }

        private static SaveSceneArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new SaveSceneArgs();
            }

            return JsonConvert.DeserializeObject<SaveSceneArgs>(args) ?? new SaveSceneArgs();
        }
    }

    internal class SaveSceneArgs
    {
        [JsonProperty("allScenes")]
        public bool AllScenes { get; set; }
    }

    internal class SaveSceneResult
    {
        [JsonProperty("saved")]
        public bool Saved { get; private set; }

        [JsonProperty("scenePaths")]
        public List<string> ScenePaths { get; private set; }

        [JsonProperty("message")]
        public string Message { get; private set; }

        public SaveSceneResult(List<string> scenePaths)
        {
            Saved = true;
            ScenePaths = scenePaths;
            Message = $"Saved {scenePaths.Count} scene(s): {string.Join(", ", scenePaths)}";
        }
    }
}
