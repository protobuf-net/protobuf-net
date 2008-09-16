using NUnit.Framework;
using Examples.Ppt;
using System.Collections.Generic;
using ProtoBuf;
using System.Linq;

namespace Examples
{
    [TestFixture]
    public class ListTests
    {
        [Test]
        public void TestListBytes()
        {
            List<Test3> list = new List<Test3> { new Test3 { C = new Test1 { A= 150} } };
            Program.CheckBytes(list, 0x09, 0x1a, 0x03, 0x08, 0x96, 0x01);
        }
        [Test]
        public void TestListContents()
        {
            List<Test3> list = new List<Test3>
            {
                new Test3 { C = new Test1 { A = 123}},
                new Test3 { C = new Test1 { A = 456}},
                new Test3 { C = new Test1 { A = 789}}
            };
            
            var clone = Serializer.DeepClone(list);
            CheckLists(list, clone);
        }

        class Test3Enumerable : IEnumerable<Test3>
        {
            private readonly List<Test3> items = new List<Test3>();


            public IEnumerator<Test3> GetEnumerator()
            {
                foreach (var item in items) yield return item;
            }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Add(Test3 item) { items.Add(item); }
        }

        [Test]
        public void TestEnumerableBytes()
        {
            Test3Enumerable list = new Test3Enumerable { new Test3 { C = new Test1 { A = 150 } } };
            Program.CheckBytes(list, 0x09, 0x1a, 0x03, 0x08, 0x96, 0x01);
        }
        [Test]
        public void TestEnumerableContents()
        {
            Test3Enumerable items = new Test3Enumerable
            {
                new Test3 { C = new Test1 { A = 123}},
                new Test3 { C = new Test1 { A = 456}},
                new Test3 { C = new Test1 { A = 789}}
            };

            var clone = Serializer.DeepClone(items);
            CheckLists(items, clone);
        }

        [Test]
        public void TestArrayBytes()
        {
            Test3[] list = new Test3[] { new Test3 { C = new Test1 { A = 150 } } };
            Program.CheckBytes(list, 0x09, 0x1a, 0x03, 0x08, 0x96, 0x01);
        }
        [Test]
        public void TestArrayContents()
        {
            Test3[] arr = new Test3[]
            {
                new Test3 { C = new Test1 { A = 123}},
                new Test3 { C = new Test1 { A = 456}},
                new Test3 { C = new Test1 { A = 789}}
            };

            var clone = Serializer.DeepClone(arr);
            CheckLists(arr, clone);
        }

        class Test3Comparer : IEqualityComparer<Test3>
        {

            public bool Equals(Test3 x, Test3 y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x == null || y == null) return false;
                if (ReferenceEquals(x.C, y.C)) return true;
                if (x.C == null || y.C == null) return false;
                return x.C.A == y.C.A;
            }
            public int GetHashCode(Test3 obj)
            {
                throw new System.NotImplementedException();
            }
        }
        static void CheckLists(IEnumerable<Test3> original, IEnumerable<Test3> clone)
        {
            Assert.IsTrue(original.SequenceEqual(clone,new Test3Comparer()));
        }
    }
}
