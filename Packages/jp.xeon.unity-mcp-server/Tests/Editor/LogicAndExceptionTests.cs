using System;
using NUnit.Framework;
using UnityEngine;

namespace UnityMcp.Tests
{
    /// <summary>
    /// 論理演算・型変換・例外処理のテスト（25件: 17成功 / 8失敗）
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

        // --- 以下、意図的に失敗するテスト ---

        [Test]
        public void Fail_Bool_AndWithFalse()
        {
            Assert.IsTrue(true && false, "true && false はfalse");
        }

        [Test]
        public void Fail_Null_NotNull_Wrong()
        {
            string value = null;
            Assert.IsNotNull(value, "nullの値にIsNotNullは失敗する");
        }

        [Test]
        public void Fail_Equality_DifferentTypes()
        {
            Assert.AreEqual(1, 1.1, "intとdoubleの比較で不一致");
        }

        [Test]
        public void Fail_Cast_StringToInt_Throws()
        {
            Assert.AreEqual(123, (object)"123", "文字列とintは異なるオブジェクト");
        }

        [Test]
        public void Fail_Exception_NoThrow()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                int result = 1 + 1;
            }, "例外がスローされないためfail");
        }

        [Test]
        public void Fail_Exception_WrongType()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                int result = 1 / int.Parse("0");
            }, "DivideByZeroExceptionでありArgumentExceptionではない");
        }

        [Test]
        public void Fail_GreaterThan_WrongComparison()
        {
            Assert.Greater(3, 5, "3は5より大きくない");
        }

        [Test]
        public void Fail_LessThan_WrongComparison()
        {
            Assert.Less(10, 5, "10は5より小さくない");
        }
    }
}
