using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityMcp.Tools.Asset
{
    /// <summary>
    /// シーン・Prefab内のMissing Referenceを検索するツール
    /// </summary>
    public class FindMissingReferences : IMcpTool
    {
        private const int PropertyScanMaxDepth = 5;

        public string Name => "find_missing_references";

        public string Description =>
            "Scan the current scene or a specified Prefab asset for missing (broken) references. " +
            "Reports GameObjects and components that have fields referencing destroyed or missing assets. " +
            "For scene files (.unity), open the scene first and use target 'scene'.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"target\":{\"type\":\"string\",\"description\":\"Scan target. 'scene' for the current active scene, or an asset path to a Prefab (e.g. 'Assets/Prefabs/Player.prefab'). Default: 'scene'\",\"default\":\"scene\"}" +
            "},\"required\":[]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);
            var target = string.IsNullOrEmpty(parameters.Target) ? "scene" : parameters.Target;

            if (target.Equals("scene", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<object>(ScanCurrentScene());
            }

            if (target.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<object>(ScanPrefab(target));
            }

            if (target.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<object>(
                    new MissingReferenceResult(target, new List<MissingReferenceItem>())
                );
            }

            return Task.FromResult<object>(new MissingReferenceResult(target, new List<MissingReferenceItem>()));
        }

        private static FindMissingReferencesArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new FindMissingReferencesArgs();
            }

            return JsonConvert.DeserializeObject<FindMissingReferencesArgs>(args) ?? new FindMissingReferencesArgs();
        }

        private static MissingReferenceResult ScanCurrentScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var items = new List<MissingReferenceItem>();

            foreach (var root in scene.GetRootGameObjects())
            {
                ScanGameObjectTree(root, string.Empty, items);
            }

            return new MissingReferenceResult($"CurrentScene:{scene.name}", items);
        }

        private static MissingReferenceResult ScanPrefab(string assetPath)
        {
            var root = PrefabUtility.LoadPrefabContents(assetPath);
            if (root == null)
            {
                return new MissingReferenceResult(assetPath, new List<MissingReferenceItem>());
            }

            var items = new List<MissingReferenceItem>();
            try
            {
                ScanGameObjectTree(root, string.Empty, items);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return new MissingReferenceResult(assetPath, items);
        }

        private static void ScanGameObjectTree(GameObject go, string parentPath, List<MissingReferenceItem> items)
        {
            var path = string.IsNullOrEmpty(parentPath) ? go.name : $"{parentPath}/{go.name}";

            foreach (var component in go.GetComponents<Component>())
            {
                if (component == null)
                {
                    items.Add(MissingReferenceItem.ForMissingScript(path));
                    continue;
                }

                ScanComponentFields(path, component, items);
            }

            foreach (Transform child in go.transform)
            {
                ScanGameObjectTree(child.gameObject, path, items);
            }
        }

        private static void ScanComponentFields(string gameObjectPath, Component component, List<MissingReferenceItem> items)
        {
            var so = new SerializedObject(component);
            var prop = so.GetIterator();
            var enterChildren = true;

            while (prop.NextVisible(enterChildren))
            {
                enterChildren = prop.depth < PropertyScanMaxDepth;

                if (prop.propertyType != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                if (prop.objectReferenceValue == null && prop.objectReferenceInstanceIDValue != 0)
                {
                    items.Add(MissingReferenceItem.ForMissingField(gameObjectPath, component, prop.name));
                }
            }
        }
    }

    internal class FindMissingReferencesArgs
    {
        [JsonProperty("target")]
        public string Target { get; set; } = "scene";
    }
}
