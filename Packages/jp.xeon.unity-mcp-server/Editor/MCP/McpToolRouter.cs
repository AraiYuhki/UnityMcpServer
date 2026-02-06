using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor.TestTools.TestRunner.Api;

namespace UnityMcp
{
    /// <summary>
    /// MCPツールのルーティングを管理するクラス
    /// ツールの登録と実行を担当する
    /// </summary>
    public static class McpToolRouter
    {
        /// <summary>
        /// 登録されたツールの辞書
        /// キー: ツール名、値: IMcpToolインスタンス
        /// </summary>
        private static Dictionary<string, IMcpTool> toolList = new();

        /// <summary>
        /// ツールリストを初期化し、デフォルトのツールを登録する
        /// サーバー起動時に呼び出される
        /// </summary>
        public static void Initialize()
        {
            toolList.Clear();
            var editModeTestTool = new RunTests("run_editmode_tests", "Run Test Runner in EditMode", TestMode.EditMode);
            var playModeTestTool = new RunTests("run_playmode_tests",  "Run Test Runner in PlayMode", TestMode.PlayMode);
            TryRegisterTool("check_status", CheckStatus);
            TryRegisterTool(editModeTestTool);
            TryRegisterTool(playModeTestTool);
        }

        /// <summary>
        /// IMcpToolインスタンスをツールとして登録する
        /// 同名のツールが既に登録されている場合は登録せずfalseを返す
        /// </summary>
        /// <param name="tool">登録するツール</param>
        /// <returns>登録に成功した場合true</returns>
        public static bool TryRegisterTool(IMcpTool tool)
        {
            return toolList.TryAdd(tool.Name, tool);
        }

        /// <summary>
        /// ツールを登録する
        /// 同名のツールが既に登録されている場合は登録せずfalseを返す
        /// </summary>
        /// <param name="toolName">ツール名</param>
        /// <param name="func">ツールの実行関数（引数: JSON文字列、戻り値: 結果オブジェクト）</param>
        /// <returns>登録に成功した場合true</returns>
        public static bool TryRegisterTool(string toolName, Func<string, Task<object>> func)
        {
            var tool = new CommonMcpTool(toolName, func);
            return toolList.TryAdd(toolName, tool);
        }

        /// <summary>
        /// 登録されたツールのリストを取得する
        /// </summary>
        /// <returns>登録済みツールの読み取り専用リスト</returns>
        public static IReadOnlyList<IMcpTool> GetToolList()
        {
            return new List<IMcpTool>(toolList.Values);
        }

        /// <summary>
        /// 指定されたツールを実行する
        /// </summary>
        /// <param name="toolName">実行するツール名</param>
        /// <param name="arguments">ツールに渡す引数（JSON文字列）</param>
        /// <returns>ツールの実行結果</returns>
        /// <exception cref="InvalidOperationException">指定されたツールが見つからない場合</exception>
        public static async Task<object> Execute(string toolName, string arguments)
        {
            if (!toolList.TryGetValue(toolName, out var tool))
            {
                throw new InvalidOperationException($"Unknown tool: {toolName}");
            }
            return await tool.Execute(arguments);
        }

        /// <summary>
        /// サーバーの稼働状態を確認するツール
        /// </summary>
        private static Task<object> CheckStatus(string _)
        {
            return Task.FromResult<object>(true);
        }
    }
}
