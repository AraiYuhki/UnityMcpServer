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
        private const int DefaultHoldMs = 500;
        private const int MaxHoldMs = 30000;

        public string Name => "simulate_keyboard";

        public string Description =>
            "Simulate keyboard input via the Input System (PlayMode only). " +
            "Inject a snapshot of currently held keys to drive InputAction-based game logic. " +
            "Use action 'press' to hold the given keys, 'release' to release all keys, " +
            "'tap' to press and release them in one call, or 'hold' to keep them pressed for durationMs " +
            "while the player loop keeps running (for continuous movement input). " +
            "Key names follow the Input System Key enum (e.g. 'W', 'Space', 'Enter', 'LeftArrow').";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"keys\":{\"type\":\"array\",\"items\":{\"type\":\"string\"},\"description\":\"Key names to set as pressed (Input System Key enum). Required for 'press' and 'tap'.\"}," +
            "\"action\":{\"type\":\"string\",\"enum\":[\"press\",\"release\",\"tap\",\"hold\"],\"description\":\"press: hold keys and return immediately, release: release all keys, tap: press then release, hold: keep pressed for durationMs then release. Default: tap.\"}," +
            "\"durationMs\":{\"type\":\"integer\",\"description\":\"How long to keep the keys pressed for action 'hold' (default: 500, max: 30000)\",\"default\":500}" +
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

            return await Dispatch(parameters, keyboard, keys);
        }

        private static Task<SimulateKeyboardResult> Dispatch(
            SimulateKeyboardArgs parameters,
            Keyboard keyboard,
            Key[] keys)
        {
            switch (parameters.Action)
            {
                case "press":
                    return Task.FromResult(Press(keyboard, keys));
                case "release":
                    return Task.FromResult(Release(keyboard));
                case "tap":
                    return Tap(keyboard, keys);
                case "hold":
                    return Hold(keyboard, keys, ResolveDuration(parameters.DurationMs));
                default:
                    throw new InvalidOperationException(
                        $"Unknown action: '{parameters.Action}'. Use 'press', 'release', 'tap', or 'hold'.");
            }
        }

        /// <summary>
        /// 指定時間キーを押し続けてから離す。移動などの継続入力を再現する。
        /// </summary>
        private static async Task<SimulateKeyboardResult> Hold(Keyboard keyboard, Key[] keys, int durationMs)
        {
            RequireKeys(keys, "hold");
            QueueState(keyboard, keys);
            await InputSimulationUtility.HoldAsync(durationMs);
            QueueState(keyboard, Array.Empty<Key>());
            return BuildResult("hold", keys, $"Held {keys.Length} key(s) for {durationMs}ms.");
        }

        private static int ResolveDuration(int requested)
        {
            if (requested <= 0)
            {
                return DefaultHoldMs;
            }

            return requested > MaxHoldMs ? MaxHoldMs : requested;
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

        [JsonProperty("durationMs")]
        public int DurationMs { get; set; } = 500;
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
