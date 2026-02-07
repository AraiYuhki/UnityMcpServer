using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityMcp.JsonRpc;
using UnityMcp.Models;

namespace UnityMcp.Handlers
{
    public class ToolsCallHandler
    {
        public async Task<JsonRpcResponse> Handle(JsonRpcRequest request)
        {
            var toolName = request.Params?.Value<string>("name");
            var arguments = request.Params?["arguments"];
            var argumentsJson = arguments?.ToString(Formatting.None) ?? "{}";

            CallToolResult callToolResult;

            try
            {
                var result = await McpToolRouter.Execute(toolName, argumentsJson);
                var serialized = JsonConvert.SerializeObject(result);
                callToolResult = CallToolResult.SuccessText(serialized);
            }
            catch (Exception ex)
            {
                callToolResult = CallToolResult.ErrorText(ex.Message);
            }

            return JsonRpcResponse.Success(request.Id, callToolResult);
        }
    }
}
