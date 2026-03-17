using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityMcp.Tools.Scene
{
    /// <summary>
    /// UI要素のRectTransformプロパティを設定するツール
    /// </summary>
    public class SetRectTransform : IMcpTool
    {
        public string Name => "set_rect_transform";

        public string Description =>
            "Set RectTransform properties on a UI GameObject. " +
            "Use get_canvas_info to see current values first. " +
            "All properties are optional - only specified properties will be changed.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"gameObjectPath\":{\"type\":\"string\",\"description\":\"Hierarchy path from scene root (e.g. 'Canvas/Panel/Button')\"}," +
            "\"anchoredPosition\":{\"type\":\"object\",\"properties\":{\"x\":{\"type\":\"number\"},\"y\":{\"type\":\"number\"}},\"description\":\"Anchored position (x, y)\"}," +
            "\"sizeDelta\":{\"type\":\"object\",\"properties\":{\"x\":{\"type\":\"number\"},\"y\":{\"type\":\"number\"}},\"description\":\"Size delta (width, height)\"}," +
            "\"anchorMin\":{\"type\":\"object\",\"properties\":{\"x\":{\"type\":\"number\"},\"y\":{\"type\":\"number\"}},\"description\":\"Anchor min (x, y)\"}," +
            "\"anchorMax\":{\"type\":\"object\",\"properties\":{\"x\":{\"type\":\"number\"},\"y\":{\"type\":\"number\"}},\"description\":\"Anchor max (x, y)\"}," +
            "\"pivot\":{\"type\":\"object\",\"properties\":{\"x\":{\"type\":\"number\"},\"y\":{\"type\":\"number\"}},\"description\":\"Pivot (x, y)\"}," +
            "\"localScale\":{\"type\":\"object\",\"properties\":{\"x\":{\"type\":\"number\"},\"y\":{\"type\":\"number\"},\"z\":{\"type\":\"number\"}},\"description\":\"Local scale (x, y, z)\"}," +
            "\"localRotation\":{\"type\":\"object\",\"properties\":{\"x\":{\"type\":\"number\"},\"y\":{\"type\":\"number\"},\"z\":{\"type\":\"number\"}},\"description\":\"Local rotation in euler angles (x, y, z)\"}" +
            "},\"required\":[\"gameObjectPath\"]}";

        public Task<object> Execute(string args)
        {
            var json = JObject.Parse(args);
            var gameObjectPath = json.Value<string>("gameObjectPath");

            if (string.IsNullOrEmpty(gameObjectPath))
            {
                throw new InvalidOperationException("gameObjectPath is required.");
            }

            var go = FindGameObject(gameObjectPath);
            if (go == null)
            {
                throw new InvalidOperationException($"GameObject not found: '{gameObjectPath}'");
            }

            var rectTransform = go.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                throw new InvalidOperationException(
                    $"GameObject '{gameObjectPath}' does not have a RectTransform. It is not a UI element.");
            }

            Undo.RecordObject(rectTransform, "Set RectTransform");
            var changed = ApplyProperties(rectTransform, json);
            EditorSceneManager.MarkSceneDirty(go.scene);

            var result = new SetRectTransformResult
            {
                GameObjectPath = gameObjectPath,
                Message = $"Updated {changed.Count} properties on '{gameObjectPath}': {string.Join(", ", changed)}.",
                ChangedProperties = changed,
                RectTransform = ExtractRectTransformData(rectTransform)
            };

            return Task.FromResult<object>(result);
        }

        private static List<string> ApplyProperties(RectTransform rt, JObject json)
        {
            var changed = new List<string>();

            if (json["anchoredPosition"] != null)
            {
                rt.anchoredPosition = ParseVector2(json["anchoredPosition"]);
                changed.Add("anchoredPosition");
            }

            if (json["sizeDelta"] != null)
            {
                rt.sizeDelta = ParseVector2(json["sizeDelta"]);
                changed.Add("sizeDelta");
            }

            if (json["anchorMin"] != null)
            {
                rt.anchorMin = ParseVector2(json["anchorMin"]);
                changed.Add("anchorMin");
            }

            if (json["anchorMax"] != null)
            {
                rt.anchorMax = ParseVector2(json["anchorMax"]);
                changed.Add("anchorMax");
            }

            if (json["pivot"] != null)
            {
                rt.pivot = ParseVector2(json["pivot"]);
                changed.Add("pivot");
            }

            if (json["localScale"] != null)
            {
                rt.localScale = ParseVector3(json["localScale"]);
                changed.Add("localScale");
            }

            if (json["localRotation"] != null)
            {
                rt.localRotation = Quaternion.Euler(ParseVector3(json["localRotation"]));
                changed.Add("localRotation");
            }

            if (changed.Count == 0)
            {
                throw new InvalidOperationException(
                    "No properties specified. Provide at least one of: anchoredPosition, sizeDelta, " +
                    "anchorMin, anchorMax, pivot, localScale, localRotation.");
            }

            return changed;
        }

        private static RectTransformData ExtractRectTransformData(RectTransform rt)
        {
            return new RectTransformData
            {
                AnchoredPosition = FormatVector2(rt.anchoredPosition),
                SizeDelta = FormatVector2(rt.sizeDelta),
                AnchorMin = FormatVector2(rt.anchorMin),
                AnchorMax = FormatVector2(rt.anchorMax),
                Pivot = FormatVector2(rt.pivot),
                LocalPosition = FormatVector3(rt.localPosition),
                LocalScale = FormatVector3(rt.localScale),
                LocalRotation = FormatVector3(rt.localRotation.eulerAngles)
            };
        }

        private static string FormatVector2(Vector2 v)
        {
            return $"({v.x}, {v.y})";
        }

        private static string FormatVector3(Vector3 v)
        {
            return $"({v.x}, {v.y}, {v.z})";
        }

        private static Vector2 ParseVector2(JToken token)
        {
            return new Vector2(token.Value<float>("x"), token.Value<float>("y"));
        }

        private static Vector3 ParseVector3(JToken token)
        {
            return new Vector3(token.Value<float>("x"), token.Value<float>("y"), token.Value<float>("z"));
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

    internal class SetRectTransformResult
    {
        [JsonProperty("gameObjectPath")]
        public string GameObjectPath { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("changedProperties")]
        public List<string> ChangedProperties { get; set; }

        [JsonProperty("rectTransform")]
        public RectTransformData RectTransform { get; set; }
    }
}
