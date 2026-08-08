#if MCP_UGUI
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityMcp.Tools.Editor;

namespace UnityMcp.Tools.InputSimulation
{
    /// <summary>
    /// uGUI要素のドラッグ（press → move → release）を1回の呼び出しで再現するツール。
    /// スライダー・スクロールビュー・ドラッグ&ドロップの自動検証に使う。
    /// </summary>
    public class SimulateUiDrag : IMcpTool
    {
        private const int DefaultSteps = 10;
        private const int MaxSteps = 120;

        public string Name => "simulate_ui_drag";

        public string Description =>
            "Drag a uGUI element by firing pointer down / begin-drag / drag / end-drag / drop / up events " +
            "via ExecuteEvents (PlayMode only). Use this for sliders, scroll views and drag-and-drop, which " +
            "a single click cannot exercise. " +
            "The drag starts at the element's screen position (or 'from' if given) and moves to 'to' in " +
            "'steps' increments, advancing the player loop between steps so the UI can react. " +
            "Screen positions are in pixels with the bottom-left as origin.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"gameObjectPath\":{\"type\":\"string\",\"description\":\"Hierarchy path from scene root (e.g. 'Canvas/Panel/Slider/Handle')\"}," +
            "\"to\":{\"type\":\"object\",\"properties\":{\"x\":{\"type\":\"number\"},\"y\":{\"type\":\"number\"}},\"description\":\"Destination screen position in pixels.\"}," +
            "\"from\":{\"type\":\"object\",\"properties\":{\"x\":{\"type\":\"number\"},\"y\":{\"type\":\"number\"}},\"description\":\"Start screen position in pixels. Defaults to the element's own position.\"}," +
            "\"steps\":{\"type\":\"integer\",\"description\":\"Number of intermediate drag events (default: 10, max: 120)\",\"default\":10}," +
            "\"button\":{\"type\":\"string\",\"enum\":[\"left\",\"right\",\"middle\"],\"description\":\"Pointer button to report. Default: left.\"}" +
            "},\"required\":[\"gameObjectPath\",\"to\"]}";

        public async Task<object> Execute(string args)
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("simulate_ui_drag requires Play Mode. Enter Play Mode first.");
            }

            var parameters = ParseArgs(args);
            RequireDestination(parameters);

            var target = UiSimulationUtility.RequireGameObject(parameters.GameObjectPath);
            var from = ResolveStart(parameters, target);
            var to = ParseVector2(parameters.To);
            var steps = ResolveSteps(parameters.Steps);

            await Drag(target, parameters, from, to, steps);

            return BuildResult(parameters, from, to, steps);
        }

        private static async Task Drag(
            GameObject target,
            SimulateUiDragArgs parameters,
            Vector2 from,
            Vector2 to,
            int steps)
        {
            var eventData = UiSimulationUtility.BuildPointerData(target, parameters.Button, from);
            eventData.dragging = true;

            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(target, eventData, ExecuteEvents.initializePotentialDrag);
            ExecuteEvents.ExecuteHierarchy(target, eventData, ExecuteEvents.beginDragHandler);

            await MoveThroughSteps(target, eventData, from, to, steps);

            eventData.dragging = false;
            ExecuteEvents.ExecuteHierarchy(target, eventData, ExecuteEvents.endDragHandler);
            ExecuteEvents.ExecuteHierarchy(target, eventData, ExecuteEvents.dropHandler);
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerExitHandler);
        }

        private static async Task MoveThroughSteps(
            GameObject target,
            PointerEventData eventData,
            Vector2 from,
            Vector2 to,
            int steps)
        {
            var previous = from;
            for (var i = 1; i <= steps; i++)
            {
                var current = Vector2.Lerp(from, to, (float)i / steps);
                eventData.delta = current - previous;
                eventData.position = current;
                ExecuteEvents.ExecuteHierarchy(target, eventData, ExecuteEvents.dragHandler);
                previous = current;
                await PlayerLoopDriver.WaitForNextFrameAsync();
            }
        }

        private static Vector2 ResolveStart(SimulateUiDragArgs parameters, GameObject target)
        {
            if (parameters.From == null)
            {
                return UiSimulationUtility.ResolveScreenPosition(target);
            }

            return ParseVector2(parameters.From);
        }

        private static void RequireDestination(SimulateUiDragArgs parameters)
        {
            if (parameters.To == null)
            {
                throw new InvalidOperationException("'to' is required and must be an object with x and y.");
            }
        }

        private static int ResolveSteps(int requested)
        {
            if (requested <= 0)
            {
                return DefaultSteps;
            }

            return requested > MaxSteps ? MaxSteps : requested;
        }

        private static Vector2 ParseVector2(JObject obj)
        {
            if (obj == null)
            {
                return Vector2.zero;
            }

            return new Vector2(obj.Value<float>("x"), obj.Value<float>("y"));
        }

        private static SimulateUiDragResult BuildResult(
            SimulateUiDragArgs parameters,
            Vector2 from,
            Vector2 to,
            int steps)
        {
            return new SimulateUiDragResult
            {
                Ok = true,
                GameObjectPath = parameters.GameObjectPath,
                FromX = from.x,
                FromY = from.y,
                ToX = to.x,
                ToY = to.y,
                Steps = steps,
                Button = parameters.Button,
                Message = $"Dragged '{parameters.GameObjectPath}' from {from} to {to} in {steps} step(s)."
            };
        }

        private static SimulateUiDragArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new SimulateUiDragArgs();
            }

            return JsonConvert.DeserializeObject<SimulateUiDragArgs>(args) ?? new SimulateUiDragArgs();
        }
    }

    internal class SimulateUiDragArgs
    {
        [JsonProperty("gameObjectPath")]
        public string GameObjectPath { get; set; } = string.Empty;

        [JsonProperty("to")]
        public JObject To { get; set; }

        [JsonProperty("from")]
        public JObject From { get; set; }

        [JsonProperty("steps")]
        public int Steps { get; set; } = 10;

        [JsonProperty("button")]
        public string Button { get; set; } = "left";
    }

    internal class SimulateUiDragResult
    {
        [JsonProperty("ok")]
        public bool Ok { get; set; }

        [JsonProperty("gameObjectPath")]
        public string GameObjectPath { get; set; }

        [JsonProperty("fromX")]
        public float FromX { get; set; }

        [JsonProperty("fromY")]
        public float FromY { get; set; }

        [JsonProperty("toX")]
        public float ToX { get; set; }

        [JsonProperty("toY")]
        public float ToY { get; set; }

        [JsonProperty("steps")]
        public int Steps { get; set; }

        [JsonProperty("button")]
        public string Button { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
#endif
