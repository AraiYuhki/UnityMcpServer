using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;

namespace UnityMcp.Tools.Editor
{
    /// <summary>
    /// Unity EditorのUndo操作を実行するツール
    /// </summary>
    public class UndoOperation : IMcpTool
    {
        public string Name => "undo";

        public string Description =>
            "Undo the last operation in the Unity Editor. " +
            "Equivalent to Ctrl+Z. " +
            "Supports multiple steps by specifying count.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{\"count\":{\"type\":\"integer\",\"description\":\"Number of undo steps (default: 1)\",\"default\":1}},\"required\":[]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);
            var count = parameters.Count;

            for (var i = 0; i < count; i++)
            {
                Undo.PerformUndo();
            }

            var result = new UndoRedoResult
            {
                Operation = "undo",
                Count = count,
                Message = $"Performed {count} undo step(s)."
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
