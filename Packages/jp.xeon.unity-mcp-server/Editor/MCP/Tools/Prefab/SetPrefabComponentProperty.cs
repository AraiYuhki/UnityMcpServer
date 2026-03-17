using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityMcp.Tools.Scene;

namespace UnityMcp.Tools.Prefab
{
    /// <summary>
    /// プレハブアセット内のコンポーネントのシリアライズプロパティを編集するツール
    /// </summary>
    public class SetPrefabComponentProperty : IMcpTool
    {
        public string Name => "set_prefab_component_property";

        public string Description =>
            "Set a serialized property value on a component within a Prefab asset. " +
            "Uses PrefabUtility to load, edit, and save the Prefab. " +
            "Use get_prefab_component_info to find valid property paths. " +
            "Supports int, float, bool, string, enum (by name), Vector2, Vector3, Vector4, Color, Quaternion, and ObjectReference (by asset path).";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"assetPath\":{\"type\":\"string\",\"description\":\"Asset path to the Prefab (e.g. 'Assets/Prefabs/Player.prefab')\"}," +
            "\"gameObjectPath\":{\"type\":\"string\",\"description\":\"Child path within the Prefab (e.g. 'Body/Head'). If omitted, targets the root GameObject.\"}," +
            "\"componentType\":{\"type\":\"string\",\"description\":\"Component type name (e.g. 'Transform', 'MeshRenderer')\"}," +
            "\"propertyPath\":{\"type\":\"string\",\"description\":\"SerializedProperty path (e.g. 'm_LocalPosition', 'm_Enabled'). Use get_prefab_component_info to find valid paths.\"}," +
            "\"value\":{\"description\":\"Value to set. Type depends on the property: number, string, boolean, or object (e.g. {\\\"x\\\":1,\\\"y\\\":2,\\\"z\\\":3} for Vector3).\"}" +
            "},\"required\":[\"assetPath\",\"componentType\",\"propertyPath\",\"value\"]}";

        public Task<object> Execute(string args)
        {
            var json = JObject.Parse(args);
            var assetPath = json.Value<string>("assetPath");
            var gameObjectPath = json.Value<string>("gameObjectPath") ?? string.Empty;
            var componentType = json.Value<string>("componentType");
            var propertyPath = json.Value<string>("propertyPath");
            var valueToken = json["value"];

            ValidateArgs(assetPath, componentType, propertyPath, valueToken);

            var root = PrefabUtility.LoadPrefabContents(assetPath);
            if (root == null)
            {
                throw new InvalidOperationException($"Failed to load Prefab: '{assetPath}'");
            }

            try
            {
                var target = FindTarget(root, gameObjectPath);
                if (target == null)
                {
                    throw new InvalidOperationException(
                        $"GameObject not found in Prefab: '{gameObjectPath}'");
                }

                var component = FindComponent(target, componentType);
                if (component == null)
                {
                    throw new InvalidOperationException(
                        $"Component '{componentType}' not found on '{target.name}'");
                }

                var so = new SerializedObject(component);
                var prop = so.FindProperty(propertyPath);
                if (prop == null)
                {
                    throw new InvalidOperationException(
                        $"Property '{propertyPath}' not found on component '{componentType}'");
                }

                ApplyValue(prop, valueToken);
                so.ApplyModifiedProperties();

                PrefabUtility.SaveAsPrefabAsset(root, assetPath);

                var newValue = SerializedPropertyExtractor.GetPropertyValue(prop);
                var displayPath = string.IsNullOrEmpty(gameObjectPath) ? root.name : $"{root.name}/{gameObjectPath}";
                var result = new SetPrefabComponentPropertyResult(assetPath, displayPath, componentType, propertyPath, newValue);
                return Task.FromResult<object>(result);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ValidateArgs(string assetPath, string compType, string propPath, JToken value)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new InvalidOperationException("assetPath is required.");
            }

            if (!assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Asset is not a Prefab: '{assetPath}'");
            }

            if (string.IsNullOrEmpty(compType))
            {
                throw new InvalidOperationException("componentType is required.");
            }

            if (string.IsNullOrEmpty(propPath))
            {
                throw new InvalidOperationException("propertyPath is required.");
            }

            if (value == null)
            {
                throw new InvalidOperationException("value is required.");
            }
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

        private static void ApplyValue(SerializedProperty prop, JToken valueToken)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    prop.intValue = valueToken.Value<int>();
                    break;
                case SerializedPropertyType.Boolean:
                    prop.boolValue = valueToken.Value<bool>();
                    break;
                case SerializedPropertyType.Float:
                    prop.floatValue = valueToken.Value<float>();
                    break;
                case SerializedPropertyType.String:
                    prop.stringValue = valueToken.Value<string>();
                    break;
                case SerializedPropertyType.Enum:
                    ApplyEnumValue(prop, valueToken);
                    break;
                case SerializedPropertyType.Vector2:
                    prop.vector2Value = ParseVector2(valueToken);
                    break;
                case SerializedPropertyType.Vector3:
                    prop.vector3Value = ParseVector3(valueToken);
                    break;
                case SerializedPropertyType.Vector4:
                    prop.vector4Value = ParseVector4(valueToken);
                    break;
                case SerializedPropertyType.Color:
                    prop.colorValue = ParseColor(valueToken);
                    break;
                case SerializedPropertyType.Quaternion:
                    prop.quaternionValue = Quaternion.Euler(ParseVector3(valueToken));
                    break;
                case SerializedPropertyType.ObjectReference:
                    ApplyObjectReference(prop, valueToken);
                    break;
                case SerializedPropertyType.LayerMask:
                    prop.intValue = LayerMask.NameToLayer(valueToken.Value<string>());
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unsupported property type: {prop.propertyType}");
            }
        }

