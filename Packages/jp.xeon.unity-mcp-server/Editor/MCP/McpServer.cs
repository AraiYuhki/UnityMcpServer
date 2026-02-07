using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityMcp.JsonRpc;
using UnityMcp.Transport;

namespace UnityMcp
{
    /// <summary>
    /// MCP server running in the Unity Editor.
    /// Implements Streamable HTTP transport for the Model Context Protocol.
    /// Accepts HTTP requests and routes them through the JSON-RPC method router.
    /// </summary>
    [InitializeOnLoad]
    public static class McpServer
    {
        private static HttpListener listener;
        private static Thread thread;
        private static McpSession session;
        private static McpMethodRouter methodRouter;

        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        static McpServer()
        {
            Start();
            EditorApplication.quitting += Stop;
        }

        /// <summary>
        /// Restarts the MCP server via the Unity Editor menu.
        /// </summary>
        [MenuItem("Tools/Restart MCP Server")]
        public static void Restart()
        {
            EditorApplication.quitting -= Stop;
            Stop();
            Start();
            EditorApplication.quitting += Stop;
        }

        private static void Start()
        {
            try
            {
                var port = ResolvePort();

                session = new McpSession();
                methodRouter = new McpMethodRouter(session);
                McpToolRouter.Initialize();

                listener = new HttpListener();
                listener.Prefixes.Add($"http://localhost:{port}/mcp/");
                listener.Start();

                thread = new Thread(ListenLoop);
                thread.IsBackground = true;
                thread.Start();

                Debug.Log($"[MCP] Server started on http://localhost:{port}/mcp/");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MCP] Failed to start server: {e.Message}");
            }
        }

        private static void Stop()
        {
            listener?.Stop();
            listener?.Close();
            thread?.Join(TimeSpan.FromSeconds(3));
            thread = null;
        }

