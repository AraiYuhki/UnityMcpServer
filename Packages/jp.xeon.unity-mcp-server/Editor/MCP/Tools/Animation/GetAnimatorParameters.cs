using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityMcp.Tools.Animation
{
    /// <summary>
    /// AnimatorControllerに定義されたパラメータ一覧を返すツール
    /// </summary>
    public class GetAnimatorParameters : IMcpTool
    {
        public string Name => "get_animator_parameters";

        public string Description =>
            "Get all parameters defined in an Animator Controller. " +
            "Specify by scene GameObject path (with Animator component) or by direct asset path to an AnimatorController. " +
            "Returns parameter names, types, and default values. " +
            "Use get_scene_hierarchy or get_asset_list to find the correct path first.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"gameObjectPath\":{\"type\":\"string\",\"description\":\"Hierarchy path to a GameObject with an Animator component (e.g. 'Character/Model')\"}," +
            "\"assetPath\":{\"type\":\"string\",\"description\":\"Direct asset path to a .controller file (e.g. 'Assets/Animations/PlayerController.controller')\"}" +
            "}}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);
            var controller = ResolveController(parameters);
            var parameterInfos = CollectParameters(controller);

            var result = new GetAnimatorParametersResult
            {
                ControllerName = controller.name,
                Parameters = parameterInfos
            };

            return Task.FromResult<object>(result);
        }

        private static GetAnimatorParametersArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new GetAnimatorParametersArgs();
            }

            return JsonConvert.DeserializeObject<GetAnimatorParametersArgs>(args)
                   ?? new GetAnimatorParametersArgs();
        }

        private static AnimatorController ResolveController(GetAnimatorParametersArgs parameters)
        {
            if (!string.IsNullOrEmpty(parameters.AssetPath))
            {
                return LoadControllerFromAsset(parameters.AssetPath);
            }

            if (!string.IsNullOrEmpty(parameters.GameObjectPath))
            {
                return LoadControllerFromGameObject(parameters.GameObjectPath);
            }

            throw new InvalidOperationException(
                "At least one of 'assetPath' or 'gameObjectPath' is required.");
        }

        private static AnimatorController LoadControllerFromAsset(string assetPath)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);
            if (controller == null)
            {
                throw new InvalidOperationException(
                    $"AnimatorController not found at path: '{assetPath}'");
            }

            return controller;
        }

        private static AnimatorController LoadControllerFromGameObject(string gameObjectPath)
        {
            var go = FindGameObject(gameObjectPath);
            if (go == null)
            {
                throw new InvalidOperationException(
                    $"GameObject not found: '{gameObjectPath}'");
            }

            var animator = go.GetComponent<Animator>();
            if (animator == null)
            {
                throw new InvalidOperationException(
                    $"No Animator component found on '{gameObjectPath}'");
            }

            var controller = animator.runtimeAnimatorController as AnimatorController;
            if (controller == null)
            {
                throw new InvalidOperationException(
                    $"No AnimatorController assigned to Animator on '{gameObjectPath}'");
            }

            return controller;
        }

        private static List<AnimatorParameterInfo> CollectParameters(AnimatorController controller)
        {
            var result = new List<AnimatorParameterInfo>(controller.parameters.Length);

            foreach (var param in controller.parameters)
            {
                var info = CreateParameterInfo(param);
                result.Add(info);
            }

            return result;
        }

        private static AnimatorParameterInfo CreateParameterInfo(AnimatorControllerParameter param)
        {
            return new AnimatorParameterInfo
            {
                Name = param.name,
                Type = param.type.ToString(),
                DefaultValue = GetDefaultValue(param)
            };
        }

        private static object GetDefaultValue(AnimatorControllerParameter param)
        {
            switch (param.type)
            {
                case AnimatorControllerParameterType.Float:
                    return param.defaultFloat;
                case AnimatorControllerParameterType.Int:
                    return param.defaultInt;
                case AnimatorControllerParameterType.Bool:
                    return param.defaultBool;
                case AnimatorControllerParameterType.Trigger:
                    return false;
                default:
                    return null;
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

    internal class GetAnimatorParametersArgs
    {
        [JsonProperty("gameObjectPath")]
        public string GameObjectPath { get; set; } = string.Empty;

        [JsonProperty("assetPath")]
        public string AssetPath { get; set; } = string.Empty;
    }

    internal class AnimatorParameterInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("defaultValue")]
        public object DefaultValue { get; set; }
    }

    internal class GetAnimatorParametersResult
    {
        [JsonProperty("controllerName")]
        public string ControllerName { get; set; }

        [JsonProperty("parameters")]
        public List<AnimatorParameterInfo> Parameters { get; set; }
    }
}
