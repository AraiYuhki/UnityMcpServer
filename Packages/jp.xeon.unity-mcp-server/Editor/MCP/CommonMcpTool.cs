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

        private Func<string, Task<object>> func;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name">ツール名</param>
        /// <param name="func">ツールの実行関数</param>
        /// <param name="description">ツールの説明</param>
        public CommonMcpTool(string name, Func<string, Task<object>> func, string description = "")
        {
            Name = name;
            Description = description;
            this.func = func;
        }
        
        public Task<object> Execute(string args) => func(args);
    }
}