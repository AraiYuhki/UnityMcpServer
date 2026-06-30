#if MCP_INPUT_SYSTEM
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace UnityMcp.Tools.InputSimulation
{
    /// <summary>
    /// Input Systemの低レベルAPIでマウス入力をシミュレートするツール。
    /// PlayMode中のみ動作し、座標移動・ボタン操作・スクロールを注入する。
    /// </summary>
    public class SimulateMouse : IMcpTool
    {
        public string Name => "simulate_mouse";

        public string Description =>
            "Simulate mouse input via the Input System (PlayMode only). " +
            "Move the cursor, press/release/click a button, or scroll. " +
            "Position uses screen pixels with the bottom-left as origin. " +
            "If position is omitted, the current cursor position is kept. " +
            "Drives InputAction-based logic and uGUI when using InputSystemUIInputModule.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"action\":{\"type\":\"string\",\"enum\":[\"move\",\"press\",\"release\",\"click\",\"scroll\"],\"description\":\"Mouse action to perform. Default: click.\"}," +
            "\"position\":{\"type\":\"object\",\"properties\":{\"x\":{\"type\":\"number\"},\"y\":{\"type\":\"number\"}},\"description\":\"Screen position in pixels (bottom-left origin). Optional.\"}," +
            "\"button\":{\"type\":\"string\",\"enum\":[\"left\",\"right\",\"middle\"],\"description\":\"Mouse button for press/release/click. Default: left.\"}," +
            "\"scroll\":{\"type\":\"object\",\"properties\":{\"x\":{\"type\":\"number\"},\"y\":{\"type\":\"number\"}},\"description\":\"Scroll delta for the 'scroll' action. Optional.\"}" +
            "},\"required\":[]}";

        public Task<object> Execute(string args)
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("simulate_mouse requires Play Mode. Enter Play Mode first.");
            }

            var parameters = ParseArgs(args);
            var mouse = Mouse.current ?? InputSystem.AddDevice<Mouse>();
            var position = ResolvePosition(parameters, mouse);

            var result = Dispatch(parameters, mouse, position);
            return Task.FromResult<object>(result);
        }

        private static SimulateMouseResult Dispatch(SimulateMouseArgs parameters, Mouse mouse, Vector2 position)
        {
            switch (parameters.Action)
            {
                case "move":
                    return Move(mouse, position);
                case "press":
                    return SetButton(mouse, position, parameters.Button, true);
                case "release":
                    return SetButton(mouse, position, parameters.Button, false);
                case "click":
                    return Click(mouse, position, parameters.Button);
                case "scroll":
                    return Scroll(mouse, position, parameters.Scroll);
                default:
                    throw new InvalidOperationException($"Unknown action: '{parameters.Action}'.");
            }
        }

        private static SimulateMouseResult Move(Mouse mouse, Vector2 position)
        {
            QueueState(mouse, new MouseState { position = position });
            return BuildResult("move", position, $"Moved cursor to {position}.");
        }

        private static SimulateMouseResult SetButton(Mouse mouse, Vector2 position, string button, bool pressed)
        {
            var mouseButton = ParseButton(button);
            var state = new MouseState { position = position }.WithButton(mouseButton, pressed);
            QueueState(mouse, state);

            var action = pressed ? "press" : "release";
            return BuildResult(action, position, $"{action} {mouseButton} button at {position}.");
        }

        private static SimulateMouseResult Click(Mouse mouse, Vector2 position, string button)
        {
            var mouseButton = ParseButton(button);
            QueueState(mouse, new MouseState { position = position }.WithButton(mouseButton, true));
            QueueState(mouse, new MouseState { position = position }.WithButton(mouseButton, false));
            return BuildResult("click", position, $"Clicked {mouseButton} button at {position}.");
        }

        private static SimulateMouseResult Scroll(Mouse mouse, Vector2 position, JObject scroll)
        {
            var delta = ParseVector2(scroll);
            QueueState(mouse, new MouseState { position = position, scroll = delta });
            return BuildResult("scroll", position, $"Scrolled by {delta} at {position}.");
        }

        private static void QueueState(Mouse mouse, MouseState state)
        {
            InputSystem.QueueStateEvent(mouse, state);
            InputSystem.Update();
        }

        private static Vector2 ResolvePosition(SimulateMouseArgs parameters, Mouse mouse)
        {
            if (parameters.Position == null)
            {
                return mouse.position.ReadValue();
            }

            return ParseVector2(parameters.Position);
        }

        private static MouseButton ParseButton(string button)
        {
            switch (button)
            {
                case "left":
                    return MouseButton.Left;
                case "right":
                    return MouseButton.Right;
                case "middle":
                    return MouseButton.Middle;
                default:
                    throw new InvalidOperationException($"Invalid button: '{button}'. Use 'left', 'right', or 'middle'.");
            }
        }

        private static Vector2 ParseVector2(JObject obj)
        {
            if (obj == null)
            {
                return Vector2.zero;
            }

            var x = obj.Value<float>("x");
            var y = obj.Value<float>("y");
            return new Vector2(x, y);
        }

        private static SimulateMouseResult BuildResult(string action, Vector2 position, string message)
        {
            return new SimulateMouseResult
            {
                Ok = true,
                Action = action,
                X = position.x,
                Y = position.y,
                Message = message
            };
        }

        private static SimulateMouseArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new SimulateMouseArgs();
            }

            return JsonConvert.DeserializeObject<SimulateMouseArgs>(args) ?? new SimulateMouseArgs();
        }
    }

    internal class SimulateMouseArgs
    {
        [JsonProperty("action")]
        public string Action { get; set; } = "click";

        [JsonProperty("position")]
        public JObject Position { get; set; }

        [JsonProperty("button")]
        public string Button { get; set; } = "left";

        [JsonProperty("scroll")]
        public JObject Scroll { get; set; }
    }

    internal class SimulateMouseResult
    {
        [JsonProperty("ok")]
        public bool Ok { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("x")]
        public float X { get; set; }

        [JsonProperty("y")]
        public float Y { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
#endif
