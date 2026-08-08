using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
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

        /// <summary>ドメインリロードをまたいでセッションIDを保持するためのSessionStateキー</summary>
        private const string SessionIdKey = "UnityMcp.SessionId";

        /// <summary>
        /// メインスレッドでの実行待ちリクエスト。
        /// ドメインリロードで破棄する際、無応答にせずビジーエラーを返すために保持する。
        /// </summary>
        private static readonly ConcurrentDictionary<HttpListenerContext, JsonRpcRequest> pendingRequests = new();

        static McpServer()
        {
            if (Application.isBatchMode)
            {
                return;
            }
            Start();
            EditorApplication.quitting += Stop;
            // ドメインリロード前にリスナーを閉じないと、リロード後の Start で同じポートを bind できず起動失敗する
            AssemblyReloadEvents.beforeAssemblyReload += StopForReload;
        }

        /// <summary>
        /// Restarts the MCP server via the Unity Editor menu.
        /// </summary>
        [MenuItem("Tools/Restart MCP Server")]
        public static void Restart()
        {
            EditorApplication.quitting -= Stop;
            AssemblyReloadEvents.beforeAssemblyReload -= StopForReload;
            Stop();
            Start();
            EditorApplication.quitting += Stop;
            AssemblyReloadEvents.beforeAssemblyReload += StopForReload;
        }

        private static void Start()
        {
            try
            {
                var port = ResolvePort();

                // ドメインリロード時は以前のセッションIDを復元してReadyへ戻す
                var persistedId = SessionState.GetString(SessionIdKey, "");
                if (!string.IsNullOrEmpty(persistedId))
                {
                    session = new McpSession(persistedId);
                    session.Restore();
                }
                else
                {
                    session = new McpSession();
                }
                SessionState.SetString(SessionIdKey, session.SessionId);

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
            // 終了・手動再起動時はセッションIDをクリアして次回は新セッションを生成する
            SessionState.EraseString(SessionIdKey);
            StopListener();
        }

        /// <summary>
        /// ドメインリロード前にリスナーだけ閉じる。SessionId は保持して、リロード後の Start で復元する。
        /// </summary>
        private static void StopForReload()
        {
            StopListener();
        }

        private static void StopListener()
        {
            // 破棄する前に、待機中のリクエストへ「再送可能なビジーエラー」を返して無応答を防ぐ
            FailPendingRequests();
            McpDispatcher.Clear();

            try
            {
                listener?.Stop();
                listener?.Close();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MCP] Error while closing listener: {e.Message}");
            }
            finally
            {
                listener = null;
            }

            thread?.Join(TimeSpan.FromSeconds(3));
            thread = null;
        }

        /// <summary>
        /// 実行待ち・実行中のまま打ち切られるリクエストへ、構造化されたビジーエラーを返す。
        /// ドメインリロードで応答が消え、クライアント側が "Unexpected content type: null" になるのを防ぐ。
        /// </summary>
        private static void FailPendingRequests()
        {
            foreach (var pair in pendingRequests)
            {
                if (!TryClaimResponse(pair.Key))
                {
                    continue;
                }

                JsonRpcHttpWriter.WriteError(
                    pair.Key,
                    pair.Value?.Id,
                    JsonRpcErrorCodes.ServerBusy,
                    "server busy: domain reloading. The request was interrupted and is safe to resend as-is.",
                    new { retryable = true, reason = "domain reloading" });
            }
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
            var request = ReadRequest(ctx);
            if (request == null)
            {
                return;
            }

            bool isInitialize = request.Method == "initialize";
            if (!IsSessionHeaderValid(ctx, isInitialize))
            {
                return;
            }

            pendingRequests[ctx] = request;
            McpDispatcher.Enqueue(() => ExecuteAndRespond(ctx, request));
        }

        /// <summary>
        /// リクエストボディをJSON-RPCへ変換する。失敗時はパースエラーを返してnullを返す。
        /// </summary>
        private static JsonRpcRequest ReadRequest(HttpListenerContext ctx)
        {
            string body;
            using (var reader = new StreamReader(ctx.Request.InputStream))
            {
                body = reader.ReadToEnd();
            }

            try
            {
                var request = JsonConvert.DeserializeObject<JsonRpcRequest>(body);
                if (request != null)
                {
                    return request;
                }
            }
            catch (JsonException e)
            {
                WriteParseError(ctx, e.Message);
                return null;
            }

            WriteParseError(ctx, "Empty or invalid JSON-RPC request");
            return null;
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
                if (TryClaimResponse(ctx))
                {
                    SendRouteResult(ctx, response);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MCP] Error executing request: {e.Message}");
                if (TryClaimResponse(ctx))
                {
                    JsonRpcHttpWriter.WriteError(ctx, request.Id, JsonRpcErrorCodes.InternalError, e.Message);
                }
            }
        }

        /// <summary>
        /// このコンテキストへの応答権を取得する。
        /// ドメインリロードによる打ち切りとの二重応答を防ぐため、除去に成功した側だけが書き込む。
        /// </summary>
        private static bool TryClaimResponse(HttpListenerContext ctx)
        {
            return pendingRequests.TryRemove(ctx, out _);
        }

        private static void SendRouteResult(HttpListenerContext ctx, JsonRpcResponse response)
        {
            if (response == null)
            {
                RespondAccepted(ctx);
                return;
            }

            AddSessionHeader(ctx);

            if (AcceptsSse(ctx.Request))
            {
                JsonRpcHttpWriter.WriteSse(ctx, response);
            }
            else
            {
                JsonRpcHttpWriter.WriteJson(ctx, response);
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
            HttpListener current;
            while ((current = listener) != null && current.IsListening)
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

            // ボディ無しの400を返すとクライアントが content-type: null で失敗するため、必ずJSONで返す
            JsonRpcHttpWriter.WriteError(
                ctx,
                null,
                JsonRpcErrorCodes.InvalidSession,
                "Invalid or missing Mcp-Session-Id header. Send 'initialize' to start a new session.",
                new { retryable = false },
                400);
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

        private static void WriteParseError(HttpListenerContext ctx, string message)
        {
            JsonRpcHttpWriter.WriteError(ctx, null, JsonRpcErrorCodes.ParseError, message);
        }

        private static void RespondAccepted(HttpListenerContext ctx)
        {
            AddSessionHeader(ctx);
            // 202はボディを持たないため Content-Type は設定しない（空ボディをJSONと宣言しないため）
            ctx.Response.StatusCode = 202;
            ctx.Response.ContentLength64 = 0;
            ctx.Response.Close();
        }

        private static void RespondMethodNotAllowed(HttpListenerContext ctx)
        {
            JsonRpcHttpWriter.WriteError(
                ctx,
                null,
                JsonRpcErrorCodes.InvalidRequest,
                $"HTTP method not allowed: {ctx.Request.HttpMethod}",
                null,
                405);
        }
    }
}
