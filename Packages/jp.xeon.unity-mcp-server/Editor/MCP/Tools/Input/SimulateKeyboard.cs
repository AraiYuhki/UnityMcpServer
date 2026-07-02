#if MCP_INPUT_SYSTEM
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace UnityMcp.Tools.InputSimulation
{
    /// <summary>
    /// Input Systemの低レベルAPIでキーボード入力をシミュレートするツール。
    /// PlayMode中のみ動作し、InputActionにバインドされたゲーム内操作を再現する。
    /// </summary>
    public class SimulateKeyboard : IMcpTool
    {
        public string Name => "simulate_keyboard";

        public string Description =>
            "Simulate keyboard input via the Input System (PlayMode only). " +
            "Inject a snapshot of currently held keys to drive InputAction-based game logic. " +
            "Use action 'press' to hold the given keys, 'release' to release all keys, " +
            "or 'tap' to press and release them in one call. " +
            "Key names follow the Input System Key enum (e.g. 'W', 'Space', 'Enter', 'LeftArrow').";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"keys\":{\"type\":\"array\",\"items\":{\"type\":\"string\"},\"description\":\"Key names to set as pressed (Input System Key enum). Required for 'press' and 'tap'.\"}," +
            "\"action\":{\"type\":\"string\",\"enum\":[\"press\",\"release\",\"tap\"],\"description\":\"press: hold keys, release: release all keys, tap: press then release. Default: tap.\"}" +
            "},\"required\":[\"keys\"]}";

        public async Task<object> Execute(string args)
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("simulate_keyboard requires Play Mode. Enter Play Mode first.");
            }

            var parameters = ParseArgs(args);
            var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            var keys = ParseKeys(parameters.Keys);

            return await Dispatch(parameters.Action, keyboard, keys);
        }

        private static Task<SimulateKeyboardResult> Dispatch(string action, Keyboard keyboard, Key[] keys)
        {
            switch (action)
            {
                case "press":
                    return Task.FromResult(Press(keyboard, keys));
                case "release":
                    return Task.FromResult(Release(keyboard));
                case "tap":
                    return Tap(keyboard, keys);
                default:
                    throw new InvalidOperationException($"Unknown action: '{action}'. Use 'press', 'release', or 'tap'.");
            }
        }

        private static SimulateKeyboardResult Press(Keyboard keyboard, Key[] keys)
        {
            RequireKeys(keys, "press");
            QueueState(keyboard, keys);
            return BuildResult("press", keys, $"Pressed {keys.Length} key(s).");
        }

        private static SimulateKeyboardResult Release(Keyboard keyboard)
        {
            QueueState(keyboard, Array.Empty<Key>());
            return BuildResult("release", Array.Empty<Key>(), "Released all keys.");
        }

        private static async Task<SimulateKeyboardResult> Tap(Keyboard keyboard, Key[] keys)
        {
            RequireKeys(keys, "tap");
            QueueState(keyboard, keys);
            await InputSimulationUtility.WaitForNextPlayerLoopFrameAsync();
            QueueState(keyboard, Array.Empty<Key>());
            return BuildResult("tap", keys, $"Tapped {keys.Length} key(s).");
        }

        private static void QueueState(Keyboard keyboard, Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));

            // エディタループから InputSystem.Update() を呼ぶとエディタ用バッファに消費されて
            // プレイモード側に反映されないため、プレイヤーループへ処理を委ねる。
            EditorApplication.QueuePlayerLoopUpdate();
        }

        private static void RequireKeys(Key[] keys, string action)
        {
            if (keys.Length == 0)
            {
                throw new InvalidOperationException($"action '{action}' requires at least one key.");
            }
        }

        private static Key[] ParseKeys(string[] names)
        {
            if (names == null)
            {
                return Array.Empty<Key>();
            }

            var keys = new Key[names.Length];
            for (var i = 0; i < names.Length; i++)
            {
                keys[i] = ParseKey(names[i]);
            }

            return keys;
        }

        private static Key ParseKey(string name)
        {
            if (Enum.TryParse<Key>(name, true, out var key) && key != Key.None)
            {
                return key;
            }

            throw new InvalidOperationException($"Invalid key name: '{name}'. Use Input System Key enum values (e.g. 'W', 'Space').");
        }

        private static SimulateKeyboardResult BuildResult(string action, Key[] keys, string message)
        {
            var names = new string[keys.Length];
            for (var i = 0; i < keys.Length; i++)
            {
                names[i] = keys[i].ToString();
            }

            var warning = InputSimulationUtility.CheckEditorInputBehaviorWarning();
            if (warning != null)
            {
                message = $"{message} {warning}";
            }

            return new SimulateKeyboardResult
            {
                Ok = true,
                Action = action,
                Keys = names,
                Message = message
            };
        }

        private static SimulateKeyboardArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new SimulateKeyboardArgs();
            }

            return JsonConvert.DeserializeObject<SimulateKeyboardArgs>(args) ?? new SimulateKeyboardArgs();
        }
    }

    internal class SimulateKeyboardArgs
    {
        [JsonProperty("keys")]
        public string[] Keys { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; } = "tap";
    }

    internal class SimulateKeyboardResult
    {
        [JsonProperty("ok")]
        public bool Ok { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("keys")]
        public string[] Keys { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
#endif
