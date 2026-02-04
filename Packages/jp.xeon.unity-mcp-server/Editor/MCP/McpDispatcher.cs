using System;
using System.Collections.Concurrent;
using UnityEditor;

namespace UnityMcp
{
    /// <summary>
    /// バックグラウンドスレッドからメインスレッドへのディスパッチャー
    /// Unity APIはメインスレッドでのみ実行可能なため、
    /// HTTPリクエスト処理スレッドからの実行をキューイングする
    /// </summary>
    [InitializeOnLoad]
    public static class McpDispatcher
    {
        /// <summary>
        /// メインスレッドで実行するアクションのキュー
        /// スレッドセーフなConcurrentQueueを使用
        /// </summary>
        private static readonly ConcurrentQueue<Action> Queue = new();

        static McpDispatcher()
        {
            // EditorApplication.updateはメインスレッドで毎フレーム呼び出される
            EditorApplication.update += Drain;
        }

        /// <summary>
        /// メインスレッドで実行するアクションをキューに追加する
        /// </summary>
        /// <param name="action">実行するアクション</param>
        public static void Enqueue(Action action)
        {
            Queue.Enqueue(action);
        }

        /// <summary>
        /// キューに溜まったアクションをすべて実行する
        /// EditorApplication.updateから毎フレーム呼び出される
        /// </summary>
        private static void Drain()
        {
            while (Queue.TryDequeue(out var action))
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }
        }
    }
}
