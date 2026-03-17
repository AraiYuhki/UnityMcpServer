using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityMcp.Tools.Scene;

namespace UnityMcp.Tools.Prefab
{
    /// <summary>
    /// プレハブアセットのGameObject階層を取得するツール
    /// </summary>
    public class GetPrefabHierarchy : IMcpTool
    {
        public string Name => "get_prefab_hierarchy";

        public string Description =>
            "Get the GameObject hierarchy of a specified Prefab asset. " +
            "Returns a tree structure with names, paths, active state, tags, layers, and optionally component names. " +
            "Specify the Prefab by its asset path (e.g. 'Assets/Prefabs/Player.prefab'). " +
            "Use get_asset_list with filter 't:Prefab' to find available Prefabs.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"assetPath\":{\"type\":\"string\",\"description\":\"Asset path to the Prefab (e.g. 'Assets/Prefabs/Player.prefab')\"}," +
            "\"maxDepth\":{\"type\":\"integer\",\"description\":\"Maximum hierarchy depth to return. 0 for unlimited (default: 10)\",\"default\":10}," +
            "\"includeComponents\":{\"type\":\"boolean\",\"description\":\"Include component name list on each GameObject (default: false)\",\"default\":false}" +
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
                var totalCount = CountRecursive(root);
                var hierarchy = GameObjectNode.Build(
                    root,
                    parentPath: "",
                    currentDepth: 1,
                    maxDepth: parameters.MaxDepth,
                    includeComponents: parameters.IncludeComponents,
                    includeInactive: true);
                var result = new PrefabHierarchyResult(parameters.AssetPath, totalCount, hierarchy);
                return Task.FromResult<object>(result);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GetPrefabHierarchyArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new GetPrefabHierarchyArgs();
            }

            return JsonConvert.DeserializeObject<GetPrefabHierarchyArgs>(args) ?? new GetPrefabHierarchyArgs();
        }

        private static int CountRecursive(GameObject go)
        {
            var count = 1;
            foreach (Transform child in go.transform)
            {
                count += CountRecursive(child.gameObject);
            }

            return count;
        }
    }

    internal class GetPrefabHierarchyArgs
    {
        [JsonProperty("assetPath")]
        public string AssetPath { get; set; } = string.Empty;

        [JsonProperty("maxDepth")]
        public int MaxDepth { get; set; } = 10;

        [JsonProperty("includeComponents")]
        public bool IncludeComponents { get; set; } = false;
    }
}
