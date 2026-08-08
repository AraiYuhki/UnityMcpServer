using System;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityMcp.JsonRpc;

namespace UnityMcp.Transport
{
    /// <summary>
    /// JSON-RPCレスポンスをHTTPへ書き出す処理をまとめたクラス。
    /// どの経路でも Content-Type を必ず設定し、ボディ無しの応答を返さないことで
    /// クライアント側の "Unexpected content type: null" を防ぐ。
    /// </summary>
    public static class JsonRpcHttpWriter
    {
        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        /// <summary>
        /// application/json としてレスポンスを書き出す
        /// </summary>
        public static void WriteJson(HttpListenerContext ctx, JsonRpcResponse response, int statusCode = 200)
        {
            try
            {
                var buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response, JsonSettings));
                ctx.Response.StatusCode = statusCode;
                ctx.Response.ContentType = "application/json";
                ctx.Response.ContentLength64 = buffer.Length;
                ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
                ctx.Response.OutputStream.Close();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MCP] Failed to write JSON response: {e.Message}");
            }
        }

        /// <summary>
        /// text/event-stream としてレスポンスを書き出す
        /// </summary>
        public static void WriteSse(HttpListenerContext ctx, JsonRpcResponse response)
        {
            try
            {
                var writer = new SseWriter(ctx.Response);
                writer.WriteEvent(JsonConvert.SerializeObject(response, JsonSettings));
                writer.Close();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MCP] Failed to write SSE response: {e.Message}");
            }
        }

        /// <summary>
        /// 構造化されたJSON-RPCエラーを書き出す
        /// </summary>
        public static void WriteError(
            HttpListenerContext ctx,
            object requestId,
            int code,
            string message,
            object data = null,
            int statusCode = 200)
        {
            var error = new JsonRpcError
            {
                Code = code,
                Message = message,
                Data = data
            };

            WriteJson(ctx, JsonRpcResponse.Failure(requestId, error), statusCode);
        }
    }
}
