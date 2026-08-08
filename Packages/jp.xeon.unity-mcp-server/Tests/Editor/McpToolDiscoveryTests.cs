using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace UnityMcp.Tests
{
    /// <summary>
    /// [McpTool]属性によるツール自動収集のテスト。
    /// 実サーバーへ紛れ込まないよう、テスト用の型には属性を付けず CreateTools に直接渡す。
    /// </summary>
    public class McpToolDiscoveryTests
    {
        [Test]
        public void CreateTools_ValidType_ReturnsInstance()
        {
            var tools = McpToolDiscovery.CreateTools(new[] { typeof(ValidTool) });
            Assert.AreEqual(1, tools.Count);
            Assert.AreEqual("valid_tool", tools[0].Name);
        }

        [Test]
        public void CreateTools_NonToolType_IsSkipped()
        {
            var tools = McpToolDiscovery.CreateTools(new[] { typeof(NotATool) });
            Assert.AreEqual(0, tools.Count);
        }

        [Test]
        public void CreateTools_AbstractType_IsSkipped()
        {
            var tools = McpToolDiscovery.CreateTools(new[] { typeof(AbstractTool) });
            Assert.AreEqual(0, tools.Count);
        }

        [Test]
        public void CreateTools_WithoutParameterlessConstructor_IsSkipped()
        {
            var tools = McpToolDiscovery.CreateTools(new[] { typeof(NoDefaultConstructorTool) });
            Assert.AreEqual(0, tools.Count);
        }

        [Test]
        public void CreateTools_ThrowingConstructor_IsSkipped()
        {
            var tools = McpToolDiscovery.CreateTools(new[] { typeof(ThrowingTool) });
            Assert.AreEqual(0, tools.Count);
        }

        [Test]
        public void CreateTools_InvalidTypeDoesNotStopCollection()
        {
            var types = new List<Type> { typeof(NotATool), typeof(ValidTool) };
            var tools = McpToolDiscovery.CreateTools(types);
            Assert.AreEqual(1, tools.Count);
            Assert.AreEqual("valid_tool", tools[0].Name);
        }

        [Test]
        public void CreateTools_EmptyInput_ReturnsEmptyList()
        {
            var tools = McpToolDiscovery.CreateTools(Array.Empty<Type>());
            Assert.AreEqual(0, tools.Count);
        }

        [Test]
        public void CollectTools_ReturnsUsableTools()
        {
            var tools = McpToolDiscovery.CollectTools();
            Assert.IsNotNull(tools);
            Assert.IsTrue(tools.All(tool => !string.IsNullOrEmpty(tool.Name)));
        }

        [Test]
        public void Initialize_RegistersToolsWithUniqueNames()
        {
            McpToolRouter.Initialize();
            var names = McpToolRouter.GetToolList().Select(tool => tool.Name).ToList();
            Assert.AreEqual(names.Count, names.Distinct().Count());
        }

        [Test]
        public void TryRegisterTool_DuplicateName_ReturnsFalse()
        {
            McpToolRouter.Initialize();
            Assert.IsTrue(McpToolRouter.TryRegisterTool(new ValidTool()));
            Assert.IsFalse(McpToolRouter.TryRegisterTool(new ValidTool()));
            McpToolRouter.Initialize();
        }

        private class ValidTool : IMcpTool
        {
            public string Name => "valid_tool";
            public string Description => "A tool used only by tests.";
            public string InputSchema => "{\"type\":\"object\",\"properties\":{},\"required\":[]}";

            public Task<object> Execute(string args)
            {
                return Task.FromResult<object>(new { ok = true });
            }
        }

        private abstract class AbstractTool : IMcpTool
        {
            public string Name => "abstract_tool";
            public string Description => string.Empty;
            public string InputSchema => "{}";

            public abstract Task<object> Execute(string args);
        }

        private class NoDefaultConstructorTool : IMcpTool
        {
            public NoDefaultConstructorTool(string name)
            {
                Name = name;
            }

            public string Name { get; }
            public string Description => string.Empty;
            public string InputSchema => "{}";

            public Task<object> Execute(string args)
            {
                return Task.FromResult<object>(null);
            }
        }

        private class ThrowingTool : IMcpTool
        {
            public ThrowingTool()
            {
                throw new InvalidOperationException("boom");
            }

            public string Name => "throwing_tool";
            public string Description => string.Empty;
            public string InputSchema => "{}";

            public Task<object> Execute(string args)
            {
                return Task.FromResult<object>(null);
            }
        }

        private class NotATool
        {
        }
    }
}
