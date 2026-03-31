using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Tools.Scene
{
    /// <summary>
    /// SerializedObjectからプロパティ情報を抽出する共通ユーティリティ
    /// </summary>
    public static class SerializedPropertyExtractor
    {
        private const int PropertyMaxDepth = 3;

        /// <summary>
        /// GameObjectにアタッチされた全コンポーネントの詳細を取得する
        /// </summary>
        /// <param name="go">対象GameObject</param>
        /// <param name="componentTypeFilter">フィルタリングするコンポーネント型名（空なら全件）</param>
        /// <returns>コンポーネント詳細リスト</returns>
        public static List<ComponentDetail> CollectComponentDetails(GameObject go, string componentTypeFilter)
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

        /// <summary>
        /// コンポーネントのシリアライズフィールドをすべて抽出する
        /// </summary>
        /// <param name="component">対象コンポーネント</param>
        /// <returns>プロパティパスをキー、値をバリューとした辞書</returns>
        public static Dictionary<string, object> ExtractFields(Component component)
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

        /// <summary>
        /// SerializedPropertyの値を読みやすい形式で取得する
        /// </summary>
        public static object GetPropertyValue(SerializedProperty prop)
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
}
