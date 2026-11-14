using System;
using NUnit.Framework;

namespace UnityMcp.Tests
{
    /// <summary>
    /// ランダムポート選択のテスト
    /// </summary>
    public class McpPortSelectorTests
    {
        [Test]
        public void TryPickPort_AllAvailable_ReturnsPortInRange()
        {
            var result = McpPortSelector.TryPickPort(new Random(0), _ => true, out var port);

            Assert.IsTrue(result);
            Assert.GreaterOrEqual(port, McpPortSelector.MinPort);
            Assert.LessOrEqual(port, McpPortSelector.MaxPort);
        }

        [Test]
        public void TryPickPort_NoneAvailable_ReturnsFalse()
        {
            var result = McpPortSelector.TryPickPort(new Random(0), _ => false, out var port);

            Assert.IsFalse(result);
            Assert.AreEqual(0, port);
        }

        [Test]
        public void TryPickPort_SkipsUnavailableCandidates()
        {
            var first = new Random(1).Next(McpPortSelector.MinPort, McpPortSelector.MaxPort + 1);

            var result = McpPortSelector.TryPickPort(new Random(1), candidate => candidate != first, out var port);

            Assert.IsTrue(result);
            Assert.AreNotEqual(first, port);
        }

        [Test]
        public void TryPickAvailablePort_ReturnsBindablePortOtherThanDefault()
        {
            var result = McpPortSelector.TryPickAvailablePort(out var port);

            Assert.IsTrue(result);
            Assert.AreNotEqual(7000, port);
        }
    }
}
