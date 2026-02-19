using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityMcp.Tools.Scene
{
    /// <summary>
    /// 指定GameObjectにアタッチされているコンポーネントの詳細情報を返すツール
    /// </summary>
    public class GetComponentInfo : IMcpTool
    {
        private const int PropertyMaxDepth = 3;

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

            var components = CollectComponentDetails(go, parameters.ComponentType);
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

        private static List<ComponentDetail> CollectComponentDetails(GameObject go, string componentTypeFilter)
        {
            var result = new List<ComponentDetail>();

            foreach (var component in go.GetComponents<Component>())
            {
                if (component == null)
                {
                    continue;
                }

                var typeName = component.GetType().Name;
                var fullTypeName = component.GetType().FullName;

                if (!string.IsNullOrEmpty(componentTypeFilter)
                    && !typeName.Contains(componentTypeFilter)
                    && !fullTypeName.Contains(componentTypeFilter))
                {
                    continue;
                }

                var fields = ExtractFields(component);
                result.Add(ComponentDetail.Create(component, fields));
            }

            return result;
        }

        private static Dictionary<string, object> ExtractFields(Component component)
        {
            var fields = new Dictionary<string, object>();
            var so = new SerializedObject(component);
            var prop = so.GetIterator();
            var enterChildren = true;

            while (prop.NextVisible(enterChildren))
            {
                if (prop.name == "m_Script")
                {
                    enterChildren = true;
                    continue;
                }

                enterChildren = prop.depth < PropertyMaxDepth && !prop.isArray;

                if (prop.isArray)
                {
                    fields[prop.propertyPath] = $"Array[{prop.arraySize}]";
                }
                else if (!prop.hasVisibleChildren)
                {
                    fields[prop.propertyPath] = GetPropertyValue(prop);
                }
            }

            return fields;
        }

        private static object GetPropertyValue(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer: return prop.intValue;
                case SerializedPropertyType.Boolean: return prop.boolValue;
                case SerializedPropertyType.Float: return prop.floatValue;
                case SerializedPropertyType.String: return prop.stringValue;
                case SerializedPropertyType.Enum:
                    var idx = prop.enumValueIndex;
                    return idx >= 0 && idx < prop.enumNames.Length ? prop.enumNames[idx] : (object)prop.intValue;
                case SerializedPropertyType.ObjectReference:
                    return GetObjectReferenceValue(prop);
                case SerializedPropertyType.LayerMask:
                    return LayerMask.LayerToName(prop.intValue);
                case SerializedPropertyType.Vector2: return prop.vector2Value.ToString();
                case SerializedPropertyType.Vector3: return prop.vector3Value.ToString();
                case SerializedPropertyType.Vector4: return prop.vector4Value.ToString();
                case SerializedPropertyType.Quaternion: return prop.quaternionValue.eulerAngles.ToString();
                case SerializedPropertyType.Color: return prop.colorValue.ToString();
                case SerializedPropertyType.Rect: return prop.rectValue.ToString();
                case SerializedPropertyType.Bounds: return prop.boundsValue.ToString();
                default: return prop.type;
            }
        }

        private static object GetObjectReferenceValue(SerializedProperty prop)
        {
            if (prop.objectReferenceValue != null)
            {
                return $"{prop.objectReferenceValue.name} ({prop.objectReferenceValue.GetType().Name})";
            }

            return prop.objectReferenceInstanceIDValue != 0 ? "Missing" : "null";
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
