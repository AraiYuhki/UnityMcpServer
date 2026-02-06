using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityMcp.Models
{
    public class InitializeParams
    {
        [JsonProperty("protocolVersion")]
        public string ProtocolVersion { get; set; }

        [JsonProperty("capabilities")]
        public JObject Capabilities { get; set; }

        [JsonProperty("clientInfo")]
        public ClientInfo ClientInfo { get; set; }
    }
}
