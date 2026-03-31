using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityMcp.Tools.Scene
{
    /// <summary>
    /// 現在開いているシーンのGameObject階層を取得するツール
    /// </summary>
    public class GetSceneHierarchy : IMcpTool
    {
        public string Name => "get_scene_hierarchy";

        public string Description =>
            "Get the GameObject hierarchy of the currently open scene. " +
            "Returns a tree structure with names, paths, active state, tags, layers, and optionally component names. " +
            "Use this to understand the scene structure before writing code or diagnosing issues.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"maxDepth\":{\"type\":\"integer\",\"description\":\"Maximum hierarchy depth to return. 0 for unlimited (default: 10)\",\"default\":10}," +
            "\"includeInactive\":{\"type\":\"boolean\",\"description\":\"Include inactive GameObjects (default: true)\",\"default\":true}," +
            "\"includeComponents\":{\"type\":\"boolean\",\"description\":\"Include component name list on each GameObject (default: false)\",\"default\":false}" +
            "},\"required\":[]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);
            var scene = EditorSceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();

            var totalCount = CountAll(roots);
            var hierarchy = BuildHierarchy(roots, parameters);
            var result = new SceneHierarchyResult(scene.name, scene.path, totalCount, hierarchy);

            return Task.FromResult<object>(result);
        }

        private static GetSceneHierarchyArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new GetSceneHierarchyArgs();
            }

            return JsonConvert.DeserializeObject<GetSceneHierarchyArgs>(args) ?? new GetSceneHierarchyArgs();
        }

        private static List<GameObjectNode> BuildHierarchy(GameObject[] roots, GetSceneHierarchyArgs parameters)
        {
            var hierarchy = new List<GameObjectNode>();
            foreach (var root in roots)
            {
                if (!parameters.IncludeInactive && !root.activeSelf)
                {
                    continue;
                }

                hierarchy.Add(GameObjectNode.Build(
                    root,
                    parentPath: "",
                    currentDepth: 1,
                    maxDepth: parameters.MaxDepth,
                    includeComponents: parameters.IncludeComponents,
                    includeInactive: parameters.IncludeInactive));
            }

            return hierarchy;
        }

        private static int CountAll(GameObject[] roots)
        {
            var total = 0;
            foreach (var root in roots)
            {
                total += CountRecursive(root);
            }

            return total;
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

    internal class GetSceneHierarchyArgs
    {
        [JsonProperty("maxDepth")]
        public int MaxDepth { get; set; } = 10;

        [JsonProperty("includeInactive")]
        public bool IncludeInactive { get; set; } = true;

        [JsonProperty("includeComponents")]
        public bool IncludeComponents { get; set; } = false;
    }
}
