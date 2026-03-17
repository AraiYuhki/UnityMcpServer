using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityMcp.Tools.Scene
{
    /// <summary>
    /// シーン上のGameObjectを別の親に移動するツール
    /// </summary>
    public class MoveGameObject : IMcpTool
    {
        public string Name => "move_gameobject";

        public string Description =>
            "Move a GameObject to a new parent in the scene hierarchy, or to the scene root. " +
            "Optionally set the sibling index to control ordering among siblings. " +
            "Use get_scene_hierarchy first to find the correct paths.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"gameObjectPath\":{\"type\":\"string\",\"description\":\"Hierarchy path of the GameObject to move (e.g. 'Canvas/Panel/Button')\"}," +
            "\"newParentPath\":{\"type\":\"string\",\"description\":\"Hierarchy path of the new parent. If omitted or null, the GameObject is moved to the scene root.\"}," +
            "\"siblingIndex\":{\"type\":\"integer\",\"description\":\"Position among siblings (0-based). If omitted, appended as the last child.\"}" +
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

            var oldPath = GetHierarchyPath(go.transform);
            var newParent = ResolveNewParent(parameters.NewParentPath);
            Undo.SetTransformParent(go.transform, newParent, $"Move GameObject '{go.name}'");

            ApplySiblingIndex(go.transform, parameters.SiblingIndex);
            EditorSceneManager.MarkSceneDirty(go.scene);

            var newPath = GetHierarchyPath(go.transform);
            var result = new MoveGameObjectResult(oldPath, newPath);
            return Task.FromResult<object>(result);
        }

        private static Transform ResolveNewParent(string newParentPath)
        {
            if (string.IsNullOrEmpty(newParentPath))
            {
                return null;
            }

            var parent = FindGameObject(newParentPath);
            if (parent == null)
            {
                throw new InvalidOperationException($"New parent GameObject not found: '{newParentPath}'");
            }

            return parent.transform;
        }

        private static void ApplySiblingIndex(Transform transform, int siblingIndex)
        {
            if (siblingIndex < 0)
            {
                return;
            }

            transform.SetSiblingIndex(siblingIndex);
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

        private static MoveGameObjectArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new MoveGameObjectArgs();
            }

            return JsonConvert.DeserializeObject<MoveGameObjectArgs>(args) ?? new MoveGameObjectArgs();
        }
    }

    internal class MoveGameObjectArgs
    {
        [JsonProperty("gameObjectPath")]
        public string GameObjectPath { get; set; } = string.Empty;

        [JsonProperty("newParentPath")]
        public string NewParentPath { get; set; } = string.Empty;

        [JsonProperty("siblingIndex")]
        public int SiblingIndex { get; set; } = -1;
    }

    internal class MoveGameObjectResult
    {
        [JsonProperty("oldPath")]
        public string OldPath { get; private set; }

        [JsonProperty("newPath")]
        public string NewPath { get; private set; }

        [JsonProperty("message")]
        public string Message { get; private set; }

        public MoveGameObjectResult(string oldPath, string newPath)
        {
            OldPath = oldPath;
            NewPath = newPath;
            Message = $"GameObject moved from '{oldPath}' to '{newPath}'.";
        }
    }
}
