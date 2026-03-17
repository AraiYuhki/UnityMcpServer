using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityMcp.Tools.Scene
{
    /// <summary>
    /// シーン上に新しいGameObjectを作成するツール
    /// </summary>
    public class CreateGameObject : IMcpTool
    {
        public string Name => "create_gameobject";

        public string Description =>
            "Create a new empty GameObject in the current scene. " +
            "Optionally specify a parent by hierarchy path, set tag/layer, and add components by type name. " +
            "Use get_scene_hierarchy to find valid parent paths.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"name\":{\"type\":\"string\",\"description\":\"Name of the new GameObject\"}," +
            "\"parentPath\":{\"type\":\"string\",\"description\":\"Hierarchy path of the parent GameObject (e.g. 'Canvas/Panel'). If omitted, created at scene root.\"}," +
            "\"tag\":{\"type\":\"string\",\"description\":\"Tag to assign (e.g. 'Player'). Must be a tag defined in the project.\"}," +
            "\"layer\":{\"type\":\"string\",\"description\":\"Layer name to assign (e.g. 'UI'). Must be a layer defined in the project.\"}," +
            "\"components\":{\"type\":\"array\",\"items\":{\"type\":\"string\"},\"description\":\"Component type names to add (e.g. ['BoxCollider','Rigidbody']).\"}" +
            "},\"required\":[\"name\"]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);

            if (string.IsNullOrEmpty(parameters.Name))
            {
                throw new InvalidOperationException("name is required.");
            }

            var go = new GameObject(parameters.Name);
            SetParent(go, parameters.ParentPath);
            SetTag(go, parameters.Tag);
            SetLayer(go, parameters.Layer);
            AddComponents(go, parameters.Components);

            Undo.RegisterCreatedObjectUndo(go, $"Create GameObject '{parameters.Name}'");
            EditorSceneManager.MarkSceneDirty(go.scene);

            var result = new CreateGameObjectResult(go.name, GetHierarchyPath(go.transform), go.GetInstanceID());
            return Task.FromResult<object>(result);
        }

        private static void SetParent(GameObject go, string parentPath)
        {
            if (string.IsNullOrEmpty(parentPath))
            {
                return;
            }

            var parent = FindGameObject(parentPath);
            if (parent == null)
            {
                throw new InvalidOperationException($"Parent GameObject not found: '{parentPath}'");
            }

            go.transform.SetParent(parent.transform, false);
        }

        private static void SetTag(GameObject go, string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return;
            }

            go.tag = tag;
        }

        private static void SetLayer(GameObject go, string layerName)
        {
            if (string.IsNullOrEmpty(layerName))
            {
                return;
            }

            var layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                throw new InvalidOperationException($"Layer not found: '{layerName}'");
            }

            go.layer = layer;
        }

        private static void AddComponents(GameObject go, List<string> componentNames)
        {
            if (componentNames == null || componentNames.Count == 0)
            {
                return;
            }

            foreach (var typeName in componentNames)
            {
                var type = ResolveComponentType(typeName);
                if (type == null)
                {
                    throw new InvalidOperationException($"Component type not found: '{typeName}'");
                }

                go.AddComponent(type);
            }
        }

        private static Type ResolveComponentType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }

            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(SafeGetTypes)
                .FirstOrDefault(t => typeof(Component).IsAssignableFrom(t) &&
                                     (t.Name == typeName || t.FullName == typeName));
        }

        private static Type[] SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException)
            {
                return Array.Empty<Type>();
            }
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

        private static CreateGameObjectArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new CreateGameObjectArgs();
            }

            return JsonConvert.DeserializeObject<CreateGameObjectArgs>(args) ?? new CreateGameObjectArgs();
        }
    }

    internal class CreateGameObjectArgs
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("parentPath")]
        public string ParentPath { get; set; } = string.Empty;

        [JsonProperty("tag")]
        public string Tag { get; set; } = string.Empty;

        [JsonProperty("layer")]
        public string Layer { get; set; } = string.Empty;

        [JsonProperty("components")]
        public List<string> Components { get; set; }
    }

    internal class CreateGameObjectResult
    {
        [JsonProperty("name")]
        public string Name { get; private set; }

        [JsonProperty("path")]
        public string Path { get; private set; }

        [JsonProperty("instanceId")]
        public int InstanceId { get; private set; }

        [JsonProperty("message")]
        public string Message { get; private set; }

        public CreateGameObjectResult(string name, string path, int instanceId)
        {
            Name = name;
            Path = path;
            InstanceId = instanceId;
            Message = $"GameObject '{name}' created at '{path}'.";
        }
    }
}
