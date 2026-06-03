using NUnit.Framework;

namespace UnityMcp.Tests
{
    /// <summary>
    /// 算術演算のテスト（18件）
    /// </summary>
    public class ArithmeticTests
    {
        [Test]
        public void Add_1Plus1_Returns2()
        {
            Assert.AreEqual(2, 1 + 1);
        }

        [Test]
        public void Add_NegativeNumbers_ReturnsCorrectSum()
        {
            Assert.AreEqual(-3, -1 + -2);
        }

        [Test]
        public void Add_ZeroPlusZero_ReturnsZero()
        {
            Assert.AreEqual(0, 0 + 0);
        }

        [Test]
        public void Add_LargeNumbers_ReturnsCorrectSum()
        {
            Assert.AreEqual(2000000, 1000000 + 1000000);
        }

        [Test]
        public void Subtract_5Minus3_Returns2()
        {
            Assert.AreEqual(2, 5 - 3);
        }

        [Test]
        public void Subtract_3Minus5_ReturnsNegative2()
        {
            Assert.AreEqual(-2, 3 - 5);
        }

        [Test]
        public void Multiply_3Times4_Returns12()
        {
            Assert.AreEqual(12, 3 * 4);
        }

        [Test]
        public void Multiply_ByZero_ReturnsZero()
        {
            Assert.AreEqual(0, 999 * 0);
        }

        [Test]
        public void Multiply_NegativeByNegative_ReturnsPositive()
        {
            Assert.AreEqual(6, -2 * -3);
        }

        [Test]
        public void Divide_10By2_Returns5()
        {
            Assert.AreEqual(5, 10 / 2);
        }

        [Test]
        public void Divide_IntegerDivision_Truncates()
        {
            Assert.AreEqual(3, 7 / 2);
        }

        [Test]
        public void Modulo_7Mod3_Returns1()
        {
            Assert.AreEqual(1, 7 % 3);
        }

        [Test]
        public void Modulo_10Mod5_ReturnsZero()
        {
            Assert.AreEqual(0, 10 % 5);
        }

        [Test]
        public void FloatAdd_ReturnsApproximate()
        {
            Assert.AreEqual(0.3f, 0.1f + 0.2f, 0.01f);
        }

        [Test]
        public void DoubleMultiply_ReturnsCorrect()
        {
            Assert.AreEqual(6.28, 3.14 * 2.0, 0.001);
        }

        [Test]
        public void Power_2Squared_Returns4()
        {
            Assert.AreEqual(4.0, System.Math.Pow(2, 2), 0.001);
        }

        [Test]
        public void Sqrt_Of9_Returns3()
        {
            Assert.AreEqual(3.0, System.Math.Sqrt(9), 0.001);
        }

        [Test]
        public void Abs_OfNegative_ReturnsPositive()
        {
            Assert.AreEqual(42, System.Math.Abs(-42));
        }
    }
}
