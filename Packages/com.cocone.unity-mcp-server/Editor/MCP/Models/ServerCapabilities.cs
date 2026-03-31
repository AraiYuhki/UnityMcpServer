using Newtonsoft.Json;

namespace UnityMcp.Models
{
    public class ServerCapabilities
    {
        [JsonProperty("tools")]
        public ToolsCapability Tools { get; set; }
    }

    public class ToolsCapability
    {
        [JsonProperty("listChanged")]
        public bool ListChanged { get; set; }
    }
}
