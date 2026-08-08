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

            if (ShouldRejectAsBusy(toolName))
            {
                return CreateBusyError(request, toolName);
            }

            var callToolResult = await ExecuteTool(toolName, argumentsJson);
            return JsonRpcResponse.Success(request.Id, callToolResult);
        }

        private static async Task<CallToolResult> ExecuteTool(string toolName, string argumentsJson)
        {
            try
            {
                var result = await McpToolRouter.Execute(toolName, argumentsJson);
                if (result is CallToolResult directResult)
                {
                    return directResult;
                }

                return CallToolResult.SuccessText(JsonConvert.SerializeObject(result));
            }
            catch (Exception ex)
            {
                return CallToolResult.ErrorText(ex.Message);
            }
        }

        /// <summary>
        /// コンパイル中・アセット更新中は、状態観測系を除くツールをビジーエラーで断る。
        /// 中途半端な状態で実行して誤った結果を返すより、再送可能なエラーを返すほうが安全。
        /// </summary>
        private static bool ShouldRejectAsBusy(string toolName)
        {
            if (EditorReadiness.IsReady)
            {
                return false;
            }

            return !EditorReadiness.IsAllowedWhileBusy(toolName);
        }

        private static JsonRpcResponse CreateBusyError(JsonRpcRequest request, string toolName)
        {
            var error = new JsonRpcError
            {
                Code = JsonRpcErrorCodes.ServerBusy,
                Message = $"server busy: {EditorReadiness.BusyReason}. '{toolName}' was not executed.",
                Data = EditorReadiness.BuildBusyDetail()
            };

            return JsonRpcResponse.Failure(request.Id, error);
        }
    }
}
