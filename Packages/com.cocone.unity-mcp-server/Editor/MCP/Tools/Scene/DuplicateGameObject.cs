using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityMcp.Tools.Scene
{
    /// <summary>
    /// シーン上のGameObjectを複製するツール
    /// </summary>
    public class DuplicateGameObject : IMcpTool
    {
        public string Name => "duplicate_gameobject";

        public string Description =>
            "Duplicate an existing GameObject in the current scene. " +
            "The duplicated object is placed under the same parent as the original. " +
            "Optionally specify a new name for the duplicate. " +
            "Use get_scene_hierarchy first to find the correct path.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"gameObjectPath\":{\"type\":\"string\",\"description\":\"Hierarchy path of the GameObject to duplicate (e.g. 'Canvas/Panel/Button')\"}," +
            "\"newName\":{\"type\":\"string\",\"description\":\"Name for the duplicated GameObject. If omitted, Unity's default naming is used.\"}" +
            "},\"required\":[\"gameObjectPath\"]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);

            if (string.IsNullOrEmpty(parameters.GameObjectPath))
            {
                throw new InvalidOperationException("gameObjectPath is required.");
            }

            var original = FindGameObject(parameters.GameObjectPath);
            if (original == null)
            {
                throw new InvalidOperationException($"GameObject not found: '{parameters.GameObjectPath}'");
            }

            var duplicate = Object.Instantiate(original, original.transform.parent);
            ApplyName(duplicate, parameters.NewName);

            Undo.RegisterCreatedObjectUndo(duplicate, $"Duplicate GameObject '{original.name}'");
            EditorSceneManager.MarkSceneDirty(duplicate.scene);

            var originalPath = GetHierarchyPath(original.transform);
            var newPath = GetHierarchyPath(duplicate.transform);
            var result = new DuplicateGameObjectResult(originalPath, newPath, duplicate.GetInstanceID());
            return Task.FromResult<object>(result);
        }

        private static void ApplyName(GameObject go, string newName)
        {
            if (string.IsNullOrEmpty(newName))
            {
                return;
            }

            go.name = newName;
        }

        private static GameObject FindGameObject(string path)
        {
            var parts = path.Split('/');
            var scene = EditorSceneManager.GetActiveScene();

            GameObject root = null;
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.name != parts[0])
                {
                    continue;
                }

                root = go;
                break;
            }

            if (root == null)
            {
                return null;
            }

            if (parts.Length == 1)
            {
                return root;
            }

            var remaining = string.Join("/", parts, 1, parts.Length - 1);
            var child = root.transform.Find(remaining);
            return child != null ? child.gameObject : null;
        }

        private static string GetHierarchyPath(Transform t)
        {
            var path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }

            return path;
        }

        private static DuplicateGameObjectArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new DuplicateGameObjectArgs();
            }

            return JsonConvert.DeserializeObject<DuplicateGameObjectArgs>(args) ?? new DuplicateGameObjectArgs();
        }
    }

    internal class DuplicateGameObjectArgs
    {
        [JsonProperty("gameObjectPath")]
        public string GameObjectPath { get; set; } = string.Empty;

        [JsonProperty("newName")]
        public string NewName { get; set; } = string.Empty;
    }

    internal class DuplicateGameObjectResult
    {
        [JsonProperty("originalPath")]
        public string OriginalPath { get; private set; }

        [JsonProperty("newPath")]
        public string NewPath { get; private set; }

        [JsonProperty("newInstanceId")]
        public int NewInstanceId { get; private set; }

        [JsonProperty("message")]
        public string Message { get; private set; }

        public DuplicateGameObjectResult(string originalPath, string newPath, int newInstanceId)
        {
            OriginalPath = originalPath;
            NewPath = newPath;
            NewInstanceId = newInstanceId;
            Message = $"GameObject '{originalPath}' duplicated as '{newPath}'.";
        }
    }
}
