using System;
using UnityMcp.Models;

namespace UnityMcp
{
    public class McpSession
    {
        public enum SessionState
        {
            Uninitialized,
            Initializing,
            Ready
        }

        public string SessionId { get; }
        public SessionState State { get; private set; }
        public string ProtocolVersion { get; private set; }
        public ClientInfo ClientInfo { get; private set; }
        public bool IsReady => State == SessionState.Ready;

        public McpSession()
        {
            SessionId = Guid.NewGuid().ToString();
            State = SessionState.Uninitialized;
        }

        public void MarkInitializing(string protocolVersion, ClientInfo clientInfo)
        {
            ProtocolVersion = protocolVersion;
            ClientInfo = clientInfo;
            State = SessionState.Initializing;
        }

        public void MarkReady()
        {
            State = SessionState.Ready;
        }
    }
}
