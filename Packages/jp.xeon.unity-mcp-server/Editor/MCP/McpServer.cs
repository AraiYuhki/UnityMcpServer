using System;
using System.Net;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace UnityMcp
{
    /// <summary>
    /// Unity Editor上で動作するMCPサーバー
    /// HTTPリクエストを受け付け、登録されたツールを実行する
    /// </summary>
    [InitializeOnLoad]
    public static class McpServer
    {
        private static HttpListener listener;
        private static Thread thread;
        
        /// <summary>
        /// MCPサーバーをリスタートする
        /// </summary>
        [MenuItem("Tools/Restart MCP Server")]
        public static void Restart()
        {
            EditorApplication.quitting -= Stop;
            Stop();
            Start();
            EditorApplication.quitting += Stop;
        }

        static McpServer()
        {
            Start();
            EditorApplication.quitting += Stop;
        }

        /// <summary>
        /// サーバーを起動する
        /// McpServerSettingアセットからポート番号を取得し、HTTPリスナーを開始する
        /// </summary>
        private static void Start()
        {
            try
            {
                // 設定アセットからポート番号を取得（見つからない場合はデフォルト7000）
                var guids = AssetDatabase.FindAssets($"t:{nameof(McpServerSetting)}");
                var port = 7000;
                if (guids is { Length: > 0 })
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    var setting = AssetDatabase.LoadAssetAtPath<McpServerSetting>(path);
                    if (setting != null)
                        port = setting.Port;
                }

                McpToolRouter.Initialize();

                listener = new HttpListener();
                listener.Prefixes.Add($"http://localhost:{port}/mcp/");
                listener.Start();

                // バックグラウンドスレッドでリクエストを待ち受ける
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

        /// <summary>
        /// サーバーを停止する
        /// Editor終了時に呼び出される
        /// </summary>
        private static void Stop()
        {
            listener?.Stop();
            listener?.Close();
            // リスナー停止後、スレッドの終了を待機する
            thread?.Join(TimeSpan.FromSeconds(3));
            thread = null;
        }

        /// <summary>
        /// HTTPリクエストを待ち受けるループ
        /// バックグラウンドスレッドで実行される
        /// </summary>
        private static void ListenLoop()
        {
            while (listener.IsListening)
            {
                try
                {
                    var context = listener.GetContext();
                    HandleRequest(context);
                }
                catch (HttpListenerException)
                {
                    // サーバー停止時にGetContext()が例外をスローするため、正常終了として扱う
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// HTTPリクエストを処理する
        /// リクエストボディをパースし、メインスレッドでツール実行をキューイングする
        /// </summary>
        private static void HandleRequest(HttpListenerContext ctx)
        {
            try
            {
                using var reader = new System.IO.StreamReader(ctx.Request.InputStream);
                var body = reader.ReadToEnd();
                var request = JsonUtility.FromJson<McpRequest>(body);

                // Unity APIはメインスレッドでのみ実行可能なため、Dispatcherを経由する
                McpDispatcher.Enqueue(() => ExecuteRequest(ctx, request));
            }
            catch (Exception e)
            {
                WriteResponse(ctx, new McpResponse
                {
                    ok = false,
                    error = e.Message
                });
            }
        }

        /// <summary>
        /// ツールを実行し、結果をレスポンスとして返す
        /// メインスレッドで実行される
        /// </summary>
        private static async void ExecuteRequest(HttpListenerContext ctx, McpRequest request)
        {
            var response = new McpResponse();
            try
            {
                var executeResult = await McpToolRouter.Execute(request);
                response.result = JsonUtility.ToJson(executeResult);
                response.ok = true;
            }
            catch (Exception e)
            {
                response.ok = false;
                response.error = e.Message;
            }

            WriteResponse(ctx, response);
        }

        /// <summary>
        /// HTTPレスポンスをJSON形式で書き込む
        /// </summary>
        private static void WriteResponse(HttpListenerContext ctx, McpResponse response)
        {
            try
            {
                var json = JsonUtility.ToJson(response);
                var buffer = Encoding.UTF8.GetBytes(json);

                ctx.Response.ContentType = "application/json";
                ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
                ctx.Response.OutputStream.Close();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MCP] Failed to write response: {e.Message}");
            }
        }
    }
}