        private static int ResolvePort()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(McpServerSetting)}");
            if (guids is not { Length: > 0 })
            {
                return 7000;
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var setting = AssetDatabase.LoadAssetAtPath<McpServerSetting>(path);
            return setting != null ? setting.Port : 7000;
        }

        private static void ListenLoop()
        {
            while (listener.IsListening)
            {
                if (!TryAcceptConnection())
                {
                    break;
                }
            }
        }

        private static bool TryAcceptConnection()
        {
            try
            {
                var context = listener.GetContext();
                ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
                return true;
            }
            catch (HttpListenerException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        /// <summary>
        /// Top-level request handler. Catches unexpected errors and delegates by HTTP method.
        /// </summary>
        private static void HandleRequest(HttpListenerContext ctx)
        {
            try
            {
                DispatchByHttpMethod(ctx);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MCP] Request handling error: {e.Message}");
            }
        }

        private static void DispatchByHttpMethod(HttpListenerContext ctx)
        {
            switch (ctx.Request.HttpMethod)
            {
                case "POST":
                    HandlePost(ctx);
                    break;
                case "GET":
                    HandleGet(ctx);
                    break;
                case "DELETE":
                    HandleDelete(ctx);
                    break;
                default:
                    RespondMethodNotAllowed(ctx);
                    break;
            }
        }

        private static void HandlePost(HttpListenerContext ctx)
        {
            string body;
            using (var reader = new StreamReader(ctx.Request.InputStream))
            {
                body = reader.ReadToEnd();
            }

            JsonRpcRequest request;
            try
            {
                request = JsonConvert.DeserializeObject<JsonRpcRequest>(body);
            }
            catch (JsonException e)
            {
                WriteParseErrorResponse(ctx, e.Message);
                return;
            }

            if (request == null)
            {
                WriteParseErrorResponse(ctx, "Empty or invalid JSON-RPC request");
                return;
            }

            bool isInitialize = request.Method == "initialize";
            if (!IsSessionHeaderValid(ctx, isInitialize))
            {
                return;
            }

            McpDispatcher.Enqueue(() => ExecuteAndRespond(ctx, request));
        }

        /// <summary>
        /// Routes the request through McpMethodRouter and writes the response.
        /// Runs on the Unity main thread via McpDispatcher.
        /// </summary>
        private static async void ExecuteAndRespond(HttpListenerContext ctx, JsonRpcRequest request)
        {
            try
            {
                var response = await methodRouter.RouteAsync(request);
                SendRouteResult(ctx, request, response);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MCP] Error executing request: {e.Message}");
                WriteInternalErrorResponse(ctx, request.Id, e.Message);
            }
        }

        private static void SendRouteResult(
            HttpListenerContext ctx,
            JsonRpcRequest request,
            JsonRpcResponse response)
        {
            if (response == null)
            {
                RespondAccepted(ctx);
                return;
            }

            AddSessionHeader(ctx);

            if (AcceptsSse(ctx.Request))
            {
                WriteSseResponse(ctx, response);
            }
            else
            {
                WriteJsonResponse(ctx, response);
            }
        }

        /// <summary>
        /// Opens an SSE stream for server-to-client notifications.
        /// Keeps the connection alive until the server stops or the client disconnects.
        /// </summary>
        private static void HandleGet(HttpListenerContext ctx)
        {
            if (!IsSessionHeaderValid(ctx, false))
            {
                return;
            }

            AddSessionHeader(ctx);
            KeepSseConnectionAlive(ctx.Response);
        }

        private static void KeepSseConnectionAlive(HttpListenerResponse response)
        {
            var writer = new SseWriter(response);
            try
            {
                WaitWhileListening();
            }
            finally
            {
                TryCloseWriter(writer);
            }
        }

        private static void WaitWhileListening()
        {
            while (listener != null && listener.IsListening)
            {
                Thread.Sleep(1000);
            }
        }

        private static void TryCloseWriter(SseWriter writer)
        {
            try
            {
                writer.Close();
            }
            catch (Exception)
            {
                // Stream already closed by client disconnect or server shutdown
            }
        }

        /// <summary>
        /// Terminates the current session and prepares for a fresh initialize handshake.
        /// </summary>
        private static void HandleDelete(HttpListenerContext ctx)
        {
            if (!IsSessionHeaderValid(ctx, false))
            {
                return;
            }

            session = new McpSession();
            methodRouter = new McpMethodRouter(session);

            ctx.Response.StatusCode = 200;
            ctx.Response.Close();
        }

        private static bool IsSessionHeaderValid(HttpListenerContext ctx, bool skipValidation)
        {
            if (skipValidation)
            {
                return true;
            }

            var headerValue = ctx.Request.Headers["Mcp-Session-Id"];
            if (headerValue == session.SessionId)
            {
                return true;
            }

            ctx.Response.StatusCode = 400;
            ctx.Response.Close();
            return false;
        }

        private static void AddSessionHeader(HttpListenerContext ctx)
        {
            ctx.Response.Headers.Add("Mcp-Session-Id", session.SessionId);
        }

        private static bool AcceptsSse(HttpListenerRequest request)
        {
            var accept = request.Headers["Accept"];
            if (accept == null)
            {
                return false;
            }

            return accept.Contains("text/event-stream");
        }

        private static void WriteJsonResponse(HttpListenerContext ctx, JsonRpcResponse response)
        {
            try
            {
                var json = JsonConvert.SerializeObject(response, JsonSettings);
                var buffer = Encoding.UTF8.GetBytes(json);
                ctx.Response.ContentType = "application/json";
                ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
                ctx.Response.OutputStream.Close();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MCP] Failed to write JSON response: {e.Message}");
            }
        }

        private static void WriteSseResponse(HttpListenerContext ctx, JsonRpcResponse response)
        {
            try
            {
                var writer = new SseWriter(ctx.Response);
                var json = JsonConvert.SerializeObject(response, JsonSettings);
                writer.WriteEvent(json);
                writer.Close();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MCP] Failed to write SSE response: {e.Message}");
            }
        }

        private static void WriteParseErrorResponse(HttpListenerContext ctx, string message)
        {
            var error = new JsonRpcError
            {
                Code = JsonRpcErrorCodes.ParseError,
                Message = message
            };
            var response = JsonRpcResponse.Failure(null, error);
            WriteJsonResponse(ctx, response);
        }

        private static void WriteInternalErrorResponse(HttpListenerContext ctx, object requestId, string message)
        {
            var error = new JsonRpcError
            {
                Code = JsonRpcErrorCodes.InternalError,
                Message = message
            };
            var response = JsonRpcResponse.Failure(requestId, error);
            WriteJsonResponse(ctx, response);
        }

        private static void RespondAccepted(HttpListenerContext ctx)
        {
            AddSessionHeader(ctx);
            ctx.Response.StatusCode = 202;
            ctx.Response.Close();
        }

        private static void RespondMethodNotAllowed(HttpListenerContext ctx)
        {
            ctx.Response.StatusCode = 405;
            ctx.Response.Close();
        }
    }
}
