namespace UnityMcp.JsonRpc
{
    public static class JsonRpcErrorCodes
    {
        public static int ParseError => -32700;
        public static int InvalidRequest => -32600;
        public static int MethodNotFound => -32601;
        public static int InvalidParams => -32602;
        public static int InternalError => -32603;
    }
}
