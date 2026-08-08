using NUnit.Framework;
using UnityEngine;
using UnityMcp.Tools.Compile;
using UnityMcp.Tools.Console;

namespace UnityMcp.Tests
{
    /// <summary>
    /// コンパイル状態の stale 判定とコンソールログ分類のテスト
    /// </summary>
    public class CompileStateTests
    {
        [Test]
        public void ToWireString_Completed_ReturnsCompleted()
        {
            Assert.AreEqual("completed", CompileState.Completed.ToWireString());
        }

        [Test]
        public void ToWireString_Pending_ReturnsPending()
        {
            Assert.AreEqual("pending", CompileState.Pending.ToWireString());
        }

        [Test]
        public void ToWireString_Compiling_ReturnsCompiling()
        {
            Assert.AreEqual("compiling", CompileState.Compiling.ToWireString());
        }

        [Test]
        public void ToWireString_Idle_ReturnsIdle()
        {
            Assert.AreEqual("idle", CompileState.Idle.ToWireString());
        }

        [Test]
        public void IsStale_Completed_ReturnsFalse()
        {
            Assert.IsFalse(CompileState.Completed.IsStale());
        }

        [Test]
        public void IsStale_Pending_ReturnsTrue()
        {
            Assert.IsTrue(CompileState.Pending.IsStale());
        }

        [Test]
        public void IsStale_Compiling_ReturnsTrue()
        {
            Assert.IsTrue(CompileState.Compiling.IsStale());
        }

        [Test]
        public void IsStale_Idle_ReturnsTrue()
        {
            Assert.IsTrue(CompileState.Idle.IsStale());
        }

        [Test]
        public void LogEntry_Error_IsErrorButNotException()
        {
            var entry = LogEntry.Create(1, "boom", string.Empty, LogType.Error);
            Assert.IsTrue(entry.IsError());
            Assert.IsFalse(entry.IsException());
        }

        [Test]
        public void LogEntry_Exception_IsBothErrorAndException()
        {
            var entry = LogEntry.Create(2, "boom", string.Empty, LogType.Exception);
            Assert.IsTrue(entry.IsError());
            Assert.IsTrue(entry.IsException());
        }

        [Test]
        public void LogEntry_Assert_IsError()
        {
            var entry = LogEntry.Create(3, "assert", string.Empty, LogType.Assert);
            Assert.IsTrue(entry.IsError());
        }

        [Test]
        public void LogEntry_Warning_IsNotError()
        {
            var entry = LogEntry.Create(4, "careful", string.Empty, LogType.Warning);
            Assert.IsFalse(entry.IsError());
        }

        [Test]
        public void LogEntry_Log_IsNotError()
        {
            var entry = LogEntry.Create(5, "hello", string.Empty, LogType.Log);
            Assert.IsFalse(entry.IsError());
        }

        [Test]
        public void LogEntry_Create_KeepsGivenId()
        {
            var entry = LogEntry.Create(42, "hello", string.Empty, LogType.Log);
            Assert.AreEqual(42, entry.Id);
        }
    }
}
