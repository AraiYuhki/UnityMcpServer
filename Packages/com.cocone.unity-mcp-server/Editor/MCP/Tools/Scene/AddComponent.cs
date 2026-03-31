using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityMcp.Tools.Scene
{
    /// <summary>
    /// 指定GameObjectにコンポーネントを追加するツール
    /// </summary>
    public class AddComponent : IMcpTool
    {
        public string Name => "add_component";

        public string Description =>
            "Add a component to a specified GameObject. " +
            "Specify the component by its type name (e.g. 'Rigidbody', 'BoxCollider', 'AudioSource'). " +
            "For custom scripts, use the full type name. " +
            "Use get_scene_hierarchy first to find the correct GameObject path.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"gameObjectPath\":{\"type\":\"string\",\"description\":\"Hierarchy path from scene root (e.g. 'Canvas/Panel/Button')\"}," +
            "\"componentType\":{\"type\":\"string\",\"description\":\"Component type name (e.g. 'Rigidbody', 'BoxCollider', 'AudioSource'). For custom scripts, use the full type name.\"}" +
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

            var go = FindGameObject(parameters.GameObjectPath);
            if (go == null)
            {
                throw new InvalidOperationException($"GameObject not found: '{parameters.GameObjectPath}'");
            }

            var type = FindComponentType(parameters.ComponentType);
            if (type == null)
            {
                throw new InvalidOperationException($"Component type not found: '{parameters.ComponentType}'");
            }

            if (!typeof(Component).IsAssignableFrom(type))
            {
                throw new InvalidOperationException($"Type '{type.FullName}' is not a Component.");
            }

            Undo.AddComponent(go, type);
            EditorSceneManager.MarkSceneDirty(go.scene);

            var result = new AddComponentResult(parameters.GameObjectPath, type.FullName);
            return Task.FromResult<object>(result);
        }

        private static AddComponentArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new AddComponentArgs();
            }

            return JsonConvert.DeserializeObject<AddComponentArgs>(args) ?? new AddComponentArgs();
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

        private static Type FindComponentType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var t in assembly.GetTypes())
                {
                    if (t.Name == typeName && typeof(Component).IsAssignableFrom(t))
                    {
                        return t;
                    }
                }
            }

            return null;
        }
    }

    internal class AddComponentArgs
    {
        [JsonProperty("gameObjectPath")]
        public string GameObjectPath { get; set; } = string.Empty;

        [JsonProperty("componentType")]
        public string ComponentType { get; set; } = string.Empty;
    }

    internal class AddComponentResult
    {
        [JsonProperty("gameObjectPath")]
        public string GameObjectPath { get; private set; }

        [JsonProperty("componentType")]
        public string ComponentType { get; private set; }

        [JsonProperty("message")]
        public string Message { get; private set; }

        public AddComponentResult(string gameObjectPath, string componentType)
        {
            GameObjectPath = gameObjectPath;
            ComponentType = componentType;
            Message = $"Component '{componentType}' added to '{gameObjectPath}'.";
        }
    }
}
