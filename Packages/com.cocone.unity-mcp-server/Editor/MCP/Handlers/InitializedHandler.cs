using UnityMcp.JsonRpc;

namespace UnityMcp.Handlers
{
    public class InitializedHandler
    {
        public void Handle(JsonRpcRequest request, McpSession session)
        {
            session.MarkReady();
        }
    }
}
