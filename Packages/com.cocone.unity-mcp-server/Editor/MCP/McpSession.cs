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

        /// <summary>
        /// ドメインリロード後の復元用。既存のセッションIDを引き継いで生成する
        /// </summary>
        /// <param name="existingSessionId">復元するセッションID</param>
        public McpSession(string existingSessionId)
        {
            SessionId = existingSessionId;
            State = SessionState.Uninitialized;
        }

        /// <summary>
        /// ドメインリロード後にReady状態へ復元する
        /// </summary>
        public void Restore()
        {
            State = SessionState.Ready;
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
