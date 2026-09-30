#if MCP_UGUI
using System;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityMcp.Tools.InputSimulation
{
    /// <summary>
    /// uGUIのテキスト入力欄(InputField / TMP_InputField)へ直接テキストを流し込むツール。
    /// PlayMode中のみ動作する。simulate_keyboardは低レベルのInput Systemイベントを経由するため、
    /// エディタウィンドウがOSフォーカスを持たない自動化環境(このMCPサーバーの典型的な利用形態)では
    /// プレイヤーループが進まずキー入力が反映されないことがある(docs/MCP_INPUT_EMULATION_REPORT.md参照)。
    /// このツールはInputFieldのAPIを直接呼ぶため、OSフォーカスやInput Systemのルーティングに依存しない。
    /// TMP_InputFieldはTextMeshProへの直接参照を持たずリフレクションで操作する(パッケージ未導入でも
    /// コンパイルできるようにするため)。
    /// </summary>
    public class SimulateUiTextInput : IMcpTool
    {
        private const string TmpInputFieldTypeName = "TMPro.TMP_InputField, Unity.TextMeshPro";

        public string Name => "simulate_ui_text_input";

        public string Description =>
            "Set the text of a uGUI text input field (UnityEngine.UI.InputField or TMPro.TMP_InputField) " +
            "directly via its API (PlayMode only). Use this instead of simulate_keyboard for typing into " +
            "input fields: simulate_keyboard drives the low-level Input System, which requires the Editor " +
            "window to have OS focus and the player loop to be advancing, neither of which hold when the " +
            "Editor runs headless/unfocused (the common case for this server). This tool bypasses that by " +
            "calling the field's text setter and invoking onValueChanged directly, so it works regardless of " +
            "window focus. Set submit=true to also invoke onEndEdit/onSubmit as if Enter was pressed. " +
            "Specify the GameObject by its hierarchy path from a scene root (e.g. 'Canvas/Panel/NameInput').";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"gameObjectPath\":{\"type\":\"string\",\"description\":\"Hierarchy path from scene root to the InputField/TMP_InputField GameObject (e.g. 'Canvas/Panel/NameInput')\"}," +
            "\"text\":{\"type\":\"string\",\"description\":\"Text to set. Replaces the field's current content entirely.\"}," +
            "\"submit\":{\"type\":\"boolean\",\"description\":\"If true, also invoke onEndEdit/onSubmit after setting the text (as if Enter was pressed). Default: false.\",\"default\":false}" +
            "},\"required\":[\"gameObjectPath\",\"text\"]}";

        public Task<object> Execute(string args)
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("simulate_ui_text_input requires Play Mode. Enter Play Mode first.");
            }

            var parameters = ParseArgs(args);
            var target = UiSimulationUtility.RequireGameObject(parameters.GameObjectPath);

            var inputField = target.GetComponent<InputField>();
            if (inputField != null)
            {
                return Task.FromResult<object>(ApplyToInputField(inputField, parameters));
            }

            var tmpField = FindTmpInputField(target);
            if (tmpField != null)
            {
                return Task.FromResult<object>(ApplyToTmpInputField(tmpField, parameters));
            }

            throw new InvalidOperationException(
                $"GameObject '{parameters.GameObjectPath}' has neither an InputField nor a TMP_InputField component.");
        }

        private static SimulateUiTextInputResult ApplyToInputField(InputField field, SimulateUiTextInputArgs parameters)
        {
            field.text = parameters.Text;
            if (parameters.Submit)
            {
                field.onEndEdit?.Invoke(field.text);
                var eventData = new PointerEventData(EventSystem.current);
                ExecuteEvents.Execute(field.gameObject, eventData, ExecuteEvents.submitHandler);
            }

            return BuildResult(parameters, "InputField", field.text);
        }

        private static SimulateUiTextInputResult ApplyToTmpInputField(Component field, SimulateUiTextInputArgs parameters)
        {
            var type = field.GetType();
            var textProperty = type.GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
            if (textProperty == null || !textProperty.CanWrite)
            {
                throw new InvalidOperationException("TMP_InputField.text property was not found via reflection.");
            }

            textProperty.SetValue(field, parameters.Text);

            if (parameters.Submit)
            {
                var onEndEdit = type.GetField("onEndEdit", BindingFlags.Public | BindingFlags.Instance)?.GetValue(field);
                InvokeUnityEvent(onEndEdit, (string)textProperty.GetValue(field));
                var eventData = new PointerEventData(EventSystem.current);
                ExecuteEvents.Execute(field.gameObject, eventData, ExecuteEvents.submitHandler);
            }

            return BuildResult(parameters, "TMP_InputField", (string)textProperty.GetValue(field));
        }

        private static void InvokeUnityEvent(object unityEvent, string value)
        {
            if (unityEvent == null)
            {
                return;
            }

            var invoke = unityEvent.GetType().GetMethod("Invoke", new[] { typeof(string) });
            invoke?.Invoke(unityEvent, new object[] { value });
        }

        private static Component FindTmpInputField(GameObject target)
        {
            var type = Type.GetType(TmpInputFieldTypeName);
            return type == null ? null : target.GetComponent(type);
        }

        private static SimulateUiTextInputResult BuildResult(
            SimulateUiTextInputArgs parameters, string fieldType, string finalText)
        {
            return new SimulateUiTextInputResult
            {
                Ok = true,
                GameObjectPath = parameters.GameObjectPath,
                FieldType = fieldType,
                Text = finalText,
                Submitted = parameters.Submit,
                Message = $"Set {fieldType} '{parameters.GameObjectPath}' text to \"{finalText}\"."
                          + (parameters.Submit ? " Submitted (onEndEdit/onSubmit invoked)." : string.Empty)
            };
        }

        private static SimulateUiTextInputArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                throw new InvalidOperationException("gameObjectPath and text are required.");
            }

            var parameters = JsonConvert.DeserializeObject<SimulateUiTextInputArgs>(args);
            if (parameters == null || string.IsNullOrEmpty(parameters.GameObjectPath))
            {
                throw new InvalidOperationException("gameObjectPath is required.");
            }

            if (parameters.Text == null)
            {
                throw new InvalidOperationException("text is required.");
            }

            return parameters;
        }
    }

    internal class SimulateUiTextInputArgs
    {
        [JsonProperty("gameObjectPath")]
        public string GameObjectPath { get; set; } = string.Empty;

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("submit")]
        public bool Submit { get; set; }
    }

    internal class SimulateUiTextInputResult
    {
        [JsonProperty("ok")]
        public bool Ok { get; set; }

        [JsonProperty("gameObjectPath")]
        public string GameObjectPath { get; set; }

        [JsonProperty("fieldType")]
        public string FieldType { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("submitted")]
        public bool Submitted { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
#endif
