using System.Threading.Tasks;

namespace UnityMcp
{
    /// <summary>
    /// MCPツールのインターフェース
    /// カスタムツールを作成する際はこのインターフェースを実装する
    /// </summary>
    public interface IMcpTool
    {
        /// <summary>ツール名</summary>
        string Name { get; }
        /// <summary>ツールの説明</summary>
        string Description { get; }
        /// <summary>ツールの入力スキーマ（JSON Schema文字列）</summary>
        string InputSchema { get; }
        /// <summary>
        /// ツールを実行する
        /// </summary>
        /// <param name="args">引数（JSON文字列）</param>
        /// <returns>実行結果</returns>
        Task<object> Execute(string args);
    }
}
