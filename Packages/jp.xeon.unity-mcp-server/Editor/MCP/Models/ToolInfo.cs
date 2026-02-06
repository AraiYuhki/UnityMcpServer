using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityMcp.Models
{
    public class ToolInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("inputSchema")]
        public JObject InputSchema { get; set; }
    }
}
