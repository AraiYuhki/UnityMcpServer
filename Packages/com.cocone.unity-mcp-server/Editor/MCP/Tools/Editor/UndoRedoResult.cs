using Newtonsoft.Json;

namespace UnityMcp.Tools.Editor
{
    /// <summary>
    /// Undo/Redo操作ツールの共通戻り値モデル
    /// </summary>
    internal class UndoRedoResult
    {
        /// <summary>操作種別（"undo" または "redo"）</summary>
        [JsonProperty("operation")]
        public string Operation { get; set; }

        /// <summary>実行した回数</summary>
        [JsonProperty("count")]
        public int Count { get; set; }

        /// <summary>操作結果のメッセージ</summary>
        [JsonProperty("message")]
        public string Message { get; set; }
    }

    /// <summary>
    /// Undo/Redo操作ツールの共通引数モデル
    /// </summary>
    internal class UndoRedoArgs
    {
        /// <summary>操作を繰り返す回数</summary>
        [JsonProperty("count")]
        public int Count { get; set; } = 1;
    }
}
