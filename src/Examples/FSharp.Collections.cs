using Xunit;
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
using Microsoft.FSharp.Collections;

namespace Examples
{

    public class FSharpCollections
    {
        #region Dictionary
        [Fact]
        public void Dictionary_FSharpConcreteProperties()
        {
            TestDictionaryImpl<FSharpConcreteProperties>();
        }

        [Fact]
        public void Dictionary_FSharpConcreteFields()
        {
            TestDictionaryImpl<FSharpConcreteFields>();
        }
        [Fact]
        public void Dictionary_FSharpInterfaceProperties()
        {
            TestDictionaryImpl<FSharpInterfaceProperties>();
        }

        [Fact]
        public void Dictionary_ImmutableInterfaceFields()
        {
            TestDictionaryImpl<ImmutableInterfaceFields>();
        }
        #endregion
        #region List
        [Fact]
        public void List_FSharpConcreteProperties()
        {
            TestListImpl<FSharpConcreteProperties>();
        }

        [Fact]
        public void List_FSharpConcreteFields()
        {
            TestListImpl<FSharpConcreteFields>();
        }
        [Fact]
        public void List_FSharpInterfaceProperties()
        {
            TestListImpl<FSharpInterfaceProperties>();
        }

        [Fact]
        public void List_ImmutableInterfaceFields()
        {
            TestListImpl<ImmutableInterfaceFields>();
        }
        #endregion

        //#region Array
        //[Fact]
        //public void Array_FSharpConcreteProperties()
        //{
        //    TestArrayImpl<FSharpConcreteProperties>();
        //}

        //[Fact]
        //public void Array_FSharpConcreteFields()
        //{
        //    TestArrayImpl<FSharpConcreteFields>();
        //}
        //#endregion

        #region Set
        [Fact]
        public void Set_FSharpConcreteProperties()
        {
            TestSetImpl<FSharpConcreteProperties>();
        }

        [Fact]
        public void Set_FSharpConcreteFields()
        {
            TestSetImpl<FSharpConcreteFields>();
        }
        [Fact]
        public void Set_FSharpInterfaceProperties()
        {
            TestSetImpl<FSharpInterfaceProperties>();
        }

        [Fact]
        public void Set_ImmutableInterfaceFields()
        {
            TestSetImpl<ImmutableInterfaceFields>();
        }


        #endregion


        private static void TestDictionaryImpl<T>([CallerMemberName] string name = null)
            where T : class, IFSharpCollectionWrapper, new()
        {
            var model = RuntimeTypeModel.Create();
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
        where T : class, IFSharpCollectionWrapper, new()
        {
            var values = new Dictionary<int, string> { { 1, "a" }, { 2, "b" }, { 3, "c" }, { 4, "d" } };
            var obj = new T { Map = MapModule.OfSeq<int, string>(values.Select(r=>new Tuple<int,string>(r.Key, r.Value)))};

            using var ms = new MemoryStream();
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
#pragma warning disable CS0618
                clone = (T)model.Deserialize(ms, null, typeof(T));
#pragma warning restore CS0618
            }
            catch (Exception ex)
            {
                throw new ProtoException(caption + ":deserialize", ex);
            }
            Assert.Equal(4, clone.Map.Count); //, caption);
            Assert.Equal("a", clone.Map[1]); //, caption);
            Assert.Equal("b", clone.Map[2]); //, caption);
            Assert.Equal("c", clone.Map[3]); //, caption);
            Assert.Equal("d", clone.Map[4]); //, caption);
        }

        private static void TestListImpl<T>([CallerMemberName] string name = null)
            where T : class, IFSharpCollectionWrapper, new()
        {
            var model = RuntimeTypeModel.Create();
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
            where T : class, IFSharpCollectionWrapper, new()
        {
            int[] values = { 1, 2, 3, 4 };
            var obj = new T { List = ListModule.OfArray<int> (values) };

            using var ms = new MemoryStream();
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
#pragma warning disable CS0618
                clone = (T)model.Deserialize(ms, null, typeof(T));
#pragma warning restore CS0618
            }
            catch (Exception ex)
            {
                throw new ProtoException(caption + ":deserialize", ex);
            }
            AssertSequence(new[] { 1, 2, 3, 4 }, clone.List, caption);
            ms.Position = 0;
            try
            {
#pragma warning disable CS0618
                model.Deserialize(ms, clone, type: null); // this is append!
#pragma warning restore CS0618
            }
            catch (Exception ex)
            {
                throw new ProtoException(caption + ":serialize", ex);
            }
            AssertSequence(new[] { 1, 2, 3, 4, 1, 2, 3, 4 }, clone.List, caption);
        }

#pragma warning disable IDE0060
        static void AssertSequence<T>(IEnumerable<T> expected, IEnumerable<T> actual, string caption)
#pragma warning restore IDE0060
        {
            if (expected == null)
            {
                Assert.Null(actual); //, caption);
                return;
            }
            if (expected != null) Assert.NotNull(actual); //, caption);

            Assert.NotSame(expected, actual); //, caption);

            var expArr = expected.ToArray();
            var actArr = actual.ToArray();
            Assert.Equal(expArr.Length, actArr.Length); //, caption + ":length");
            for (int i = 0; i < actArr.Length; i++)
            {
                Assert.Equal(expArr[i], actArr[i]); //, caption + ":" + i);
            }
        }

