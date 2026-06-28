using System;
using System.Collections.Concurrent;
using UnityEditor;
using UnityEngine;

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
            if (Application.isBatchMode)
            {
                return;
            }
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
        /// キューに積まれた未実行アクションをすべて破棄する
        /// サーバー停止時に呼び出し、停止後のアクション残留を防ぐ
        /// </summary>
        public static void Clear()
        {
            while (Queue.TryDequeue(out _)) { }
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
                    Debug.LogException(e);
                }
            }
        }
    }
}
