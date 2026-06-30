#if MCP_UGUI
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace UnityMcp.Tools.InputSimulation
{
    /// <summary>
    /// uGUIの要素に対してポインタイベントを直接発火させるツール。
    /// PlayMode中のみ動作し、入力バックエンドに依存せずButton等のクリックを再現する。
    /// </summary>
    public class SimulateUiClick : IMcpTool
    {
        public string Name => "simulate_ui_click";

        public string Description =>
            "Click a uGUI element by firing pointer events directly via ExecuteEvents (PlayMode only). " +
            "Invokes pointer enter/down/up/click handlers (e.g. Button.onClick) on the target GameObject, " +
            "independent of the active input backend. " +
            "Specify the GameObject by its hierarchy path from a scene root (e.g. 'Canvas/Panel/Button').";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"gameObjectPath\":{\"type\":\"string\",\"description\":\"Hierarchy path from scene root (e.g. 'Canvas/Panel/Button')\"}," +
            "\"button\":{\"type\":\"string\",\"enum\":[\"left\",\"right\",\"middle\"],\"description\":\"Pointer button to report. Default: left.\"}" +
            "},\"required\":[\"gameObjectPath\"]}";

        public Task<object> Execute(string args)
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("simulate_ui_click requires Play Mode. Enter Play Mode first.");
            }

            var parameters = ParseArgs(args);
            if (string.IsNullOrEmpty(parameters.GameObjectPath))
            {
                throw new InvalidOperationException("gameObjectPath is required.");
            }

            var target = FindGameObject(parameters.GameObjectPath);
            if (target == null)
            {
                throw new InvalidOperationException($"GameObject not found: '{parameters.GameObjectPath}'");
            }

            var result = Click(target, parameters);
            return Task.FromResult<object>(result);
        }

        private static SimulateUiClickResult Click(GameObject target, SimulateUiClickArgs parameters)
        {
            var eventData = BuildPointerData(target, parameters.Button);

            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerExitHandler);

            return new SimulateUiClickResult
            {
                Ok = true,
                GameObjectPath = parameters.GameObjectPath,
                Button = parameters.Button,
                Message = $"Fired pointer click on '{parameters.GameObjectPath}'."
            };
        }

        private static PointerEventData BuildPointerData(GameObject target, string button)
        {
            var rectTransform = target.transform as RectTransform;
            var position = rectTransform != null
                ? (Vector2)rectTransform.position
                : Vector2.zero;

            return new PointerEventData(EventSystem.current)
            {
                button = ParseButton(button),
                position = position,
                pointerPress = target,
                pointerCurrentRaycast = new RaycastResult { gameObject = target }
            };
        }

        private static PointerEventData.InputButton ParseButton(string button)
        {
            switch (button)
            {
                case "left":
                    return PointerEventData.InputButton.Left;
                case "right":
                    return PointerEventData.InputButton.Right;
                case "middle":
                    return PointerEventData.InputButton.Middle;
                default:
                    throw new InvalidOperationException($"Invalid button: '{button}'. Use 'left', 'right', or 'middle'.");
            }
        }

        private static GameObject FindGameObject(string path)
        {
            var parts = path.Split('/');
            var scene = SceneManager.GetActiveScene();

            var root = FindRoot(scene, parts[0]);
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

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.name == name)
                {
                    return go;
                }
            }

            return null;
        }

        private static SimulateUiClickArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new SimulateUiClickArgs();
            }

            return JsonConvert.DeserializeObject<SimulateUiClickArgs>(args) ?? new SimulateUiClickArgs();
        }
    }

    internal class SimulateUiClickArgs
    {
        [JsonProperty("gameObjectPath")]
        public string GameObjectPath { get; set; } = string.Empty;

        [JsonProperty("button")]
        public string Button { get; set; } = "left";
    }

    internal class SimulateUiClickResult
    {
        [JsonProperty("ok")]
        public bool Ok { get; set; }

        [JsonProperty("gameObjectPath")]
        public string GameObjectPath { get; set; }

        [JsonProperty("button")]
        public string Button { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
#endif
