using UnityEditor;
using UnityMcp.Tools.Compile;

namespace UnityMcp
{
    /// <summary>
    /// エディタがMCPリクエストを処理できる状態かを判定するヘルパー。
    /// ビジー中の応答をビジーエラーとして正規化し、クライアントが安全に再送できるようにする。
    /// </summary>
    public static class EditorReadiness
    {
        /// <summary>
        /// ビジー中でも実行を許可するツール。状態の観測と待機に必要なもののみ。
        /// </summary>
        private static readonly string[] AlwaysAllowedTools =
        {
            "check_status",
            "get_compile_errors",
            "compile_and_wait",
            "get_console_logs",
            "get_play_mode_state",
            "get_editmode_test_results",
            "get_playmode_test_results"
        };

        /// <summary>
        /// スクリプトのコンパイル中か
        /// </summary>
        public static bool IsCompiling => EditorApplication.isCompiling;

        /// <summary>
        /// アセットデータベースの更新中（ドメインリロード直後を含む）か
        /// </summary>
        public static bool IsUpdating => EditorApplication.isUpdating;

        /// <summary>
        /// 通常のツール呼び出しを受け付けられる状態か
        /// </summary>
        public static bool IsReady => !IsCompiling && !IsUpdating;

        /// <summary>
        /// ビジー状態の理由。準備完了ならnull。
        /// </summary>
        public static string BusyReason
        {
            get
            {
                if (IsCompiling)
                {
                    return "compiling scripts";
                }

                if (IsUpdating)
                {
                    return "updating the asset database (domain reload)";
                }

                return null;
            }
        }

        /// <summary>
        /// 指定ツールがビジー中でも実行を許可されているか
        /// </summary>
        public static bool IsAllowedWhileBusy(string toolName)
        {
            foreach (var allowed in AlwaysAllowedTools)
            {
                if (allowed == toolName)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// ビジーエラーのdataフィールドへ載せる詳細を組み立てる
        /// </summary>
        public static object BuildBusyDetail()
        {
            return new
            {
                retryable = true,
                isCompiling = IsCompiling,
                isUpdating = IsUpdating,
                compileState = CompilationCache.State.ToWireString(),
                hint = "The editor is busy. This request is safe to resend as-is after a short delay, " +
                       "or call 'compile_and_wait' to block until compilation settles."
            };
        }
    }
}
