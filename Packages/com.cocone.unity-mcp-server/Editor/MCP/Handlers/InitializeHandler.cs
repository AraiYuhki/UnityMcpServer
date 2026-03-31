using UnityMcp.JsonRpc;
using UnityMcp.Models;

namespace UnityMcp.Handlers
{
    public class InitializeHandler
    {
        private const string SupportedProtocolVersion = "2025-03-26";

        public JsonRpcResponse Handle(JsonRpcRequest request, McpSession session)
        {
            var initParams = request.Params?.ToObject<InitializeParams>();
            var clientProtocol = initParams?.ProtocolVersion;
            var clientInfo = initParams?.ClientInfo;

            session.MarkInitializing(clientProtocol, clientInfo);

            var result = new InitializeResult
            {
                ProtocolVersion = SupportedProtocolVersion,
                ServerInfo = new ServerInfo
                {
                    Name = "unity-mcp-server",
                    Version = "1.0.0"
                },
                Capabilities = new ServerCapabilities
                {
                    Tools = new ToolsCapability
                    {
                        ListChanged = false
                    }
                }
            };

            return JsonRpcResponse.Success(request.Id, result);
        }
    }
}
