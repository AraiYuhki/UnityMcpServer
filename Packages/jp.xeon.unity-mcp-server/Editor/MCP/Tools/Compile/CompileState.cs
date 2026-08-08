namespace UnityMcp.Tools.Compile
{
    /// <summary>
    /// コンパイルの進行状態。
    /// Pending / Compiling の間に取得したコンパイル結果は前回のものであり信用できない（stale）。
    /// </summary>
    public enum CompileState
    {
        /// <summary>一度もコンパイルを観測していない</summary>
        Idle,

        /// <summary>スクリプト変更を検知したが、まだコンパイルが始まっていない</summary>
        Pending,

        /// <summary>コンパイル実行中</summary>
        Compiling,

        /// <summary>コンパイルが完了し、結果が確定している</summary>
        Completed
    }

    /// <summary>
    /// CompileState をMCPの戻り値用文字列へ変換するヘルパー
    /// </summary>
    public static class CompileStateExtensions
    {
        /// <summary>
        /// クライアントへ返す小文字表記の状態名を返す
        /// </summary>
        public static string ToWireString(this CompileState state)
        {
            switch (state)
            {
                case CompileState.Pending:
                    return "pending";
                case CompileState.Compiling:
                    return "compiling";
                case CompileState.Completed:
                    return "completed";
                default:
                    return "idle";
            }
        }

        /// <summary>
        /// この状態で得られるコンパイル結果が確定済みでない（stale）かどうか
        /// </summary>
        public static bool IsStale(this CompileState state)
        {
            return state != CompileState.Completed;
        }
    }
}
