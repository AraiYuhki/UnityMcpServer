using System;

namespace UnityMcp
{
    /// <summary>
    /// MCPツールとして自動登録する対象を示す属性。
    /// <see cref="IMcpTool"/> を実装し公開パラメータなしコンストラクタを持つクラスに付与すると、
    /// サーバー起動時に外部アセンブリからでも自動的に登録される。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class McpToolAttribute : Attribute
    {
    }
}
