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
        /// キー: ツール名、値: 引数を受け取り結果を返す非同期関数
        /// </summary>
        private static Dictionary<string, Func<string, Task<object>>> toolList = new();

        /// <summary>
        /// ツールリストを初期化し、デフォルトのツールを登録する
        /// サーバー起動時に呼び出される
        /// </summary>
        public static void Initialize()
        {
            toolList.Clear();
            TryRegisterTool("check_status", CheckStatus);
            TryRegisterTool("run_editmode_tests", _ => RunTests.Execute(TestMode.EditMode));
            TryRegisterTool("run_playmode_tests", _ => RunTests.Execute(TestMode.PlayMode));
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
            return toolList.TryAdd(toolName, func);
        }

        /// <summary>
        /// リクエストに応じたツールを実行する
        /// </summary>
        /// <param name="request">MCPリクエスト</param>
        /// <returns>ツールの実行結果</returns>
        /// <exception cref="InvalidOperationException">指定されたツールが見つからない場合</exception>
        public static async Task<object> Execute(McpRequest request)
        {
            if (!toolList.TryGetValue(request.tool, out var func))
                throw new InvalidOperationException($"Unknown tool: {request.tool}");
            return await func(request.arguments);
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
