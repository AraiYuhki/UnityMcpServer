using Newtonsoft.Json;

namespace UnityMcp.Models
{
    /// <summary>
    /// MCP仕様に準拠した画像コンテンツ
    /// base64エンコードされた画像データを保持する
    /// </summary>
    public class ImageContent
    {
        [JsonProperty("type")]
        public string Type { get; } = "image";

        [JsonProperty("data")]
        public string Data { get; set; }

        [JsonProperty("mimeType")]
        public string MimeType { get; set; }
    }
}
