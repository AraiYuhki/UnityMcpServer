using Newtonsoft.Json;

namespace UnityMcp.Models
{
    public class TextContent
    {
        [JsonProperty("type")]
        public string Type { get; } = "text";

        [JsonProperty("text")]
        public string Text { get; set; }
    }
}