        private static void ApplyEnumValue(SerializedProperty prop, JToken valueToken)
        {
            var valueName = valueToken.Value<string>();
            for (var i = 0; i < prop.enumNames.Length; i++)
            {
                if (string.Equals(prop.enumNames[i], valueName, StringComparison.OrdinalIgnoreCase))
                {
                    prop.enumValueIndex = i;
                    return;
                }
            }

            if (int.TryParse(valueName, out var index))
            {
                prop.enumValueIndex = index;
                return;
            }

            throw new InvalidOperationException(
                $"Invalid enum value: '{valueName}'. Valid values: {string.Join(", ", prop.enumNames)}");
        }

        private static Vector2 ParseVector2(JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                return new Vector2(
                    token.Value<float>("x"),
                    token.Value<float>("y"));
            }

            throw new InvalidOperationException("Vector2 value must be an object with 'x' and 'y' fields.");
        }

        private static Vector3 ParseVector3(JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                return new Vector3(
                    token.Value<float>("x"),
                    token.Value<float>("y"),
                    token.Value<float>("z"));
            }

            throw new InvalidOperationException("Vector3 value must be an object with 'x', 'y', and 'z' fields.");
        }

        private static Vector4 ParseVector4(JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                return new Vector4(
                    token.Value<float>("x"),
                    token.Value<float>("y"),
                    token.Value<float>("z"),
                    token.Value<float>("w"));
            }

            throw new InvalidOperationException("Vector4 value must be an object with 'x', 'y', 'z', and 'w' fields.");
        }

        private static Color ParseColor(JToken token)
        {
            if (token.Type == JTokenType.String)
            {
                if (ColorUtility.TryParseHtmlString(token.Value<string>(), out var color))
                {
                    return color;
                }

                throw new InvalidOperationException(
                    "Color string must be a valid HTML color (e.g. '#FF0000', 'red').");
            }

            if (token.Type == JTokenType.Object)
            {
                return new Color(
                    token.Value<float>("r"),
                    token.Value<float>("g"),
                    token.Value<float>("b"),
                    token["a"] != null ? token.Value<float>("a") : 1f);
            }

            throw new InvalidOperationException(
                "Color value must be an HTML string (e.g. '#FF0000') or object with 'r','g','b','a' fields.");
        }

        private static void ApplyObjectReference(SerializedProperty prop, JToken valueToken)
        {
            var assetPath = valueToken.Value<string>();
            if (string.IsNullOrEmpty(assetPath) || assetPath == "null")
            {
                prop.objectReferenceValue = null;
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException($"Asset not found at path: '{assetPath}'");
            }

            prop.objectReferenceValue = asset;
        }
    }

    internal class SetPrefabComponentPropertyResult
    {
        [JsonProperty("assetPath")]
        public string AssetPath { get; private set; }

        [JsonProperty("gameObjectPath")]
        public string GameObjectPath { get; private set; }

        [JsonProperty("componentType")]
        public string ComponentType { get; private set; }

        [JsonProperty("propertyPath")]
        public string PropertyPath { get; private set; }

        [JsonProperty("newValue")]
        public object NewValue { get; private set; }

        [JsonProperty("message")]
        public string Message { get; private set; }

        public SetPrefabComponentPropertyResult(
            string assetPath,
            string goPath,
            string compType,
            string propPath,
            object newValue)
        {
            AssetPath = assetPath;
            GameObjectPath = goPath;
            ComponentType = compType;
            PropertyPath = propPath;
            NewValue = newValue;
            Message = $"Property '{propPath}' on '{compType}' in Prefab '{assetPath}' set to '{newValue}'.";
        }
    }
}
