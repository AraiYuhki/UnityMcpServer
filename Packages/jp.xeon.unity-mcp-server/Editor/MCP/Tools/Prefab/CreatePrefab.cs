using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityMcp.Tools.Prefab
{
    /// <summary>
    /// シーン上のGameObjectからプレハブアセットを新規作成するツール
    /// </summary>
    public class CreatePrefab : IMcpTool
    {
        public string Name => "create_prefab";

        public string Description =>
            "Create a new Prefab asset from a GameObject in the current scene. " +
            "By default the original scene GameObject remains unchanged (a disconnected copy is saved as the " +
            "asset). Pass connect=true to instead behave like dragging the GameObject into the Project window: " +
            "the scene GameObject itself becomes a connected instance of the new Prefab (blue-linked), matching " +
            "the usual Editor 'turn into Prefab' workflow. " +
            "Specify the source GameObject by its hierarchy path and the destination asset path for the Prefab. " +
            "Use get_scene_hierarchy to find the correct hierarchy path.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"gameObjectPath\":{\"type\":\"string\",\"description\":\"Hierarchy path of the source GameObject in the scene (e.g. 'Canvas/Panel/Button')\"}," +
            "\"savePath\":{\"type\":\"string\",\"description\":\"Asset path to save the Prefab (e.g. 'Assets/Prefabs/Player.prefab'). Must end with '.prefab'.\"}," +
            "\"connect\":{\"type\":\"boolean\",\"description\":\"If true, connect the scene GameObject to the new Prefab as an instance (like dragging into the Project window) instead of leaving it as a disconnected copy. Default: false.\",\"default\":false}" +
            "},\"required\":[\"gameObjectPath\",\"savePath\"]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);
            ValidateArgs(parameters);

            var go = FindGameObject(parameters.GameObjectPath);
            if (go == null)
            {
                throw new InvalidOperationException($"GameObject not found: '{parameters.GameObjectPath}'");
            }

            var directory = Path.GetDirectoryName(parameters.SavePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var prefab = SavePrefab(go, parameters.SavePath, parameters.Connect, out var success);
            if (!success || prefab == null)
            {
                throw new InvalidOperationException($"Failed to create Prefab at '{parameters.SavePath}'.");
            }

            if (parameters.Connect)
            {
                EditorSceneManager.MarkSceneDirty(go.scene);
            }

            var result = new CreatePrefabResult(parameters.SavePath, go.name, parameters.Connect);
            return Task.FromResult<object>(result);
        }

        private static GameObject SavePrefab(GameObject go, string savePath, bool connect, out bool success)
        {
            if (connect)
            {
                return PrefabUtility.SaveAsPrefabAssetAndConnect(go, savePath, InteractionMode.AutomatedAction, out success);
            }

            return PrefabUtility.SaveAsPrefabAsset(go, savePath, out success);
        }

        private static CreatePrefabArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new CreatePrefabArgs();
            }

            return JsonConvert.DeserializeObject<CreatePrefabArgs>(args) ?? new CreatePrefabArgs();
        }

        private static void ValidateArgs(CreatePrefabArgs parameters)
        {
            if (string.IsNullOrEmpty(parameters.GameObjectPath))
            {
                throw new InvalidOperationException("gameObjectPath is required.");
            }

            if (string.IsNullOrEmpty(parameters.SavePath))
            {
                throw new InvalidOperationException("savePath is required.");
            }

            if (!parameters.SavePath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("savePath must end with '.prefab'.");
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
    }

    internal class CreatePrefabArgs
    {
        [JsonProperty("gameObjectPath")]
        public string GameObjectPath { get; set; } = string.Empty;

        [JsonProperty("savePath")]
        public string SavePath { get; set; } = string.Empty;

        [JsonProperty("connect")]
        public bool Connect { get; set; }
    }

    internal class CreatePrefabResult
    {
        [JsonProperty("savePath")]
        public string SavePath { get; private set; }

        [JsonProperty("gameObjectName")]
        public string GameObjectName { get; private set; }

        [JsonProperty("connected")]
        public bool Connected { get; private set; }

        [JsonProperty("message")]
        public string Message { get; private set; }

        public CreatePrefabResult(string savePath, string gameObjectName, bool connected)
        {
            SavePath = savePath;
            GameObjectName = gameObjectName;
            Connected = connected;
            Message = connected
                ? $"Prefab created at '{savePath}' and the scene GameObject '{gameObjectName}' is now a connected instance of it."
                : $"Prefab created at '{savePath}' from GameObject '{gameObjectName}'.";
        }
    }
}
