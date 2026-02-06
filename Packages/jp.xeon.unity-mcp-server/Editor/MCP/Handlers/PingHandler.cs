using Newtonsoft.Json.Linq;
using UnityMcp.JsonRpc;

namespace UnityMcp.Handlers
{
    public class PingHandler
    {
        public JsonRpcResponse Handle(JsonRpcRequest request)
        {
            return JsonRpcResponse.Success(request.Id, new JObject());
        }
    }
}
