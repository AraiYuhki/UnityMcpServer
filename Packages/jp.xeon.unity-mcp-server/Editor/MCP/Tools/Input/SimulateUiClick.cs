#if MCP_UGUI
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityMcp.Tools.Editor;

namespace UnityMcp.Tools.InputSimulation
{
    /// <summary>
    /// uGUIの要素に対してポインタイベントを直接発火させるツール。
    /// PlayMode中のみ動作し、入力バックエンドに依存せずButton等のクリックを再現する。
    /// holdMs を指定すると押しっぱなしを再現でき、press / release に分けた操作も行える。
    /// </summary>
    public class SimulateUiClick : IMcpTool
    {
        private const int MaxHoldMs = 30000;

        public string Name => "simulate_ui_click";

        public string Description =>
            "Click, press, release or press-and-hold a uGUI element by firing pointer events directly via " +
            "ExecuteEvents (PlayMode only). Invokes pointer enter/down/up/click handlers (e.g. Button.onClick) " +
            "on the target GameObject, independent of the active input backend. " +
            "Use action 'click' for a normal click, 'hold' to keep the button pressed for holdMs while the " +
            "player loop keeps running (e.g. a 'move forward' button), or 'press'/'release' to control the " +
            "press state across several calls. " +
            "Specify the GameObject by its hierarchy path from a scene root (e.g. 'Canvas/Panel/Button').";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"gameObjectPath\":{\"type\":\"string\",\"description\":\"Hierarchy path from scene root (e.g. 'Canvas/Panel/Button')\"}," +
            "\"action\":{\"type\":\"string\",\"enum\":[\"click\",\"hold\",\"press\",\"release\"],\"description\":\"Pointer action to perform. Default: click.\"}," +
            "\"holdMs\":{\"type\":\"integer\",\"description\":\"How long to keep the pointer pressed for action 'hold' (default: 500, max: 30000)\",\"default\":500}," +
            "\"button\":{\"type\":\"string\",\"enum\":[\"left\",\"right\",\"middle\"],\"description\":\"Pointer button to report. Default: left.\"}" +
            "},\"required\":[\"gameObjectPath\"]}";

        public async Task<object> Execute(string args)
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("simulate_ui_click requires Play Mode. Enter Play Mode first.");
            }

            var parameters = ParseArgs(args);
            var target = UiSimulationUtility.RequireGameObject(parameters.GameObjectPath);
            var eventData = BuildPointerData(target, parameters.Button);

            return await Dispatch(parameters, target, eventData);
        }

        private static Task<SimulateUiClickResult> Dispatch(
            SimulateUiClickArgs parameters,
            GameObject target,
            PointerEventData eventData)
        {
            switch (parameters.Action)
            {
                case "click":
                    return Task.FromResult(Click(parameters, target, eventData));
                case "hold":
                    return Hold(parameters, target, eventData);
                case "press":
                    return Task.FromResult(Press(parameters, target, eventData));
                case "release":
                    return Task.FromResult(Release(parameters, target, eventData));
                default:
                    throw new InvalidOperationException(
                        $"Unknown action: '{parameters.Action}'. Use 'click', 'hold', 'press', or 'release'.");
            }
        }

        private static SimulateUiClickResult Click(
            SimulateUiClickArgs parameters,
            GameObject target,
            PointerEventData eventData)
        {
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerExitHandler);

            return BuildResult(parameters, "click", $"Fired pointer click on '{parameters.GameObjectPath}'.");
        }

        /// <summary>
        /// 指定時間ポインタを押し続けてから離す。押しっぱなしを要求するUIの検証に使う。
        /// </summary>
        private static async Task<SimulateUiClickResult> Hold(
            SimulateUiClickArgs parameters,
            GameObject target,
            PointerEventData eventData)
        {
            var holdMs = ResolveHoldMs(parameters.HoldMs);

            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerDownHandler);
            await PlayerLoopDriver.HoldAsync(holdMs);
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerExitHandler);

            return BuildResult(parameters, "hold",
                $"Held pointer on '{parameters.GameObjectPath}' for {holdMs}ms.");
        }

        private static SimulateUiClickResult Press(
            SimulateUiClickArgs parameters,
            GameObject target,
            PointerEventData eventData)
        {
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerDownHandler);

            return BuildResult(parameters, "press",
                $"Pressed pointer on '{parameters.GameObjectPath}'. Call action 'release' to let go.");
        }

        private static SimulateUiClickResult Release(
            SimulateUiClickArgs parameters,
            GameObject target,
            PointerEventData eventData)
        {
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerExitHandler);

            return BuildResult(parameters, "release", $"Released pointer on '{parameters.GameObjectPath}'.");
        }

        private static PointerEventData BuildPointerData(GameObject target, string button)
        {
            var position = UiSimulationUtility.ResolveScreenPosition(target);
            return UiSimulationUtility.BuildPointerData(target, button, position);
        }

        private static int ResolveHoldMs(int requested)
        {
            if (requested <= 0)
            {
                return 0;
            }

            return requested > MaxHoldMs ? MaxHoldMs : requested;
        }

        private static SimulateUiClickResult BuildResult(
            SimulateUiClickArgs parameters,
            string action,
            string message)
        {
            return new SimulateUiClickResult
            {
                Ok = true,
                Action = action,
                GameObjectPath = parameters.GameObjectPath,
                Button = parameters.Button,
                Message = message
            };
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

        [JsonProperty("action")]
        public string Action { get; set; } = "click";

        [JsonProperty("holdMs")]
        public int HoldMs { get; set; } = 500;

        [JsonProperty("button")]
        public string Button { get; set; } = "left";
    }

    internal class SimulateUiClickResult
    {
        [JsonProperty("ok")]
        public bool Ok { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("gameObjectPath")]
        public string GameObjectPath { get; set; }

        [JsonProperty("button")]
        public string Button { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
#endif
