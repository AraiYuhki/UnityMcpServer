using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityMcp.Tools.Scene
{
    /// <summary>
    /// CanvasおよびRectTransformの詳細情報を返すツール
    /// </summary>
    public class GetCanvasInfo : IMcpTool
    {
        public string Name => "get_canvas_info";

        public string Description =>
            "Get detailed Canvas and RectTransform information for UI GameObjects. " +
            "Returns Canvas settings, RectTransform layout data (anchors, pivot, size, position), " +
            "and UI component details. Use get_scene_hierarchy to find Canvas GameObjects first.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"gameObjectPath\":{\"type\":\"string\",\"description\":\"Hierarchy path from scene root (e.g. 'Canvas/Panel/Button')\"}," +
            "\"includeChildren\":{\"type\":\"boolean\",\"description\":\"Also return RectTransform info for direct children. Default: false.\"}" +
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

            var rectTransform = go.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                throw new InvalidOperationException(
                    $"GameObject '{parameters.GameObjectPath}' does not have a RectTransform. It is not a UI element.");
            }

            var result = BuildResult(go, rectTransform, parameters);
            return Task.FromResult<object>(result);
        }

        private static CanvasInfoResult BuildResult(
            GameObject go,
            RectTransform rectTransform,
            GetCanvasInfoArgs parameters)
        {
            var result = new CanvasInfoResult
            {
                GameObjectPath = parameters.GameObjectPath,
                Canvas = ExtractCanvasData(go),
                RectTransform = ExtractRectTransformData(rectTransform),
                Children = new List<ChildRectTransformData>()
            };

            if (parameters.IncludeChildren)
            {
                CollectChildrenInfo(rectTransform, parameters.GameObjectPath, result.Children);
            }

            return result;
        }

        private static CanvasData ExtractCanvasData(GameObject go)
        {
            var canvas = go.GetComponent<Canvas>();
            if (canvas == null)
            {
                return null;
            }

            return new CanvasData
            {
                RenderMode = canvas.renderMode.ToString(),
                SortingOrder = canvas.sortingOrder,
                SortingLayerName = canvas.sortingLayerName,
                PixelPerfect = canvas.pixelPerfect
            };
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

        private static void CollectChildrenInfo(
            RectTransform parent,
            string parentPath,
            List<ChildRectTransformData> children)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var childRect = child.GetComponent<RectTransform>();
                if (childRect == null)
                {
                    continue;
                }

                var childPath = parentPath + "/" + child.name;
                children.Add(new ChildRectTransformData
                {
                    Name = child.name,
                    Path = childPath,
                    RectTransform = ExtractRectTransformData(childRect)
                });
            }
        }

        private static string FormatVector2(Vector2 v)
        {
            return $"({v.x}, {v.y})";
        }

        private static string FormatVector3(Vector3 v)
        {
            return $"({v.x}, {v.y}, {v.z})";
        }

        private static GetCanvasInfoArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new GetCanvasInfoArgs();
            }

            return JsonConvert.DeserializeObject<GetCanvasInfoArgs>(args) ?? new GetCanvasInfoArgs();
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

    internal class GetCanvasInfoArgs
    {
        [JsonProperty("gameObjectPath")]
        public string GameObjectPath { get; set; } = string.Empty;

        [JsonProperty("includeChildren")]
        public bool IncludeChildren { get; set; }
    }

    internal class CanvasInfoResult
    {
        [JsonProperty("gameObjectPath")]
        public string GameObjectPath { get; set; }

        [JsonProperty("canvas")]
        public CanvasData Canvas { get; set; }

        [JsonProperty("rectTransform")]
        public RectTransformData RectTransform { get; set; }

        [JsonProperty("children")]
        public List<ChildRectTransformData> Children { get; set; }
    }

    internal class CanvasData
    {
        [JsonProperty("renderMode")]
        public string RenderMode { get; set; }

        [JsonProperty("sortingOrder")]
        public int SortingOrder { get; set; }

        [JsonProperty("sortingLayerName")]
        public string SortingLayerName { get; set; }

        [JsonProperty("pixelPerfect")]
        public bool PixelPerfect { get; set; }
    }

    internal class RectTransformData
    {
        [JsonProperty("anchoredPosition")]
        public string AnchoredPosition { get; set; }

        [JsonProperty("sizeDelta")]
        public string SizeDelta { get; set; }

        [JsonProperty("anchorMin")]
        public string AnchorMin { get; set; }

        [JsonProperty("anchorMax")]
        public string AnchorMax { get; set; }

        [JsonProperty("pivot")]
        public string Pivot { get; set; }

        [JsonProperty("localPosition")]
        public string LocalPosition { get; set; }

        [JsonProperty("localScale")]
        public string LocalScale { get; set; }

        [JsonProperty("localRotation")]
        public string LocalRotation { get; set; }
    }

    internal class ChildRectTransformData
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("rectTransform")]
        public RectTransformData RectTransform { get; set; }
    }
}
