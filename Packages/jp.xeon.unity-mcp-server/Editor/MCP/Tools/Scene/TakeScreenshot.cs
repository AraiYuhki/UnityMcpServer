using System;
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
            "Useful for verifying visual state of the scene.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"view\":{\"type\":\"string\",\"enum\":[\"scene\",\"game\"],\"description\":\"Which view to capture: 'scene' for Scene view camera, 'game' for main camera (default: 'scene')\",\"default\":\"scene\"}," +
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
            var base64 = CaptureFromCamera(camera, width, height, quality);
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

        [JsonProperty("width")]
        public int Width { get; set; } = 1920;

        [JsonProperty("height")]
        public int Height { get; set; } = 1080;

        [JsonProperty("quality")]
        public int Quality { get; set; } = 75;
    }
}
