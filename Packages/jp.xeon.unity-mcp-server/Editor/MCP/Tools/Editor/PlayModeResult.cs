using Newtonsoft.Json;

namespace UnityMcp.Tools.Editor
{
    /// <summary>
    /// PlayMode関連ツールの共通戻り値モデル
    /// </summary>
    internal class PlayModeResult
    {
        /// <summary>現在の状態（"playing", "paused", "editing"）</summary>
        [JsonProperty("state")]
        public string State { get; set; }

        /// <summary>再生中かどうか</summary>
        [JsonProperty("isPlaying")]
        public bool IsPlaying { get; set; }

        /// <summary>一時停止中かどうか</summary>
        [JsonProperty("isPaused")]
        public bool IsPaused { get; set; }

        /// <summary>操作結果のメッセージ</summary>
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
