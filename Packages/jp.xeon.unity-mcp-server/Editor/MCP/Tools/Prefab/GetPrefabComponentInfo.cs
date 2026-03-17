using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityMcp.Tools.Scene;

namespace UnityMcp.Tools.Prefab
{
    /// <summary>
    /// プレハブアセット内のGameObjectにアタッチされているコンポーネントの詳細情報を返すツール
    /// </summary>
    public class GetPrefabComponentInfo : IMcpTool
    {
        public string Name => "get_prefab_component_info";

        public string Description =>
            "Get detailed information about components attached to a GameObject within a Prefab asset. " +
            "Specify the Prefab by its asset path and optionally a child path within the Prefab hierarchy. " +
            "Returns serialized field values for each component. " +
            "Use get_prefab_hierarchy first to find the correct child path.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"assetPath\":{\"type\":\"string\",\"description\":\"Asset path to the Prefab (e.g. 'Assets/Prefabs/Player.prefab')\"}," +
            "\"gameObjectPath\":{\"type\":\"string\",\"description\":\"Path to a child GameObject within the Prefab (e.g. 'Body/Head'). If omitted, returns components on the root GameObject.\"}," +
            "\"componentType\":{\"type\":\"string\",\"description\":\"Filter by component type name (e.g. 'Rigidbody'). If omitted, all components are returned.\"}" +
            "},\"required\":[\"assetPath\"]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);

            if (string.IsNullOrEmpty(parameters.AssetPath))
            {
                throw new InvalidOperationException("assetPath is required.");
            }

            if (!parameters.AssetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Asset is not a Prefab: '{parameters.AssetPath}'");
            }

            var root = PrefabUtility.LoadPrefabContents(parameters.AssetPath);
            if (root == null)
            {
                throw new InvalidOperationException($"Failed to load Prefab: '{parameters.AssetPath}'");
            }

            try
            {
                var target = FindTarget(root, parameters.GameObjectPath);
                if (target == null)
                {
                    throw new InvalidOperationException(
                        $"GameObject not found in Prefab: '{parameters.GameObjectPath}'");
                }

                var displayPath = BuildDisplayPath(root, parameters.GameObjectPath);
                var components = SerializedPropertyExtractor.CollectComponentDetails(target, parameters.ComponentType);
                var result = ComponentInfoResult.Create(target, displayPath, components);
                return Task.FromResult<object>(result);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GetPrefabComponentInfoArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new GetPrefabComponentInfoArgs();
            }

            return JsonConvert.DeserializeObject<GetPrefabComponentInfoArgs>(args) ?? new GetPrefabComponentInfoArgs();
        }

        private static GameObject FindTarget(GameObject root, string childPath)
        {
            if (string.IsNullOrEmpty(childPath))
            {
                return root;
            }

            var child = root.transform.Find(childPath);
            return child != null ? child.gameObject : null;
        }

        private static string BuildDisplayPath(GameObject root, string childPath)
        {
            if (string.IsNullOrEmpty(childPath))
            {
                return root.name;
            }

            return $"{root.name}/{childPath}";
        }
    }

    internal class GetPrefabComponentInfoArgs
    {
        [JsonProperty("assetPath")]
        public string AssetPath { get; set; } = string.Empty;

        [JsonProperty("gameObjectPath")]
        public string GameObjectPath { get; set; } = string.Empty;

        [JsonProperty("componentType")]
        public string ComponentType { get; set; } = string.Empty;
    }
}
