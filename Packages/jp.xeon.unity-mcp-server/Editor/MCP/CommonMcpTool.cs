using System;
using System.Threading.Tasks;

namespace UnityMcp
{
    /// <summary>
    /// デリゲートをIMcpToolとして扱うための汎用ツールクラス
    /// </summary>
    public class CommonMcpTool : IMcpTool
    {
        public string Name { get; }
        public string Description { get; }
        public string InputSchema { get; }

        private Func<string, Task<object>> func;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name">ツール名</param>
        /// <param name="func">ツールの実行関数</param>
        /// <param name="description">ツールの説明</param>
        /// <param name="inputSchema">ツールの入力スキーマ（JSON Schema文字列）</param>
        public CommonMcpTool(string name, Func<string, Task<object>> func, string description = "", string inputSchema = "{\"type\":\"object\",\"properties\":{}}")
        {
            Name = name;
            Description = description;
            InputSchema = inputSchema;
            this.func = func;
        }
        
        public Task<object> Execute(string args) => func(args);
    }
}