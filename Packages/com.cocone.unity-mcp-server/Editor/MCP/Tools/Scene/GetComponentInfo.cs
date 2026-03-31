using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityMcp.Tools.Scene
{
    /// <summary>
    /// 指定GameObjectにアタッチされているコンポーネントの詳細情報を返すツール
    /// </summary>
    public class GetComponentInfo : IMcpTool
    {
        public string Name => "get_component_info";

        public string Description =>
            "Get detailed information about components attached to a specified GameObject. " +
            "Specify the GameObject by its hierarchy path (e.g. 'Canvas/Panel/Button'). " +
            "Returns serialized field values for each component. " +
            "Use get_scene_hierarchy first to find the correct path.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"gameObjectPath\":{\"type\":\"string\",\"description\":\"Hierarchy path from scene root (e.g. 'Canvas/Panel/Button')\"}," +
            "\"componentType\":{\"type\":\"string\",\"description\":\"Filter by component type name (e.g. 'Button'). If omitted, all components are returned.\"}" +
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

            var components = SerializedPropertyExtractor.CollectComponentDetails(go, parameters.ComponentType);
            var result = ComponentInfoResult.Create(go, parameters.GameObjectPath, components);
            return Task.FromResult<object>(result);
        }

        private static GetComponentInfoArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new GetComponentInfoArgs();
            }

            return JsonConvert.DeserializeObject<GetComponentInfoArgs>(args) ?? new GetComponentInfoArgs();
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
    }

    internal class GetComponentInfoArgs
    {
        [JsonProperty("gameObjectPath")]
        public string GameObjectPath { get; set; } = string.Empty;

        [JsonProperty("componentType")]
        public string ComponentType { get; set; } = string.Empty;
    }
}
