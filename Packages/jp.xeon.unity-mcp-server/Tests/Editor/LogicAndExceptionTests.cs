using System;
using NUnit.Framework;
using UnityEngine;

namespace UnityMcp.Tests
{
    /// <summary>
    /// 論理演算・型変換・例外処理のテスト（17件）
    /// </summary>
    public class LogicAndExceptionTests
    {
        [Test]
        public void Bool_TrueAndTrue_ReturnsTrue()
        {
            Assert.IsTrue(true && true);
        }

        [Test]
        public void Bool_TrueOrFalse_ReturnsTrue()
        {
            Assert.IsTrue(true || false);
        }

        [Test]
        public void Bool_NotTrue_ReturnsFalse()
        {
            Assert.IsFalse(!true);
        }

        [Test]
        public void Null_IsNull_ReturnsTrue()
        {
            string value = null;
            Assert.IsNull(value);
        }

        [Test]
        public void NotNull_ReturnsTrue()
        {
            Assert.IsNotNull("hello");
        }

        [Test]
        public void Equality_SameValue_ReturnsTrue()
        {
            Assert.AreEqual(42, 42);
        }

        [Test]
        public void Inequality_DifferentValues_ReturnsTrue()
        {
            Assert.AreNotEqual(1, 2);
        }

        [Test]
        public void Cast_IntToDouble_Succeeds()
        {
            int intValue = 10;
            double doubleValue = intValue;
            Assert.AreEqual(10.0, doubleValue, 0.001);
        }

        [Test]
        public void Cast_DoubleToInt_Truncates()
        {
            double doubleValue = 3.9;
            int intValue = (int)doubleValue;
            Assert.AreEqual(3, intValue);
        }

        [Test]
        public void Ternary_TrueCondition_ReturnsFirst()
        {
            var result = true ? "yes" : "no";
            Assert.AreEqual("yes", result);
        }

        [Test]
        public void Ternary_FalseCondition_ReturnsSecond()
        {
            var result = false ? "yes" : "no";
            Assert.AreEqual("no", result);
        }

        [Test]
        public void NullCoalescing_NullValue_ReturnsFallback()
        {
            string value = null;
            Assert.AreEqual("default", value ?? "default");
        }

        [Test]
        public void NullCoalescing_NonNullValue_ReturnsOriginal()
        {
            string value = "actual";
            Assert.AreEqual("actual", value ?? "default");
        }

        [Test]
        public void Exception_DivideByZero_ThrowsException()
        {
            Assert.Throws<DivideByZeroException>(() =>
            {
                int result = 1 / int.Parse("0");
            });
        }

        [Test]
        public void Exception_NullReference_ThrowsException()
        {
            string value = null;
            Assert.Throws<NullReferenceException>(() =>
            {
                int length = value.Length;
            });
        }

        [Test]
        public void Exception_IndexOutOfRange_ThrowsException()
        {
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var arr = new int[3];
                int v = arr[10];
            });
        }

        [Test]
        public void Vector3_Magnitude_Calculated()
        {
            var v = new Vector3(3, 4, 0);
            Assert.AreEqual(5.0f, v.magnitude, 0.001f);
        }
    }
}
