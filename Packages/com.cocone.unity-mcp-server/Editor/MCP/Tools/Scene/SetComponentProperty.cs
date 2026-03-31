using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityMcp.Tools.Scene
{
    /// <summary>
    /// シーン上のGameObjectのコンポーネントプロパティを編集するツール
    /// </summary>
    public class SetComponentProperty : IMcpTool
    {
        public string Name => "set_component_property";

        public string Description =>
            "Set a serialized property value on a component attached to a GameObject in the current scene. " +
            "Specify the GameObject by hierarchy path, the component by type name, and the property by its propertyPath. " +
            "Use get_component_info first to find the correct propertyPath. " +
            "Supports int, float, bool, string, enum (by name), Vector2, Vector3, Color, and ObjectReference (by asset path).";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"gameObjectPath\":{\"type\":\"string\",\"description\":\"Hierarchy path from scene root (e.g. 'Canvas/Panel/Button')\"}," +
            "\"componentType\":{\"type\":\"string\",\"description\":\"Component type name (e.g. 'Transform', 'MeshRenderer')\"}," +
            "\"propertyPath\":{\"type\":\"string\",\"description\":\"SerializedProperty path (e.g. 'm_LocalPosition', 'm_Enabled'). Use get_component_info to find valid paths.\"}," +
            "\"value\":{\"description\":\"Value to set. Type depends on the property: number, string, boolean, or object (e.g. {\\\"x\\\":1,\\\"y\\\":2,\\\"z\\\":3} for Vector3).\"}" +
            "},\"required\":[\"gameObjectPath\",\"componentType\",\"propertyPath\",\"value\"]}";

        public Task<object> Execute(string args)
        {
            var json = JObject.Parse(args);
            var gameObjectPath = json.Value<string>("gameObjectPath");
            var componentType = json.Value<string>("componentType");
            var propertyPath = json.Value<string>("propertyPath");
            var valueToken = json["value"];

            ValidateArgs(gameObjectPath, componentType, propertyPath, valueToken);

            var go = FindGameObject(gameObjectPath);
            if (go == null)
            {
                throw new InvalidOperationException($"GameObject not found: '{gameObjectPath}'");
            }

            var component = FindComponent(go, componentType);
            if (component == null)
            {
                throw new InvalidOperationException(
                    $"Component '{componentType}' not found on '{gameObjectPath}'");
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
            EditorSceneManager.MarkSceneDirty(go.scene);

            var newValue = SerializedPropertyExtractor.GetPropertyValue(prop);
            var result = new SetComponentPropertyResult(gameObjectPath, componentType, propertyPath, newValue);
            return Task.FromResult<object>(result);
        }

        private static void ValidateArgs(string goPath, string compType, string propPath, JToken value)
        {
            if (string.IsNullOrEmpty(goPath))
            {
                throw new InvalidOperationException("gameObjectPath is required.");
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

    internal class SetComponentPropertyResult
    {
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

        public SetComponentPropertyResult(string goPath, string compType, string propPath, object newValue)
        {
            GameObjectPath = goPath;
            ComponentType = compType;
            PropertyPath = propPath;
            NewValue = newValue;
            Message = $"Property '{propPath}' on '{compType}' set to '{newValue}'.";
        }
    }
}
