using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace UnityMcp.Tests
{
    /// <summary>
    /// コレクション操作のテスト（18件）
    /// </summary>
    public class CollectionTests
    {
        [Test]
        public void List_Add_IncreasesCount()
        {
            var list = new List<int> { 1, 2, 3 };
            list.Add(4);
            Assert.AreEqual(4, list.Count);
        }

        [Test]
        public void List_Remove_DecreasesCount()
        {
            var list = new List<int> { 1, 2, 3 };
            list.Remove(2);
            Assert.AreEqual(2, list.Count);
        }

        [Test]
        public void List_Contains_FindsElement()
        {
            var list = new List<string> { "apple", "banana", "cherry" };
            Assert.IsTrue(list.Contains("banana"));
        }

        [Test]
        public void List_Clear_MakesEmpty()
        {
            var list = new List<int> { 1, 2, 3 };
            list.Clear();
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void List_IndexOf_ReturnsCorrectIndex()
        {
            var list = new List<int> { 10, 20, 30 };
            Assert.AreEqual(1, list.IndexOf(20));
        }

        [Test]
        public void Dictionary_Add_StoresValue()
        {
            var dict = new Dictionary<string, int> { { "a", 1 } };
            Assert.AreEqual(1, dict["a"]);
        }

        [Test]
        public void Dictionary_ContainsKey_ReturnsTrue()
        {
            var dict = new Dictionary<string, int> { { "key", 42 } };
            Assert.IsTrue(dict.ContainsKey("key"));
        }

        [Test]
        public void Dictionary_Count_ReturnsCorrect()
        {
            var dict = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };
            Assert.AreEqual(2, dict.Count);
        }

        [Test]
        public void Dictionary_TryGetValue_ExistingKey_ReturnsTrue()
        {
            var dict = new Dictionary<string, int> { { "x", 99 } };
            Assert.IsTrue(dict.TryGetValue("x", out _));
        }

        [Test]
        public void Array_Length_ReturnsCorrect()
        {
            var arr = new[] { 1, 2, 3, 4, 5 };
            Assert.AreEqual(5, arr.Length);
        }

        [Test]
        public void Array_Sort_SortsAscending()
        {
            var arr = new[] { 3, 1, 2 };
            System.Array.Sort(arr);
            Assert.AreEqual(new[] { 1, 2, 3 }, arr);
        }

        [Test]
        public void Linq_Where_FiltersCorrectly()
        {
            var numbers = new[] { 1, 2, 3, 4, 5 };
            var evens = numbers.Where(n => n % 2 == 0).ToArray();
            Assert.AreEqual(2, evens.Length);
        }

        [Test]
        public void Linq_Sum_CalculatesCorrectly()
        {
            var numbers = new[] { 1, 2, 3, 4, 5 };
            Assert.AreEqual(15, numbers.Sum());
        }

        [Test]
        public void Linq_Any_ReturnsTrueWhenMatched()
        {
            var numbers = new[] { 1, 2, 3 };
            Assert.IsTrue(numbers.Any(n => n > 2));
        }

        [Test]
        public void Linq_All_ReturnsTrueWhenAllMatch()
        {
            var numbers = new[] { 2, 4, 6 };
            Assert.IsTrue(numbers.All(n => n % 2 == 0));
        }

        [Test]
        public void Linq_First_ReturnsFirstElement()
        {
            var numbers = new[] { 10, 20, 30 };
            Assert.AreEqual(10, numbers.First());
        }

        [Test]
        public void Linq_Count_WithPredicate()
        {
            var numbers = new[] { 1, 2, 3, 4, 5 };
            Assert.AreEqual(3, numbers.Count(n => n > 2));
        }

        [Test]
        public void HashSet_Add_PreventsDuplicates()
        {
            var set = new HashSet<int> { 1, 2, 3 };
            set.Add(2);
            Assert.AreEqual(3, set.Count);
        }
    }
}
