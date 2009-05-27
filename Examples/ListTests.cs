using NUnit.Framework;
using Examples.Ppt;
using System.Collections.Generic;
using ProtoBuf;
using System.Linq;
using NUnit.Framework.SyntaxHelpers;
using System;

namespace Examples
{
    [ProtoContract]
    class Entity
    {
        [ProtoMember(1)]
        public string Foo { get; set; }
    }

    class MyList : List<Entity> { }

    [TestFixture]
    public class ListTests
    {
        [Test]
        public void TestEmtpyBasicListOfEntity()
        {
            var foos = new List<Entity>();
            var clone = Serializer.DeepClone(foos);
            Assert.IsNull(clone);
        }

        [Test]
        public void TestEmptyMyListOfEntity()
        {
            var foos = new MyList();
            var clone = Serializer.DeepClone(foos);
            Assert.IsNull(clone);
        }

        [Test]
        public void TestNonEmtpyBasicListOfEntity()
        {
            var foos = new List<Entity>
            {
                new Entity { Foo = "abc"},
                new Entity { Foo = "def"},
            };
            var clone = Serializer.DeepClone(foos);
            Assert.IsNotNull(clone);
            Assert.AreNotSame(foos, clone);
            Assert.AreEqual(foos.GetType(), clone.GetType());
            Assert.AreEqual(2, clone.Count);
            Assert.AreEqual(foos[0].Foo, clone[0].Foo);
            Assert.AreEqual(foos[1].Foo, clone[1].Foo);
        }

        [Test]
        public void TestNonEmptyMyListOfEntity()
        {
            var foos = new MyList() 
            {
                new Entity { Foo = "abc"},
                new Entity { Foo = "def"},
            };
            var clone = Serializer.DeepClone(foos);
            Assert.IsNotNull(clone);
            Assert.AreNotSame(foos, clone);
            Assert.AreEqual(foos.GetType(), clone.GetType());
            Assert.AreEqual(2, clone.Count);
            Assert.AreEqual(foos[0].Foo, clone[0].Foo);
            Assert.AreEqual(foos[1].Foo, clone[1].Foo);
        }

        [Test]
        public void TestCompositeDictionary()
        {
            DictionaryTestEntity obj = new DictionaryTestEntity
            {
                Foo = "bar",
                Stuff =
                {
                    {"abc", CompositeType.Create(123)},
                    {"def", CompositeType.Create(DateTime.Today)},
                    {"ghi", CompositeType.Create("hello world")},
                }
            }, clone = Serializer.DeepClone(obj);

            Assert.IsNotNull(clone);
            Assert.AreNotSame(clone, obj);
            Assert.AreEqual("bar", clone.Foo);
            Assert.AreEqual(3, clone.Stuff.Count);
            Assert.AreEqual(123, clone.Stuff["abc"].Value);
            Assert.AreEqual(DateTime.Today, clone.Stuff["def"].Value);
            Assert.AreEqual("hello world", clone.Stuff["ghi"].Value);
        }

        [ProtoContract]
        class DictionaryTestEntity
        {
            public DictionaryTestEntity() {
                Stuff = new CustomBox();
            }
            [ProtoMember(1)]
            public string Foo { get; set; }

            [ProtoMember(2)]
            public CustomBox Stuff { get; private set; }
        }


        class CustomBox : Dictionary<string, CompositeType>
        {
            
        } 

        [ProtoContract]
        [ProtoInclude(1, typeof(CompositeType<int>))]
        [ProtoInclude(2, typeof(CompositeType<DateTime>))]
        [ProtoInclude(3, typeof(CompositeType<string>))]
        abstract class CompositeType
        {
            public static CompositeType<T> Create<T>(T value)
            {
                return new CompositeType<T> { Value = value };
            }

            protected abstract object ValueImpl {get;set;}
            public object Value
            {
                get { return ValueImpl; }
                set { ValueImpl = value; } 
            }
        }
        [ProtoContract]
        class CompositeType<T> : CompositeType
        {
            [ProtoMember(1)]
            public new T Value { get; set; }

            protected override object ValueImpl
            {
                get { return Value; }
                set { Value = (T)value; }
            }
        }

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
