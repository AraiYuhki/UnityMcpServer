using Newtonsoft.Json;

namespace UnityMcp.JsonRpc
{
    public class JsonRpcResponse
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonProperty("id")]
        public object Id { get; set; }

        [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
        public object Result { get; set; }

        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public JsonRpcError Error { get; set; }

        public static JsonRpcResponse Success(object id, object result)
        {
            return new JsonRpcResponse
            {
                Id = id,
                Result = result
            };
        }

        public static JsonRpcResponse Failure(object id, JsonRpcError error)
        {
            return new JsonRpcResponse
            {
                Id = id,
                Error = error
            };
        }
    }
}
