namespace UnityMcp.JsonRpc
{
    public static class JsonRpcErrorCodes
    {
        public static int ParseError => -32700;
        public static int InvalidRequest => -32600;
        public static int MethodNotFound => -32601;
        public static int InvalidParams => -32602;
        public static int InternalError => -32603;

        /// <summary>
        /// サーバーがビジー（コンパイル中・ドメインリロード中・アセット更新中）で処理できない。
        /// 同一リクエストの安全な再送が可能であることを示す。
        /// </summary>
        public static int ServerBusy => -32001;

        /// <summary>
        /// セッションIDが一致しない。initialize からやり直す必要がある。
        /// </summary>
        public static int InvalidSession => -32002;
    }
}
