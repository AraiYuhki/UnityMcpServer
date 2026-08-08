using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace UnityMcp.Tools.Compile
{
    /// <summary>
    /// コンパイルメッセージと進行状態を保持するクラス。
    /// Domain Reload後も自動再登録されるよう [InitializeOnLoad] を使用し、
    /// 状態・結果は SessionState へ退避してリロードを跨いでも失われないようにする。
    /// </summary>
    [InitializeOnLoad]
    public static class CompilationCache
    {
        /// <summary>Pendingのまま動き出さない場合に完了扱いへ戻すまでの猶予</summary>
        private const int PendingTimeoutSeconds = 60;

        private const string StateKey = "UnityMcp.Compile.State";
        private const string MessagesKey = "UnityMcp.Compile.Messages";
        private const string FinishedAtKey = "UnityMcp.Compile.FinishedAtTicks";
        private const string StartedAtKey = "UnityMcp.Compile.StartedAtTicks";
        private const string PendingAtKey = "UnityMcp.Compile.PendingAtTicks";
        private const string DurationKey = "UnityMcp.Compile.DurationMs";

        private static readonly List<CompileMessage> messages = new();

        static CompilationCache()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            Restore();
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
        }

        /// <summary>
        /// 現在のコンパイル状態。EditorApplication.isCompiling と突き合わせて補正する。
        /// </summary>
        public static CompileState State
        {
            get
            {
                Reconcile();
                return LoadState();
            }
        }

        /// <summary>
        /// 結果がまだ確定していない（前回結果を返してしまう恐れがある）かどうか
        /// </summary>
        public static bool IsStale => State.IsStale();

        /// <summary>
        /// コンパイルが保留中または実行中かどうか
        /// </summary>
        public static bool IsBusy
        {
            get
            {
                var state = State;
                return state == CompileState.Pending || state == CompileState.Compiling;
            }
        }

        /// <summary>
        /// 直近のコンパイル完了時刻。未完了・未観測の場合はnull。
        /// </summary>
        public static DateTime? FinishedAt => LoadTicks(FinishedAtKey);

        /// <summary>
        /// 直近のコンパイルに要した時間（ミリ秒）。未計測の場合は0。
        /// </summary>
        public static int LastDurationMs => SessionState.GetInt(DurationKey, 0);

        /// <summary>
        /// Unityが「スクリプトのコンパイルに失敗している」と認識しているかどうか。
        /// 自前キャッシュとは独立した裏付けとして利用する。
        /// </summary>
        public static bool CompilationFailed => EditorUtility.scriptCompilationFailed;

        /// <summary>
        /// キャッシュされているコンパイルメッセージを返す
        /// </summary>
        public static IReadOnlyList<CompileMessage> GetMessages()
        {
            return messages;
        }

        /// <summary>
        /// スクリプト変更を検知した際に呼び出し、結果を保留状態へ落とす。
        /// これ以降 get_compile_errors は isStale=true を返す。
        /// </summary>
        public static void MarkPending()
        {
            if (LoadState() == CompileState.Compiling)
            {
                return;
            }

            StoreState(CompileState.Pending);
            StoreTicks(PendingAtKey, DateTime.UtcNow);
        }

        private static void OnCompilationStarted(object context)
        {
            messages.Clear();
            SessionState.EraseString(MessagesKey);
            StoreState(CompileState.Compiling);
            StoreTicks(StartedAtKey, DateTime.UtcNow);
        }

        private static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] compilerMessages)
        {
            foreach (var message in compilerMessages)
            {
                messages.Add(CompileMessage.FromCompilerMessage(message));
            }
        }

        private static void OnCompilationFinished(object context)
        {
            Complete();
        }

        /// <summary>
        /// コンパイル完了として状態・結果・所要時間を確定させる
        /// </summary>
        private static void Complete()
        {
            var finishedAt = DateTime.UtcNow;
            StoreState(CompileState.Completed);
            StoreTicks(FinishedAtKey, finishedAt);
            SessionState.SetInt(DurationKey, MeasureDurationMs(finishedAt));
            SessionState.SetString(MessagesKey, JsonConvert.SerializeObject(messages));
        }

        private static int MeasureDurationMs(DateTime finishedAt)
        {
            var startedAt = LoadTicks(StartedAtKey);
            if (startedAt == null)
            {
                return 0;
            }

            return (int)(finishedAt - startedAt.Value).TotalMilliseconds;
        }

        /// <summary>
        /// 保存済み状態とエディタの実状態を突き合わせ、取り残された状態を補正する。
        /// ドメインリロードでイベントを取り逃した場合の Compiling 固着を防ぐ。
        /// </summary>
        private static void Reconcile()
        {
            var stored = LoadState();
            if (EditorApplication.isCompiling)
            {
                StoreIfChanged(stored, CompileState.Compiling);
                return;
            }

            if (stored == CompileState.Compiling)
            {
                Complete();
                return;
            }

            if (stored == CompileState.Pending && IsPendingExpired())
            {
                Complete();
            }
        }

        private static void StoreIfChanged(CompileState stored, CompileState next)
        {
            if (stored == next)
            {
                return;
            }

            StoreState(next);
        }

        private static bool IsPendingExpired()
        {
            var pendingAt = LoadTicks(PendingAtKey);
            if (pendingAt == null)
            {
                return true;
            }

            return (DateTime.UtcNow - pendingAt.Value).TotalSeconds > PendingTimeoutSeconds;
        }

        /// <summary>
        /// ドメインリロード後に SessionState から状態と結果を復元する。
        /// 記録が無い場合、ドメインがロードできている＝直近のコンパイルは成立しているとみなす。
        /// </summary>
        private static void Restore()
        {
            var stored = SessionState.GetString(StateKey, string.Empty);
            if (string.IsNullOrEmpty(stored))
            {
                Complete();
                return;
            }

            RestoreMessages();
        }

        private static void RestoreMessages()
        {
            var json = SessionState.GetString(MessagesKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            var restored = JsonConvert.DeserializeObject<List<CompileMessage>>(json);
            if (restored == null)
            {
                return;
            }

            messages.Clear();
            messages.AddRange(restored);
        }

        private static CompileState LoadState()
        {
            var stored = SessionState.GetString(StateKey, string.Empty);
            if (Enum.TryParse<CompileState>(stored, out var state))
            {
                return state;
            }

            return CompileState.Idle;
        }

        private static void StoreState(CompileState state)
        {
            SessionState.SetString(StateKey, state.ToString());
        }

        private static DateTime? LoadTicks(string key)
        {
            var stored = SessionState.GetString(key, string.Empty);
            if (!long.TryParse(stored, out var ticks))
            {
                return null;
            }

            return new DateTime(ticks, DateTimeKind.Utc);
        }

        private static void StoreTicks(string key, DateTime value)
        {
            SessionState.SetString(key, value.Ticks.ToString());
        }
    }
}
