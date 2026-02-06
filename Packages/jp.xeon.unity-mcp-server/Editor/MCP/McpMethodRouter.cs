using System.Threading.Tasks;
using UnityMcp.Handlers;
using UnityMcp.JsonRpc;

namespace UnityMcp
{
    public class McpMethodRouter
    {
        private McpSession session;
        private InitializeHandler initializeHandler;
        private InitializedHandler initializedHandler;
        private PingHandler pingHandler;
        private ToolsListHandler toolsListHandler;
        private ToolsCallHandler toolsCallHandler;

        public McpSession Session => session;

        public McpMethodRouter(McpSession session)
        {
            this.session = session;
            initializeHandler = new InitializeHandler();
            initializedHandler = new InitializedHandler();
            pingHandler = new PingHandler();
            toolsListHandler = new ToolsListHandler();
            toolsCallHandler = new ToolsCallHandler();
        }

        public async Task<JsonRpcResponse> RouteAsync(JsonRpcRequest request)
        {
            if (!IsAllowedBeforeInitialization(request))
            {
                return CreateNotInitializedError(request);
            }

            switch (request.Method)
            {
                case "initialize":
                    return initializeHandler.Handle(request, session);
                case "notifications/initialized":
                    initializedHandler.Handle(request, session);
                    return null;
                case "ping":
                    return pingHandler.Handle(request);
                case "tools/list":
                    return toolsListHandler.Handle(request);
                case "tools/call":
                    return await toolsCallHandler.Handle(request);
                default:
                    return CreateMethodNotFoundError(request);
            }
        }

        private bool IsAllowedBeforeInitialization(JsonRpcRequest request)
        {
            if (session.State != McpSession.SessionState.Uninitialized)
            {
                return true;
            }

            return request.Method == "initialize" || request.Method == "ping";
        }

        private static JsonRpcResponse CreateNotInitializedError(JsonRpcRequest request)
        {
            var error = new JsonRpcError
            {
                Code = JsonRpcErrorCodes.InvalidRequest,
                Message = "Server not initialized. Send 'initialize' first."
            };

            return JsonRpcResponse.Failure(request.Id, error);
        }

        private static JsonRpcResponse CreateMethodNotFoundError(JsonRpcRequest request)
        {
            var error = new JsonRpcError
            {
                Code = JsonRpcErrorCodes.MethodNotFound,
                Message = $"Method not found: {request.Method}"
            };

            return JsonRpcResponse.Failure(request.Id, error);
        }
    }
}
