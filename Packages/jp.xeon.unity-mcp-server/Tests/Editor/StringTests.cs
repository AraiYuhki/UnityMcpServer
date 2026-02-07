using NUnit.Framework;

namespace UnityMcp.Tests
{
    /// <summary>
    /// 文字列操作のテスト（25件: 17成功 / 8失敗）
    /// </summary>
    public class StringTests
    {
        [Test]
        public void Concat_TwoStrings_ReturnsCombined()
        {
            Assert.AreEqual("HelloWorld", "Hello" + "World");
        }

        [Test]
        public void Length_EmptyString_ReturnsZero()
        {
            Assert.AreEqual(0, "".Length);
        }

        [Test]
        public void Length_Hello_Returns5()
        {
            Assert.AreEqual(5, "Hello".Length);
        }

        [Test]
        public void ToUpper_ReturnsUpperCase()
        {
            Assert.AreEqual("HELLO", "hello".ToUpper());
        }

        [Test]
        public void ToLower_ReturnsLowerCase()
        {
            Assert.AreEqual("hello", "HELLO".ToLower());
        }

        [Test]
        public void Trim_RemovesWhitespace()
        {
            Assert.AreEqual("hello", "  hello  ".Trim());
        }

        [Test]
        public void Contains_SubstringFound_ReturnsTrue()
        {
            Assert.IsTrue("Hello World".Contains("World"));
        }

        [Test]
        public void Contains_SubstringNotFound_ReturnsFalse()
        {
            Assert.IsFalse("Hello World".Contains("xyz"));
        }

        [Test]
        public void StartsWith_MatchingPrefix_ReturnsTrue()
        {
            Assert.IsTrue("UnityMcp".StartsWith("Unity"));
        }

        [Test]
        public void EndsWith_MatchingSuffix_ReturnsTrue()
        {
            Assert.IsTrue("UnityMcp".EndsWith("Mcp"));
        }

        [Test]
        public void Replace_SubstitutesCorrectly()
        {
            Assert.AreEqual("Hello Unity", "Hello World".Replace("World", "Unity"));
        }

        [Test]
        public void Substring_ExtractsCorrectly()
        {
            Assert.AreEqual("World", "Hello World".Substring(6));
        }

        [Test]
        public void IndexOf_FindsCharacter()
        {
            Assert.AreEqual(5, "Hello World".IndexOf(' '));
        }

        [Test]
        public void Split_SplitsCorrectly()
        {
            var parts = "a,b,c".Split(',');
            Assert.AreEqual(3, parts.Length);
        }

        [Test]
        public void IsNullOrEmpty_EmptyString_ReturnsTrue()
        {
            Assert.IsTrue(string.IsNullOrEmpty(""));
        }

        [Test]
        public void IsNullOrEmpty_Null_ReturnsTrue()
        {
            Assert.IsTrue(string.IsNullOrEmpty(null));
        }

        [Test]
        public void Format_InterpolatesCorrectly()
        {
            Assert.AreEqual("Count: 5", string.Format("Count: {0}", 5));
        }

        // --- 以下、意図的に失敗するテスト ---

        [Test]
        public void Fail_Concat_WrongExpectation()
        {
            Assert.AreEqual("Hello World", "Hello" + "World", "スペースがないため不一致");
        }

        [Test]
        public void Fail_ToUpper_CaseSensitive()
        {
            Assert.AreEqual("hello", "hello".ToUpper(), "ToUpperはHELLOを返す");
        }

        [Test]
        public void Fail_Length_WrongCount()
        {
            Assert.AreEqual(6, "Hello".Length, "Helloは5文字であり6ではない");
        }

        [Test]
        public void Fail_Contains_CaseSensitive()
        {
            Assert.IsTrue("Hello".Contains("hello"), "Containsは大文字小文字を区別する");
        }

        [Test]
        public void Fail_Replace_OriginalUnchanged()
        {
            var original = "Hello";
            original.Replace("Hello", "World");
            Assert.AreEqual("World", original, "Replaceは新しい文字列を返し元は変更しない");
        }

        [Test]
        public void Fail_Trim_DoesNotRemoveMiddle()
        {
            Assert.AreEqual("HelloWorld", "Hello World".Trim(), "Trimは中間のスペースを除去しない");
        }

        [Test]
        public void Fail_IndexOf_NotFound_ReturnsWrong()
        {
            Assert.AreEqual(0, "Hello".IndexOf('z'), "見つからない場合は-1を返す");
        }

        [Test]
        public void Fail_Substring_OutOfRange()
        {
            Assert.AreEqual("Hello", "Hi".Substring(0, 5), "範囲外アクセスで例外が発生する");
        }
    }
}