        //private static void TestArrayImpl<T>([CallerMemberName] string name = null)
        //    where T : class, IImmutableCollectionWrapper, new()
        //{
        //    var model = RuntimeTypeModel.Create();
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


        private static void TestSetImpl<T>([CallerMemberName] string name = null)
            where T : class, IFSharpCollectionWrapper, new()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            TestSetImpl<T>(model, "Runtime");

            var external = model.Compile(name, name + ".dll");
            PEVerify.AssertValid(name + ".dll");
            TestSetImpl<T>(external, "External");

            model.CompileInPlace();
            TestSetImpl<T>(model, "CompileInPlace");
            TestSetImpl<T>(model.Compile(), "Compile");
        }

        private static void TestSetImpl<T>(TypeModel model, string caption)
            where T : class, IFSharpCollectionWrapper, new()
        {
            var obj = new T { Set = SetModule.OfArray<int> (new[] { 1, 3, 2 }) };
            using var ms = new MemoryStream();
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
#pragma warning disable CS0618
                clone = (T)model.Deserialize(ms, null, typeof(T));
#pragma warning restore CS0618
            }
            catch (Exception ex)
            {
                throw new ProtoException(caption + ":deserialize", ex);
            }
            Assert.Equal(3, clone.Set.Count); //, caption);
            Assert.Contains(1, clone.Set); //, caption);
            Assert.Contains(2, clone.Set); //, caption);
            Assert.Contains(3, clone.Set); //, caption);
        }
        interface IFSharpCollectionWrapper
        {
            FSharpList<int> List { get; set; }
            //ImmutableArray<int> Array { get; set; }
            FSharpMap<int, string> Map { get; set; }
            FSharpSet<int> Set { get; set; }
        }

        [ProtoContract]
        public class FSharpConcreteProperties : IFSharpCollectionWrapper
        {
            [ProtoMember(1)]
            public FSharpList<int> List { get; set; }

            //[ProtoMember(2)]
            //public ImmutableArray<int> Array { get; set; }

            [ProtoMember(3)]
            public FSharpMap<int, string> Map { get; set; }
            [ProtoMember(4)]
            public FSharpSet<int> Set { get; set; }
            [ProtoMember(6)]
            public ImmutableSortedDictionary<int, string> SortedDictionary { get; set; }
        }

        [ProtoContract]
        public class FSharpInterfaceProperties : IFSharpCollectionWrapper
        {
            [ProtoMember(1)]
            public FSharpList<int> List { get; set; }

            [ProtoMember(3)]
            public FSharpMap<int, string> Map { get; set; }
            [ProtoMember(4)]
            public FSharpSet<int> Set { get; set; }

            FSharpList<int> IFSharpCollectionWrapper.List
            {
                get { return List; }
                set { List = value; }
            }

            //ImmutableArray<int> IImmutableCollectionWrapper.Array
            //{
            //    get { throw new NotSupportedException(); }
            //    set { throw new NotSupportedException(); }
            //}

            FSharpMap<int, string> IFSharpCollectionWrapper.Map
            {
                get { return Map; }
                set { Map = value; }
            }

            FSharpSet<int> IFSharpCollectionWrapper.Set
            {
                get { return Set; }
                set { Set = value; }
            }
        }



        [ProtoContract]
        public class FSharpConcreteFields : IFSharpCollectionWrapper
        {


            [ProtoMember(1)]
            public FSharpList<int> List;

            //[ProtoMember(2)]
            //public ImmutableArray<int> Array;

            [ProtoMember(3)]
            public FSharpMap<int, string> Dictionary;

            [ProtoMember(4)]
            public FSharpSet<int> Set;
            [ProtoMember(5)]
            public ImmutableSortedSet<int> SortedSet;
            [ProtoMember(6)]
            public ImmutableSortedDictionary<int, string> SortedDictionary;

            FSharpList<int> IFSharpCollectionWrapper.List
            {
                get { return List; }
                set { List = value; }
            }

            //ImmutableArray<int> IImmutableCollectionWrapper.Array
            //{
            //    get { return Array; }
            //    set { Array = value; }
            //}

            FSharpMap<int, string> IFSharpCollectionWrapper.Map
            {
                get { return Dictionary; }
                set { Dictionary = value; }
            }

            FSharpSet<int> IFSharpCollectionWrapper.Set
            {
                get { return Set; }
                set { Set = value; }
            }
        }

        [ProtoContract]
        public class ImmutableInterfaceFields : IFSharpCollectionWrapper
        {


            [ProtoMember(1)]
            public FSharpList<int> List;

            [ProtoMember(3)]
            public FSharpMap<int, string> Map;

            [ProtoMember(4)]
            public FSharpSet<int> Set;


            FSharpList<int> IFSharpCollectionWrapper.List
            {
                get { return List; }
                set { List = value; }
            }

            //ImmutableArray<int> IImmutableCollectionWrapper.Array
            //{
            //    get { throw new NotSupportedException(); }
            //    set { throw new NotSupportedException(); }
            //}

            FSharpMap<int, string> IFSharpCollectionWrapper.Map
            {
                get { return Map; }
                set { Map = value; }
            }

            FSharpSet<int> IFSharpCollectionWrapper.Set
            {
                get { return Set; }
                set { Set = value; }
            }
        }
    }
}
