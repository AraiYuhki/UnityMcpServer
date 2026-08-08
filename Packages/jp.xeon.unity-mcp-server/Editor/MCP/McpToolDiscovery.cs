using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityMcp
{
    /// <summary>
    /// <see cref="McpToolAttribute"/> が付与されたツールをプロジェクト全体から収集するクラス。
    /// このパッケージ外のアセンブリで定義されたツールも対象になる。
    /// </summary>
    public static class McpToolDiscovery
    {
        /// <summary>
        /// 属性付きのツールを収集してインスタンス化する。
        /// </summary>
        /// <returns>インスタンス化に成功したツールのリスト</returns>
        public static IReadOnlyList<IMcpTool> CollectTools()
        {
            return CreateTools(TypeCache.GetTypesWithAttribute<McpToolAttribute>());
        }

        /// <summary>
        /// 指定された型からツールを生成する。
        /// 要件を満たさない型は警告を出して読み飛ばし、生成処理自体は継続する。
        /// </summary>
        /// <param name="types">生成対象の型</param>
        /// <returns>インスタンス化に成功したツールのリスト</returns>
        public static IReadOnlyList<IMcpTool> CreateTools(IEnumerable<Type> types)
        {
            var tools = new List<IMcpTool>();
            foreach (var type in types)
            {
                AddTool(tools, type);
            }
            return tools;
        }

        /// <summary>
        /// 型からツールを生成してリストへ追加する。生成できない場合は何もしない。
        /// </summary>
        private static void AddTool(List<IMcpTool> tools, Type type)
        {
            var tool = CreateTool(type);
            if (tool == null)
            {
                return;
            }
            tools.Add(tool);
        }

        /// <summary>
        /// 型がツールとして使えるか検証し、インスタンスを生成する。
        /// </summary>
        /// <returns>生成したツール。要件を満たさない場合はnull</returns>
        private static IMcpTool CreateTool(Type type)
        {
            if (!typeof(IMcpTool).IsAssignableFrom(type))
            {
                Debug.LogWarning($"[MCP] {type.FullName} has [McpTool] but does not implement IMcpTool. Skipped.");
                return null;
            }
            if (type.IsAbstract || type.IsGenericTypeDefinition)
            {
                Debug.LogWarning($"[MCP] {type.FullName} has [McpTool] but is abstract or generic. Skipped.");
                return null;
            }
            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                Debug.LogWarning($"[MCP] {type.FullName} has [McpTool] but has no public parameterless constructor. Skipped.");
                return null;
            }
            return Instantiate(type);
        }

        /// <summary>
        /// 型をインスタンス化する。コンストラクタが例外を投げた場合はnullを返す。
        /// </summary>
        private static IMcpTool Instantiate(Type type)
        {
            try
            {
                return (IMcpTool)Activator.CreateInstance(type);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MCP] Failed to instantiate MCP tool {type.FullName}: {e.Message}");
                return null;
            }
        }
    }
}
