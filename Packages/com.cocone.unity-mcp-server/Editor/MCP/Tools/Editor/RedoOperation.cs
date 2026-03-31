using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;

namespace UnityMcp.Tools.Editor
{
    /// <summary>
    /// Unity EditorのRedo操作を実行するツール
    /// </summary>
    public class RedoOperation : IMcpTool
    {
        public string Name => "redo";

        public string Description =>
            "Redo the last undone operation in the Unity Editor. " +
            "Equivalent to Ctrl+Shift+Z. " +
            "Supports multiple steps by specifying count.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{\"count\":{\"type\":\"integer\",\"description\":\"Number of redo steps (default: 1)\",\"default\":1}},\"required\":[]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);
            var count = parameters.Count;

            for (var i = 0; i < count; i++)
            {
                Undo.PerformRedo();
            }

            var result = new UndoRedoResult
            {
                Operation = "redo",
                Count = count,
                Message = $"Performed {count} redo step(s)."
            };

            return Task.FromResult<object>(result);
        }

        private static UndoRedoArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new UndoRedoArgs();
            }

            return JsonConvert.DeserializeObject<UndoRedoArgs>(args) ?? new UndoRedoArgs();
        }
    }
}
