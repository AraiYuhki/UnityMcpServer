using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Tools.Console
{
    /// <summary>
    /// Consoleログをメモリキャッシュするクラス。
    /// [InitializeOnLoad] によりDomain Reload後も自動で再登録される。
    /// 各エントリには単調増加するトークン（Id）を割り当て、「前回以降の新規ログだけ」を取得できるようにする。
    /// </summary>
    [InitializeOnLoad]
    public static class ConsoleLogCache
    {
        private const int MaxCapacity = 1000;
        private const string SequenceKey = "UnityMcp.Console.NextSequence";
        private const string CheckpointKey = "UnityMcp.Console.Checkpoint";

        private static readonly Queue<LogEntry> entries = new();
        private static readonly object lockObj = new();

        /// <summary>次に割り当てるトークン。ログ受信スレッドから触るためロック下でのみ操作する。</summary>
        private static long nextSequence;

        static ConsoleLogCache()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            nextSequence = LoadSequence();
            Application.logMessageReceivedThreaded += OnLogReceived;
            AssemblyReloadEvents.beforeAssemblyReload += PersistSequence;
        }

        /// <summary>
        /// 次に発行されるトークン。この値を控えておけば以降の新規ログだけを取得できる。
        /// </summary>
        public static long CurrentToken
        {
            get
            {
                lock (lockObj)
                {
                    return nextSequence;
                }
            }
        }

        /// <summary>
        /// キャッシュに残っている最も古いエントリのトークン。
        /// これより小さい sinceToken を指定した場合、その間のログは失われている。
        /// </summary>
        public static long OldestToken
        {
            get
            {
                lock (lockObj)
                {
                    return entries.Count == 0 ? nextSequence : entries.Peek().Id;
                }
            }
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
        /// 指定トークン以降に記録されたログエントリだけを返す
        /// </summary>
        public static LogEntry[] GetEntriesSince(long sinceToken)
        {
            var snapshot = GetEntries();
            var result = new List<LogEntry>();

            foreach (var entry in snapshot)
            {
                if (entry.Id >= sinceToken)
                {
                    result.Add(entry);
                }
            }

            return result.ToArray();
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
        /// 現在位置をチェックポイントとして記録し、直前のチェックポイントを返す。
        /// 状態遷移ツールがこの区間の新規エラーを要約するために使う。
        /// </summary>
        public static long Checkpoint()
        {
            var previous = LoadCheckpoint();
            var current = CurrentToken;
            SessionState.SetString(CheckpointKey, current.ToString());
            PersistSequence();
            return previous;
        }

        /// <summary>
        /// キャッシュをクリアする。トークンは巻き戻さない。
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

                entries.Enqueue(LogEntry.Create(nextSequence, condition, stackTrace, logType));
                nextSequence++;
            }
        }

        private static long LoadCheckpoint()
        {
            var stored = SessionState.GetString(CheckpointKey, string.Empty);
            return long.TryParse(stored, out var value) ? value : 0;
        }

        private static long LoadSequence()
        {
            var stored = SessionState.GetString(SequenceKey, string.Empty);
            return long.TryParse(stored, out var value) ? value : 0;
        }

        /// <summary>
        /// ドメインリロードでトークンが巻き戻らないよう、現在位置を退避する
        /// </summary>
        private static void PersistSequence()
        {
            SessionState.SetString(SequenceKey, CurrentToken.ToString());
        }
    }
}
