using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityMcp.JsonRpc;
using UnityMcp.Models;

namespace UnityMcp.Handlers
{
    public class ToolsListHandler
    {
        public JsonRpcResponse Handle(JsonRpcRequest request)
        {
            var tools = McpToolRouter.GetToolList();
            var toolInfos = new List<ToolInfo>();

            foreach (var tool in tools)
            {
                toolInfos.Add(ConvertToToolInfo(tool));
            }

            var result = new ToolsListResult
            {
                Tools = toolInfos
            };

            return JsonRpcResponse.Success(request.Id, result);
        }

        private static ToolInfo ConvertToToolInfo(IMcpTool tool)
        {
            return new ToolInfo
            {
                Name = tool.Name,
                Description = tool.Description,
                InputSchema = JObject.Parse(tool.InputSchema)
            };
        }
    }
}
