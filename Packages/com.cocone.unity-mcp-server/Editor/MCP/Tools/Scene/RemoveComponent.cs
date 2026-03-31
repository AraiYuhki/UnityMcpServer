using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityMcp.Tools.Scene
{
    /// <summary>
    /// 指定GameObjectからコンポーネントを削除するツール
    /// </summary>
    public class RemoveComponent : IMcpTool
    {
        public string Name => "remove_component";

        public string Description =>
            "Remove a component from a specified GameObject. " +
            "Specify the component by its type name. Cannot remove Transform component. " +
            "Use get_component_info first to check which components are attached.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"gameObjectPath\":{\"type\":\"string\",\"description\":\"Hierarchy path from scene root (e.g. 'Canvas/Panel/Button')\"}," +
            "\"componentType\":{\"type\":\"string\",\"description\":\"Component type name to remove (e.g. 'Rigidbody', 'BoxCollider'). Cannot be 'Transform'.\"}" +
            "},\"required\":[\"gameObjectPath\",\"componentType\"]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);

            if (string.IsNullOrEmpty(parameters.GameObjectPath))
            {
                throw new InvalidOperationException("gameObjectPath is required.");
            }

            if (string.IsNullOrEmpty(parameters.ComponentType))
            {
                throw new InvalidOperationException("componentType is required.");
            }

            if (parameters.ComponentType == "Transform")
            {
                throw new InvalidOperationException("Cannot remove Transform component.");
            }

            var go = FindGameObject(parameters.GameObjectPath);
            if (go == null)
            {
                throw new InvalidOperationException($"GameObject not found: '{parameters.GameObjectPath}'");
            }

            var component = FindComponent(go, parameters.ComponentType);
            if (component == null)
            {
                throw new InvalidOperationException(
                    $"Component '{parameters.ComponentType}' not found on '{parameters.GameObjectPath}'");
            }

            var removedTypeName = component.GetType().FullName;
            Undo.DestroyObjectImmediate(component);
            EditorSceneManager.MarkSceneDirty(go.scene);

            var result = new RemoveComponentResult(parameters.GameObjectPath, removedTypeName);
            return Task.FromResult<object>(result);
        }

        private static RemoveComponentArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new RemoveComponentArgs();
            }

            return JsonConvert.DeserializeObject<RemoveComponentArgs>(args) ?? new RemoveComponentArgs();
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

        private static Component FindComponent(GameObject go, string typeName)
        {
            foreach (var component in go.GetComponents<Component>())
            {
                if (component == null)
                {
                    continue;
                }

                if (component.GetType().Name == typeName || component.GetType().FullName == typeName)
                {
                    return component;
                }
            }

            return null;
        }
    }

    internal class RemoveComponentArgs
    {
        [JsonProperty("gameObjectPath")]
        public string GameObjectPath { get; set; } = string.Empty;

        [JsonProperty("componentType")]
        public string ComponentType { get; set; } = string.Empty;
    }

    internal class RemoveComponentResult
    {
        [JsonProperty("gameObjectPath")]
        public string GameObjectPath { get; private set; }

        [JsonProperty("removedComponentType")]
        public string RemovedComponentType { get; private set; }

        [JsonProperty("message")]
        public string Message { get; private set; }

        public RemoveComponentResult(string gameObjectPath, string removedComponentType)
        {
            GameObjectPath = gameObjectPath;
            RemovedComponentType = removedComponentType;
            Message = $"Component '{removedComponentType}' removed from '{gameObjectPath}'.";
        }
    }
}
