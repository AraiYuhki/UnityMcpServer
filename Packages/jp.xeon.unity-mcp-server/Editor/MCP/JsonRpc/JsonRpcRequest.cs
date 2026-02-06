using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityMcp.JsonRpc
{
    public class JsonRpcRequest
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonProperty("id")]
        public object Id { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("params")]
        public JObject Params { get; set; }

        public bool IsNotification => Id == null;
    }
}
