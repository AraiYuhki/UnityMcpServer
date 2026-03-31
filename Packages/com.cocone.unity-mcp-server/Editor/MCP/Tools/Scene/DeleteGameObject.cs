using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityMcp.Tools.Scene
{
    /// <summary>
    /// シーン上のGameObjectを削除するツール
    /// </summary>
    public class DeleteGameObject : IMcpTool
    {
        public string Name => "delete_gameobject";

        public string Description =>
            "Delete a GameObject from the current scene by its hierarchy path. " +
            "This operation supports undo. " +
            "Use get_scene_hierarchy first to find the correct path.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"gameObjectPath\":{\"type\":\"string\",\"description\":\"Hierarchy path from scene root (e.g. 'Canvas/Panel/Button')\"}" +
            "},\"required\":[\"gameObjectPath\"]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);

            if (string.IsNullOrEmpty(parameters.GameObjectPath))
            {
                throw new InvalidOperationException("gameObjectPath is required.");
            }

            var go = FindGameObject(parameters.GameObjectPath);
            if (go == null)
            {
                throw new InvalidOperationException($"GameObject not found: '{parameters.GameObjectPath}'");
            }

            var scene = go.scene;
            Undo.DestroyObjectImmediate(go);
            EditorSceneManager.MarkSceneDirty(scene);

            var result = new DeleteGameObjectResult(parameters.GameObjectPath);
            return Task.FromResult<object>(result);
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

        private static DeleteGameObjectArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new DeleteGameObjectArgs();
            }

            return JsonConvert.DeserializeObject<DeleteGameObjectArgs>(args) ?? new DeleteGameObjectArgs();
        }
    }

    internal class DeleteGameObjectArgs
    {
        [JsonProperty("gameObjectPath")]
        public string GameObjectPath { get; set; } = string.Empty;
    }

    internal class DeleteGameObjectResult
    {
        [JsonProperty("deletedPath")]
        public string DeletedPath { get; private set; }

        [JsonProperty("message")]
        public string Message { get; private set; }

        public DeleteGameObjectResult(string deletedPath)
        {
            DeletedPath = deletedPath;
            Message = $"GameObject '{deletedPath}' has been deleted.";
        }
    }
}
