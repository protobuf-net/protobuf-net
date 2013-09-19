using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Examples
{
    [TestFixture]
    public class ImmutableCollections
    {
        #region Dictionary
        [Test]
        public void Dictionary_ImmutableConcreteProperties()
        {
            TestDictionaryImpl<ImmutableConcreteProperties>();
        }

        [Test]
        public void Dictionary_ImmutableConcreteFields()
        {
            TestDictionaryImpl<ImmutableConcreteFields>();
        }
        [Test]
        public void Dictionary_ImmutableInterfaceProperties()
        {
            TestDictionaryImpl<ImmutableInterfaceProperties>();
        }

        [Test]
        public void Dictionary_ImmutableInterfaceFields()
        {
            TestDictionaryImpl<ImmutableInterfaceFields>();
        }
        #endregion
        #region List
        [Test]
        public void List_ImmutableConcreteProperties()
        {
            TestListImpl<ImmutableConcreteProperties>();
        }

        [Test]
        public void List_ImmutableConcreteFields()
        {
            TestListImpl<ImmutableConcreteFields>();
        }
        [Test]
        public void List_ImmutableInterfaceProperties()
        {
            TestListImpl<ImmutableInterfaceProperties>();
        }

        [Test]
        public void List_ImmutableInterfaceFields()
        {
            TestListImpl<ImmutableInterfaceFields>();
        }
        #endregion

        //#region Array
        //[Test]
        //public void Array_ImmutableConcreteProperties()
        //{
        //    TestArrayImpl<ImmutableConcreteProperties>();
        //}

        //[Test]
        //public void Array_ImmutableConcreteFields()
        //{
        //    TestArrayImpl<ImmutableConcreteFields>();
        //}
        //#endregion

        #region HashSet
        [Test]
        public void HashSet_ImmutableConcreteProperties()
        {
            TestHashSetImpl<ImmutableConcreteProperties>();
        }

        [Test]
        public void HashSet_ImmutableConcreteFields()
        {
            TestHashSetImpl<ImmutableConcreteFields>();
        }
        [Test]
        public void HashSet_ImmutableInterfaceProperties()
        {
            TestHashSetImpl<ImmutableInterfaceProperties>();
        }

        [Test]
        public void HashSet_ImmutableInterfaceFields()
        {
            TestHashSetImpl<ImmutableInterfaceFields>();
        }


        #endregion

        #region SortedSet
        [Test]
        public void SortedSet_ImmutableConcreteProperties()
        {
            TestSortedSetImpl<ImmutableConcreteProperties>();
        }



        [Test]
        public void SortedSet_ImmutableConcreteFields()
        {
            TestSortedSetImpl<ImmutableConcreteFields>();
        }

        #endregion

        #region SortedDictionary
        [Test]
        public void SortedDictionary_ImmutableConcreteProperties()
        {
            TestSortedDictionaryImpl<ImmutableConcreteProperties>();
        }

        [Test]
        public void SortedDictionary_ImmutableConcreteFields()
        {
            TestSortedDictionaryImpl<ImmutableConcreteFields>();
        }
        #endregion

        private static void TestDictionaryImpl<T>([CallerMemberName] string name = null)
            where T : class, IImmutableCollectionWrapper, new()
        {
            var model = TypeModel.Create();
            model.AutoCompile = false;

            TestDictionaryImpl<T>(model, "Runtime");

            var external = model.Compile(name, name + ".dll");
            PEVerify.AssertValid(name + ".dll");
            TestDictionaryImpl<T>(external, "External");

            model.CompileInPlace();
            TestDictionaryImpl<T>(model, "CompileInPlace");
            TestDictionaryImpl<T>(model.Compile(), "Compile");
        }

        private static void TestDictionaryImpl<T>(TypeModel model, string caption)
        where T : class, IImmutableCollectionWrapper, new()
        {
            var values = new Dictionary<int, string> { { 1, "a" }, { 2, "b" }, { 3, "c" }, { 4, "d" } };
            var obj = new T { Dictionary = ImmutableDictionary.Create<int,string>().AddRange(values) };

            using (var ms = new MemoryStream())
            {
                try
                {
                    model.Serialize(ms, obj);
                }
                catch (Exception ex)
                {
                    throw new ProtoException(caption + ":serialize", ex);
                }
                ms.Position = 0;
                T clone;
                try
                {
                    clone = (T)model.Deserialize(ms, null, typeof(T));
                }
                catch (Exception ex)
                {
                    throw new ProtoException(caption + ":deserialize", ex);
                }
                Assert.AreEqual(4, clone.Dictionary.Count, caption);
                Assert.AreEqual("a", clone.Dictionary[1], caption);
                Assert.AreEqual("b", clone.Dictionary[2], caption);
                Assert.AreEqual("c", clone.Dictionary[3], caption);
                Assert.AreEqual("d", clone.Dictionary[4], caption);
            }
        }

        private static void TestListImpl<T>([CallerMemberName] string name = null)
            where T : class, IImmutableCollectionWrapper, new()
        {
            var model = TypeModel.Create();
            model.AutoCompile = false;
            TestListImpl<T>(model, "Runtime");

            var external = model.Compile(name, name + ".dll");
            PEVerify.AssertValid(name + ".dll");
            TestListImpl<T>(external, "External");

            model.CompileInPlace();
            TestListImpl<T>(model, "CompileInPlace");
            TestListImpl<T>(model.Compile(), "Compile");
        }
        private static void TestListImpl<T>(TypeModel model, string caption)
            where T : class, IImmutableCollectionWrapper, new()
        {
            int[] values = { 1, 2, 3, 4 };
            var obj = new T { List = ImmutableList.Create<int>(values) };

            using (var ms = new MemoryStream())
            {
                try
                {
                    model.Serialize(ms, obj);
                } catch(Exception ex)
                {
                    throw new ProtoException(caption + ":serialize", ex);
                }
                ms.Position = 0;
                T clone;
                try
                {
                    clone = (T)model.Deserialize(ms, null, typeof(T));
                } catch(Exception ex)
                {
                    throw new ProtoException(caption + ":deserialize", ex);
                }
                AssertSequence(new[] { 1, 2, 3, 4 }, clone.List, caption);
                ms.Position = 0;
                try
                {
                    model.Deserialize(ms, clone, null); // this is append!
                } catch(Exception ex)
                {
                    throw new ProtoException(caption + ":serialize", ex);
                }
                AssertSequence(new[] { 1, 2, 3, 4, 1, 2, 3, 4 }, clone.List, caption);
            }
        }

        static void AssertSequence<T>(IEnumerable<T> expected, IEnumerable<T> actual, string caption)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, caption);
                return;
            }
            if (expected != null) Assert.IsNotNull(actual, caption);

            Assert.AreNotSame(expected, actual, caption);

            var expArr = expected.ToArray();
            var actArr = actual.ToArray();
            Assert.AreEqual(expArr.Length, actArr.Length, caption + ":length");
            for(int i = 0 ; i < actArr.Length ; i++)
            {
                Assert.AreEqual(expArr[i], actArr[i], caption + ":" + i);
            }
        }

        //private static void TestArrayImpl<T>([CallerMemberName] string name = null)
        //    where T : class, IImmutableCollectionWrapper, new()
        //{
        //    var model = TypeModel.Create();
        //    model.AutoCompile = false;
        //    TestArrayImpl<T>(model, "Runtime");

        //    var external = model.Compile(name, name + ".dll");
        //    PEVerify.AssertValid(name + ".dll");
        //    TestArrayImpl<T>(external, "External");

        //    model.CompileInPlace();
        //    TestArrayImpl<T>(model, "CompileInPlace");
        //    TestArrayImpl<T>(model.Compile(), "Compile");
        //}
        //private static void TestArrayImpl<T>(TypeModel model, string caption)
        //    where T : class, IImmutableCollectionWrapper, new()
        //{
        //    int[] values = { 1, 2, 3, 4 };
        //    var obj = new T { Array = ImmutableArray.Create<int>(values) };

        //    using (var ms = new MemoryStream())
        //    {
        //        try
        //        {
        //            model.Serialize(ms, obj);
        //        } catch(Exception ex)
        //        {
        //            throw new ProtoException(caption + ":serialize", ex);
        //        }
        //        ms.Position = 0;
        //        T clone;
        //        try
        //        {
        //            clone = (T)model.Deserialize(ms, null, typeof(T));
        //        } catch(Exception ex)
        //        {
        //            throw new ProtoException(caption + ":deserialize", ex);
        //        }
        //        AssertSequence(new[] { 1, 2, 3, 4 }, clone.Array, caption);
        //        ms.Position = 0;
        //        try
        //        {
        //            model.Deserialize(ms, clone, null); // this is append!
        //        } catch(Exception ex)
        //        {
        //            throw new ProtoException(caption + ":deserialize", ex);
        //        }
        //        AssertSequence(new[] { 1, 2, 3, 4, 1, 2, 3, 4 }, clone.Array, caption);
        //    }
        //}


        private static void TestHashSetImpl<T>([CallerMemberName] string name = null)
            where T : class, IImmutableCollectionWrapper, new()
        {
            var model = TypeModel.Create();
            model.AutoCompile = false;
            TestHashSetImpl<T>(model, "Runtime");

            var external = model.Compile(name, name + ".dll");
            PEVerify.AssertValid(name + ".dll");
            TestHashSetImpl<T>(external, "External");

            model.CompileInPlace();
            TestHashSetImpl<T>(model, "CompileInPlace");
            TestHashSetImpl<T>(model.Compile(), "Compile");
        }

        private static void TestHashSetImpl<T>(TypeModel model, string caption)
            where T : class, IImmutableCollectionWrapper, new()
        {
            var obj = new T { HashSet = ImmutableHashSet.Create(1, 3, 2) };
            using (var ms = new MemoryStream())
            {
                try
                {
                    model.Serialize(ms, obj);
                }
                catch (Exception ex)
                {
                    throw new ProtoException(caption + ":serialize", ex);
                }
                ms.Position = 0;
                T clone;
                try
                {
                    clone = (T)model.Deserialize(ms, null, typeof(T));
                }
                catch (Exception ex)
                {
                    throw new ProtoException(caption + ":deserialize", ex);
                }
                Assert.AreEqual(3, clone.HashSet.Count, caption);
                Assert.IsTrue(clone.HashSet.Contains(1), caption);
                Assert.IsTrue(clone.HashSet.Contains(2), caption);
                Assert.IsTrue(clone.HashSet.Contains(3), caption);
            }
        }
        private static void TestSortedSetImpl<T>([CallerMemberName] string name = null)
    where T : class, IImmutableCollectionWrapper, new()
        {
            var model = TypeModel.Create();
            model.AutoCompile = false;
            TestSortedSetImpl<T>(model, "Runtime");

            var external = model.Compile(name, name + ".dll");
            PEVerify.AssertValid(name + ".dll");
            TestSortedSetImpl<T>(external, "External");

            model.CompileInPlace();
            TestSortedSetImpl<T>(model, "CompileInPlace");
            TestSortedSetImpl<T>(model.Compile(), "Compile");
        }

        private static void TestSortedSetImpl<T>(TypeModel model, string caption)
            where T : class, IImmutableCollectionWrapper, new()
        {
            var obj = new T { SortedSet = ImmutableSortedSet.Create(1, 3, 2) };
            using (var ms = new MemoryStream())
            {
                try
                {
                    model.Serialize(ms, obj);
                }
                catch (Exception ex)
                {
                    throw new ProtoException(caption + ":serialize", ex);
                }
                ms.Position = 0;
                T clone;
                try
                {
                    clone = (T)model.Deserialize(ms, null, typeof(T));
                }
                catch (Exception ex)
                {
                    throw new ProtoException(caption + ":deserialize", ex);
                }
                AssertSequence(new[] { 1, 2, 3 }, clone.SortedSet, caption);
            }
        }
        private static void TestSortedDictionaryImpl<T>([CallerMemberName] string name = null)
    where T : class, IImmutableCollectionWrapper, new()
        {
            var model = TypeModel.Create();
            model.AutoCompile = false;
            TestSortedDictionaryImpl<T>(model, "Runtime");

            var external = model.Compile(name, name + ".dll");
            PEVerify.AssertValid(name + ".dll");
            TestSortedDictionaryImpl<T>(external, "External");

            model.CompileInPlace();
            TestSortedDictionaryImpl<T>(model, "CompileInPlace");
            TestSortedDictionaryImpl<T>(model.Compile(), "Compile");
        }

        private static void TestSortedDictionaryImpl<T>(TypeModel model, string caption)
            where T : class, IImmutableCollectionWrapper, new()
        {
            var dict = new Dictionary<int,string> {
                {1,"a"},
                {3,"c"},
                {2,"b"}
            };
            var obj = new T { SortedDictionary = ImmutableSortedDictionary.Create<int, string>().AddRange(dict) };
            using (var ms = new MemoryStream())
            {
                try
                {
                    model.Serialize(ms, obj);
                }
                catch (Exception ex)
                {
                    throw new ProtoException(caption + ":serialize", ex);
                }
                ms.Position = 0;
                T clone;
                try
                {
                    clone = (T)model.Deserialize(ms, null, typeof(T));
                }
                catch (Exception ex)
                {
                    throw new ProtoException(caption + ":deserialize", ex);
                }
                Assert.AreEqual(3, clone.SortedDictionary.Count, caption);
                Assert.AreEqual("a", clone.SortedDictionary[1], caption);
                Assert.AreEqual("b", clone.SortedDictionary[2], caption);
                Assert.AreEqual("c", clone.SortedDictionary[3], caption);
                AssertSequence(new[] { 1, 2, 3 }, clone.SortedDictionary.Keys, caption);
            }
        }

        interface IImmutableCollectionWrapper
        {
            ImmutableList<int> List { get; set; }
            //ImmutableArray<int> Array { get; set; }
            ImmutableDictionary<int, string> Dictionary { get; set; }
            ImmutableHashSet<int> HashSet { get; set; }
            ImmutableSortedSet<int> SortedSet { get; set; }
            ImmutableSortedDictionary<int,string> SortedDictionary { get; set; }
        }

        [ProtoContract]
        public class ImmutableConcreteProperties : IImmutableCollectionWrapper
        {
            [ProtoMember(1)]
            public ImmutableList<int> List { get; set; }

            //[ProtoMember(2)]
            //public ImmutableArray<int> Array { get; set; }

            [ProtoMember(3)]
            public ImmutableDictionary<int,string> Dictionary { get; set; }
            [ProtoMember(4)]
            public ImmutableHashSet<int> HashSet { get; set; }
            [ProtoMember(5)]
            public ImmutableSortedSet<int> SortedSet { get; set; }
            [ProtoMember(6)]
            public ImmutableSortedDictionary<int, string> SortedDictionary { get; set; }
        }

        [ProtoContract]
        public class ImmutableInterfaceProperties : IImmutableCollectionWrapper
        {
            [ProtoMember(1)]
            public IImmutableList<int> List { get; set; }

            [ProtoMember(3)]
            public IImmutableDictionary<int, string> Dictionary { get; set; }
            [ProtoMember(4)]
            public IImmutableSet<int> HashSet { get; set; }

            ImmutableList<int> IImmutableCollectionWrapper.List
            {
                get { return List.ToImmutableList(); }
                set { List = value; }
            }

            //ImmutableArray<int> IImmutableCollectionWrapper.Array
            //{
            //    get { throw new NotSupportedException(); }
            //    set { throw new NotSupportedException(); }
            //}

            ImmutableDictionary<int, string> IImmutableCollectionWrapper.Dictionary
            {
                get { return Dictionary.ToImmutableDictionary(); }
                set { Dictionary = value; }
            }

            ImmutableHashSet<int> IImmutableCollectionWrapper.HashSet
            {
                get { return HashSet.ToImmutableHashSet(); }
                set { HashSet = value; }
            }

            ImmutableSortedSet<int> IImmutableCollectionWrapper.SortedSet
            {
                get { throw new NotSupportedException(); }
                set { throw new NotSupportedException(); }
            }

            ImmutableSortedDictionary<int, string> IImmutableCollectionWrapper.SortedDictionary
            {
                get { throw new NotSupportedException(); }
                set { throw new NotSupportedException(); }
            }
        }



        [ProtoContract]
        public class ImmutableConcreteFields : IImmutableCollectionWrapper
        {


            [ProtoMember(1)]
            public ImmutableList<int> List;

            //[ProtoMember(2)]
            //public ImmutableArray<int> Array;

            [ProtoMember(3)]
            public ImmutableDictionary<int, string> Dictionary;

            [ProtoMember(4)]
            public ImmutableHashSet<int> HashSet;
            [ProtoMember(5)]
            public ImmutableSortedSet<int> SortedSet;
            [ProtoMember(6)]
            public ImmutableSortedDictionary<int, string> SortedDictionary;

            ImmutableList<int> IImmutableCollectionWrapper.List
            {
                get { return List; }
                set { List = value; }
            }

            //ImmutableArray<int> IImmutableCollectionWrapper.Array
            //{
            //    get { return Array; }
            //    set { Array = value; }
            //}

            ImmutableDictionary<int, string> IImmutableCollectionWrapper.Dictionary
            {
                get { return Dictionary; }
                set { Dictionary = value; }
            }

            ImmutableHashSet<int> IImmutableCollectionWrapper.HashSet
            {
                get { return HashSet.ToImmutableHashSet(); }
                set { HashSet = value; }
            }

            ImmutableSortedSet<int> IImmutableCollectionWrapper.SortedSet
            {
                get { return SortedSet.ToImmutableSortedSet(); }
                set { SortedSet = value; }
            }

            ImmutableSortedDictionary<int, string> IImmutableCollectionWrapper.SortedDictionary
            {
                get { return SortedDictionary.ToImmutableSortedDictionary(); }
                set { SortedDictionary = value; }
            }
        }

        [ProtoContract]
        public class ImmutableInterfaceFields : IImmutableCollectionWrapper
        {


            [ProtoMember(1)]
            public IImmutableList<int> List;

            [ProtoMember(3)]
            public IImmutableDictionary<int, string> Dictionary;

            [ProtoMember(4)]
            public IImmutableSet<int> HashSet;


            ImmutableList<int> IImmutableCollectionWrapper.List
            {
                get { return List.ToImmutableList(); }
                set { List = value; }
            }

            //ImmutableArray<int> IImmutableCollectionWrapper.Array
            //{
            //    get { throw new NotSupportedException(); }
            //    set { throw new NotSupportedException(); }
            //}

            ImmutableDictionary<int, string> IImmutableCollectionWrapper.Dictionary
            {
                get { return Dictionary.ToImmutableDictionary(); }
                set { Dictionary = value; }
            }

            ImmutableHashSet<int> IImmutableCollectionWrapper.HashSet
            {
                get { return HashSet.ToImmutableHashSet(); }
                set { HashSet = value; }
            }

            ImmutableSortedSet<int> IImmutableCollectionWrapper.SortedSet
            {
                get { throw new NotSupportedException(); }
                set { throw new NotSupportedException(); }
            }

            ImmutableSortedDictionary<int, string> IImmutableCollectionWrapper.SortedDictionary
            {
                get { throw new NotSupportedException(); }
                set { throw new NotSupportedException(); }
            }
        }



    }
}
