using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityMcp.Models;

namespace UnityMcp.Tools.Scene
{
    /// <summary>
    /// SceneビューまたはGameビューのスクリーンショットをJPEGで取得するツール
    /// </summary>
    public class TakeScreenshot : IMcpTool
    {
        private const int DefaultWidth = 1920;
        private const int DefaultHeight = 1080;
        private const int DefaultQuality = 75;

        public string Name => "take_screenshot";

        public string Description =>
            "Take a screenshot of the Scene view or Game view and return it as a base64-encoded JPEG image. " +
            "Use 'scene' view to capture the editor's Scene view camera, or 'game' view to capture from the main camera. " +
            "Set includeUi to true (with view 'game') to also capture Canvas UI rendered in Screen Space - Overlay, " +
            "which the camera cannot see on its own. Works in both Edit Mode and Play Mode. " +
            "Useful for verifying visual state of the scene.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"view\":{\"type\":\"string\",\"enum\":[\"scene\",\"game\"],\"description\":\"Which view to capture: 'scene' for Scene view camera, 'game' for main camera (default: 'scene')\",\"default\":\"scene\"}," +
            "\"includeUi\":{\"type\":\"boolean\",\"description\":\"When true and view is 'game', temporarily render Screen Space - Overlay UI Canvas elements through the camera so they appear in the capture. (default: false)\",\"default\":false}," +
            "\"width\":{\"type\":\"integer\",\"description\":\"Image width in pixels (default: 1920)\",\"default\":1920}," +
            "\"height\":{\"type\":\"integer\",\"description\":\"Image height in pixels (default: 1080)\",\"default\":1080}," +
            "\"quality\":{\"type\":\"integer\",\"description\":\"JPEG quality 1-100 (default: 75)\",\"default\":75}" +
            "},\"required\":[]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);
            var width = parameters.Width > 0 ? parameters.Width : DefaultWidth;
            var height = parameters.Height > 0 ? parameters.Height : DefaultHeight;
            var quality = Mathf.Clamp(parameters.Quality > 0 ? parameters.Quality : DefaultQuality, 1, 100);

            var camera = ResolveCamera(parameters.View);

            string base64;
            if (parameters.IncludeUi)
            {
                if (!string.Equals(parameters.View, "game", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("includeUi is only supported when view is 'game'.");
                }

                base64 = CaptureFromCameraWithOverlayUi(camera, width, height, quality);
            }
            else
            {
                base64 = CaptureFromCamera(camera, width, height, quality);
            }

            var result = CallToolResult.SuccessImage(base64, "image/jpeg");
            return Task.FromResult<object>(result);
        }

        private static Camera ResolveCamera(string view)
        {
            if (string.Equals(view, "game", StringComparison.OrdinalIgnoreCase))
            {
                var mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    throw new InvalidOperationException(
                        "No main camera found in the scene. Ensure a Camera with tag 'MainCamera' exists.");
                }

                return mainCamera;
            }

            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                throw new InvalidOperationException("No active Scene View found.");
            }

            return sceneView.camera;
        }

        private static string CaptureFromCamera(Camera camera, int width, int height, int quality)
        {
            var rt = RenderTexture.GetTemporary(width, height, 24);
            var prevTarget = camera.targetTexture;
            var prevActive = RenderTexture.active;

            try
            {
                camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;

                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                var jpegData = tex.EncodeToJPG(quality);
                UnityEngine.Object.DestroyImmediate(tex);

                return Convert.ToBase64String(jpegData);
            }
            finally
            {
                camera.targetTexture = prevTarget;
                RenderTexture.active = prevActive;
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        private static string CaptureFromCameraWithOverlayUi(Camera camera, int width, int height, int quality)
        {
            var overlayCanvases = new List<Canvas>();
            var originalCameras = new List<Camera>();
            var originalPlaneDistances = new List<float>();

            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    continue;
                }

                overlayCanvases.Add(canvas);
                originalCameras.Add(canvas.worldCamera);
                originalPlaneDistances.Add(canvas.planeDistance);

                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = Mathf.Clamp(100f, camera.nearClipPlane + 0.01f, camera.farClipPlane - 0.01f);
            }

            try
            {
                return CaptureFromCamera(camera, width, height, quality);
            }
            finally
            {
                for (var i = 0; i < overlayCanvases.Count; i++)
                {
                    overlayCanvases[i].renderMode = RenderMode.ScreenSpaceOverlay;
                    overlayCanvases[i].worldCamera = originalCameras[i];
                    overlayCanvases[i].planeDistance = originalPlaneDistances[i];
                }
            }
        }

        private static TakeScreenshotArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new TakeScreenshotArgs();
            }

            return JsonConvert.DeserializeObject<TakeScreenshotArgs>(args) ?? new TakeScreenshotArgs();
        }
    }

    internal class TakeScreenshotArgs
    {
        [JsonProperty("view")]
        public string View { get; set; } = "scene";

        [JsonProperty("includeUi")]
        public bool IncludeUi { get; set; } = false;

        [JsonProperty("width")]
        public int Width { get; set; } = 1920;

        [JsonProperty("height")]
        public int Height { get; set; } = 1080;

        [JsonProperty("quality")]
        public int Quality { get; set; } = 75;
    }
}
