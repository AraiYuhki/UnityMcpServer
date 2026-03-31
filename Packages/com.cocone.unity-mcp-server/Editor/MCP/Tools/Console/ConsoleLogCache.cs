using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Tools.Console
{
    /// <summary>
    /// Consoleログをメモリキャッシュするクラス。
    /// [InitializeOnLoad] によりDomain Reload後も自動で再登録される。
    /// </summary>
    [InitializeOnLoad]
    public static class ConsoleLogCache
    {
        private const int MaxCapacity = 1000;

        private static readonly Queue<LogEntry> entries = new();
        private static readonly object lockObj = new();

        static ConsoleLogCache()
        {
            Application.logMessageReceivedThreaded += OnLogReceived;
        }

        /// <summary>
        /// キャッシュされているログエントリを配列として返す（スレッドセーフ）
        /// </summary>
        public static LogEntry[] GetEntries()
        {
            lock (lockObj)
            {
                return entries.ToArray();
            }
        }

        /// <summary>
        /// キャッシュされているログの件数
        /// </summary>
        public static int Count
        {
            get
            {
                lock (lockObj)
                {
                    return entries.Count;
                }
            }
        }

        /// <summary>
        /// キャッシュをクリアする
        /// </summary>
        public static void Clear()
        {
            lock (lockObj)
            {
                entries.Clear();
            }
        }

        private static void OnLogReceived(string condition, string stackTrace, LogType logType)
        {
            lock (lockObj)
            {
                if (entries.Count >= MaxCapacity)
                {
                    entries.Dequeue();
                }

                entries.Enqueue(LogEntry.Create(condition, stackTrace, logType));
            }
        }
    }
}
