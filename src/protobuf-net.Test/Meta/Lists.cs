using System.Collections.Generic;
using System.Linq;
using Xunit;
using System.Collections;
using ProtoBuf.Meta;
using System.Diagnostics;
using System.IO;
using System;

namespace ProtoBuf.unittest.Meta
{
    
    public class PocoListTests
    {
        public class TypeWithLists
        {
            public List<string> ListString { get; set; }
            public IList<string> IListStringTyped { get; set; }

            public ArrayList ArrayListString { get; set; }
            public IList IListStringUntyped { get; set; }

            public List<int> ListInt32 { get; set; }
            public IList<int> IListInt32Typed { get; set; }

            public ArrayList ArrayListInt32 { get; set; }
            public IList IListInt32Untyped { get; set; }

        }

        

        [Fact]
        public void EmitModelWithEverything()
        {
            var model = TypeModel.Create();
            MetaType meta = model.Add(typeof(TypeWithLists), false);
            meta.Add(1, "ListString");
            meta.Add(2, "ListInt32");
            meta.Add(3, "IListStringTyped");
            meta.Add(4, "IListInt32Typed");

            meta.Add(5, "ArrayListString", typeof(string), null);
            meta.Add(6, "ArrayListInt32", typeof(int), null);
            meta.Add(7, "IListStringUntyped", typeof(string), null);
            meta.Add(8, "IListInt32Untyped", typeof(int), null);

            model.CompileInPlace();

            model.Compile("EmitModelWithEverything", "EmitModelWithEverything.dll");
            PEVerify.Verify("EmitModelWithEverything.dll");


        }

        [Fact]
        public void AddOnTypedListShouldResolveCorrectly_String()
        {
            var model = TypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(1, "ListString");
            Assert.Equal(typeof(TypeWithLists), model[typeof(TypeWithLists)][1].ParentType); //, "ParentType");
            Assert.Equal(typeof(string), model[typeof(TypeWithLists)][1].ItemType); //, "ItemType");
            Assert.Equal(typeof(List<string>), model[typeof(TypeWithLists)][1].MemberType); //, "MemberType");
            Assert.Equal(typeof(List<string>), model[typeof(TypeWithLists)][1].DefaultType); //, "DefaultType");
        }

        [Fact]
        public void AddOnTypedListShouldResolveCorrectly_Int32()
        {
            var model = TypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(1, "ListInt32");
            Assert.Equal(typeof(TypeWithLists), model[typeof(TypeWithLists)][1].ParentType); //, "ParentType");
            Assert.Equal(typeof(int), model[typeof(TypeWithLists)][1].ItemType); //, "ItemType");
            Assert.Equal(typeof(List<int>), model[typeof(TypeWithLists)][1].MemberType); //, "MemberType");
            Assert.Equal(typeof(List<int>), model[typeof(TypeWithLists)][1].DefaultType); //, "DefaultType");
        }

        [Fact]
        public void AddOnTypedIListShouldResolveCorrectly_String()
        {
            var model = TypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(2, "IListStringTyped");
            Assert.Equal(typeof(TypeWithLists), model[typeof(TypeWithLists)][2].ParentType); //, "ParentType");
            Assert.Equal(typeof(string), model[typeof(TypeWithLists)][2].ItemType); //, "ItemType");
            Assert.Equal(typeof(IList<string>), model[typeof(TypeWithLists)][2].MemberType); //, "MemberType");
            Assert.Equal(typeof(List<string>), model[typeof(TypeWithLists)][2].DefaultType); //, "DefaultType");
        }

        [Fact]
        public void AddOnTypedIListShouldResolveCorrectly_Int32()
        {
            var model = TypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(2, "IListInt32Typed");
            Assert.Equal(typeof(TypeWithLists), model[typeof(TypeWithLists)][2].ParentType); //, "ParentType");
            Assert.Equal(typeof(int), model[typeof(TypeWithLists)][2].ItemType); //, "ItemType");
            Assert.Equal(typeof(IList<int>), model[typeof(TypeWithLists)][2].MemberType); //, "MemberType");
            Assert.Equal(typeof(List<int>), model[typeof(TypeWithLists)][2].DefaultType); //, "DefaultType");
        }

        [Fact]
        public void RoundTripTypedList_String()
        {
            var model = TypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(1, "ListString");
            TypeWithLists obj = new TypeWithLists();
            obj.ListString = new List<string>();
            obj.ListString.Add("abc");
            obj.ListString.Add("def");

            TypeWithLists clone = (TypeWithLists)model.DeepClone(obj);
            Assert.NotNull(clone);
            Assert.NotNull(clone.ListString);
            Assert.True(obj.ListString.SequenceEqual(clone.ListString));

            model.CompileInPlace();
            clone = (TypeWithLists)model.DeepClone(obj);
            Assert.NotNull(clone);
            Assert.NotNull(clone.ListString);
            Assert.True(obj.ListString.SequenceEqual(clone.ListString));

            clone = (TypeWithLists)model.Compile().DeepClone(obj);
            Assert.NotNull(clone);
            Assert.NotNull(clone.ListString);
            Assert.True(obj.ListString.SequenceEqual(clone.ListString));
        }

        [Fact]
        public void RoundTripTypedIList_String()
        {
            var model = TypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(2, "IListStringTyped");
            TypeWithLists obj = new TypeWithLists();
            obj.IListStringTyped = new List<string>();
            obj.IListStringTyped.Add("abc");
            obj.IListStringTyped.Add("def");

            TypeWithLists clone = (TypeWithLists)model.DeepClone(obj);
            Assert.NotNull(clone);
            Assert.NotNull(clone.IListStringTyped);
            Assert.True(obj.IListStringTyped.SequenceEqual(clone.IListStringTyped));

            model.CompileInPlace();
            clone = (TypeWithLists)model.DeepClone(obj);
            Assert.NotNull(clone);
            Assert.NotNull(clone.IListStringTyped);
            Assert.True(obj.IListStringTyped.SequenceEqual(clone.IListStringTyped));

            clone = (TypeWithLists)model.Compile().DeepClone(obj);
            Assert.NotNull(clone);
            Assert.NotNull(clone.IListStringTyped);
            Assert.True(obj.IListStringTyped.SequenceEqual(clone.IListStringTyped));
        }


        [Fact]
        public void RoundTripArrayList_String()
        {
            var model = TypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(3, "ArrayListString", typeof(string), null);
            TypeWithLists obj = new TypeWithLists();
            obj.ArrayListString = new ArrayList();
            obj.ArrayListString.Add("abc");
            obj.ArrayListString.Add("def");

            TypeWithLists clone = (TypeWithLists)model.DeepClone(obj);
            Assert.NotNull(clone);
            Assert.NotNull(clone.ArrayListString);
            Assert.True(obj.ArrayListString.Cast<string>().SequenceEqual(clone.ArrayListString.Cast<string>()));

            model.CompileInPlace();
            clone = (TypeWithLists)model.DeepClone(obj);
            Assert.NotNull(clone);
            Assert.NotNull(clone.ArrayListString);
            Assert.True(obj.ArrayListString.Cast<string>().SequenceEqual(clone.ArrayListString.Cast<string>()));

            clone = (TypeWithLists)model.Compile().DeepClone(obj);
            Assert.NotNull(clone);
            Assert.NotNull(clone.ArrayListString);
            Assert.True(obj.ArrayListString.Cast<string>().SequenceEqual(clone.ArrayListString.Cast<string>()));
        }

        [Fact]
        public void RoundTripIList_String()
        {
            var model = TypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(4, "IListStringUntyped", typeof(string), null);
            TypeWithLists obj = new TypeWithLists();
            obj.IListStringUntyped = new ArrayList();
            obj.IListStringUntyped.Add("abc");
            obj.IListStringUntyped.Add("def");

            TypeWithLists clone = (TypeWithLists)model.DeepClone(obj);
            Assert.NotNull(clone);
            Assert.NotNull(clone.IListStringUntyped);
            Assert.True(obj.IListStringUntyped.Cast<string>().SequenceEqual(clone.IListStringUntyped.Cast<string>()));

            model.CompileInPlace();
            clone = (TypeWithLists)model.DeepClone(obj);
            Assert.NotNull(clone);
            Assert.NotNull(clone.IListStringUntyped);
            Assert.True(obj.IListStringUntyped.Cast<string>().SequenceEqual(clone.IListStringUntyped.Cast<string>()));

            clone = (TypeWithLists)model.Compile().DeepClone(obj);
            Assert.NotNull(clone);
            Assert.NotNull(clone.IListStringUntyped);
            Assert.True(obj.IListStringUntyped.Cast<string>().SequenceEqual(clone.IListStringUntyped.Cast<string>()));
        }

        [Fact]
        public void RoundTripTypedList_Int32()
        {
            var model = TypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(1, "ListInt32");
            TypeWithLists obj = new TypeWithLists();
            obj.ListInt32 = new List<int>();
            obj.ListInt32.Add(123);
            obj.ListInt32.Add(456);

            TypeWithLists clone = (TypeWithLists)model.DeepClone(obj);
            Assert.NotNull(clone);
            Assert.NotNull(clone.ListInt32);
            Assert.True(obj.ListInt32.SequenceEqual(clone.ListInt32));

            model.CompileInPlace();
            clone = (TypeWithLists)model.DeepClone(obj);
            Assert.NotNull(clone);
            Assert.NotNull(clone.ListInt32);
            Assert.True(obj.ListInt32.SequenceEqual(clone.ListInt32));

            clone = (TypeWithLists)model.Compile().DeepClone(obj);
            Assert.NotNull(clone);
            Assert.NotNull(clone.ListInt32);
            Assert.True(obj.ListInt32.SequenceEqual(clone.ListInt32));
        }

        [Fact]
        public void RoundTripTypedIList_Int32()
        {
            var model = TypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(2, "IListInt32Typed");
            TypeWithLists obj = new TypeWithLists();
            obj.IListInt32Typed = new List<int>();
            obj.IListInt32Typed.Add(123);
            obj.IListInt32Typed.Add(456);

            TypeWithLists clone = (TypeWithLists)model.DeepClone(obj);
            Assert.NotNull(clone);
            Assert.NotNull(clone.IListInt32Typed);
            Assert.True(obj.IListInt32Typed.SequenceEqual(clone.IListInt32Typed));

            model.CompileInPlace();
            clone = (TypeWithLists)model.DeepClone(obj);
            Assert.NotNull(clone);
            Assert.NotNull(clone.IListInt32Typed);
            Assert.True(obj.IListInt32Typed.SequenceEqual(clone.IListInt32Typed));

            clone = (TypeWithLists)model.Compile().DeepClone(obj);
            Assert.NotNull(clone);
            Assert.NotNull(clone.IListInt32Typed);
            Assert.True(obj.IListInt32Typed.SequenceEqual(clone.IListInt32Typed));
        }


        [Fact]
        public void RoundTripArrayList_Int32()
        {
            var model = TypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(3, "ArrayListInt32", typeof(int), null);
            TypeWithLists obj = new TypeWithLists();
            obj.ArrayListInt32 = new ArrayList();
            obj.ArrayListInt32.Add(123);
            obj.ArrayListInt32.Add(456);

            TypeWithLists clone = (TypeWithLists)model.DeepClone(obj);
            Assert.NotNull(clone);
            Assert.NotNull(clone.ArrayListInt32);
            Assert.True(obj.ArrayListInt32.Cast<int>().SequenceEqual(clone.ArrayListInt32.Cast<int>()));

            model.CompileInPlace();
            clone = (TypeWithLists)model.DeepClone(obj);
            Assert.NotNull(clone);
            Assert.NotNull(clone.ArrayListInt32);
            Assert.True(obj.ArrayListInt32.Cast<int>().SequenceEqual(clone.ArrayListInt32.Cast<int>()));

            clone = (TypeWithLists)model.Compile().DeepClone(obj);
            Assert.NotNull(clone);
            Assert.NotNull(clone.ArrayListInt32);
            Assert.True(obj.ArrayListInt32.Cast<int>().SequenceEqual(clone.ArrayListInt32.Cast<int>()));
        }

        [Fact]
        public void RoundTripIList_Int32()
        {
            var model = TypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(4, "IListInt32Untyped", typeof(int), null);
            TypeWithLists obj = new TypeWithLists();
            obj.IListInt32Untyped = new ArrayList();
            obj.IListInt32Untyped.Add(123);
            obj.IListInt32Untyped.Add(456);

            TypeWithLists clone = (TypeWithLists)model.DeepClone(obj);
            Assert.NotNull(clone);
            Assert.NotNull(clone.IListInt32Untyped);
            Assert.True(obj.IListInt32Untyped.Cast<int>().SequenceEqual(clone.IListInt32Untyped.Cast<int>()));

            model.CompileInPlace();
            clone = (TypeWithLists)model.DeepClone(obj);
            Assert.NotNull(clone);
            Assert.NotNull(clone.IListInt32Untyped);
            Assert.True(obj.IListInt32Untyped.Cast<int>().SequenceEqual(clone.IListInt32Untyped.Cast<int>()));

            clone = (TypeWithLists)model.Compile().DeepClone(obj);
            Assert.NotNull(clone);
            Assert.NotNull(clone.IListInt32Untyped);
            Assert.True(obj.IListInt32Untyped.Cast<int>().SequenceEqual(clone.IListInt32Untyped.Cast<int>()));
        }

        public class NastyType
        {
            
            public List<List<int>> JaggedList { get; set; }

            public List<int[]> ListOfArray{ get; set; }

            public int[,] MultiDimArray { get; set; }

            public int[][] JaggedArray { get; set; }

            public List<int>[] ArrayOfList{ get; set; }

            public List<int> BasicList { get; set; }

            public int[] BasicArray{ get; set; }

            public byte[] Blob { get; set; }

            public List<byte[]> Blobs{ get; set; }
        }
        [Fact]
        public void JaggedListShouldThrow()
        {
            try {
                var model = RuntimeTypeModel.Create();
                model.Add(typeof(NastyType), true).Add(1, "JaggedList");
                model.CompileInPlace();
                Assert.Equal(42, 0); // fail
            } catch(NotSupportedException) {  }
        }
        [Fact]
        public void ListOfArrayShouldThrow()
        {
            try {
                var model = RuntimeTypeModel.Create();
                model.Add(typeof(NastyType), true).Add(1, "ListOfArray");
                model.CompileInPlace();
                Assert.Equal(42, 0); // fail
            }
            catch (NotSupportedException) { }
        }
        [Fact]
        public void MultiDimArrayShouldThrow()
        {
            try {
                var model = RuntimeTypeModel.Create();
                model.Add(typeof(NastyType), true).Add(1, "MultiDimArray");
                model.CompileInPlace();
                Assert.Equal(42, 0); // fail
            }
            catch (NotSupportedException) { }
        }
        [Fact]
        public void JaggedArrayShouldThrow()
        {
            try {
                var model = RuntimeTypeModel.Create();
                model.Add(typeof(NastyType), true).Add(1, "JaggedArray");
                model.CompileInPlace();
                Assert.Equal(42, 0); // fail
            }
            catch (NotSupportedException) { }
        }
        [Fact]
        public void ArrayOfListShouldThrow()
        {
            try {
                var model = RuntimeTypeModel.Create();
                model.Add(typeof(NastyType), true).Add(1, "ArrayOfList");
                model.CompileInPlace();
                    Assert.Equal(42, 0); // fail
            }
            catch (NotSupportedException) { }
        }
        [Fact]
        public void BasicListIsFine()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(NastyType), true).Add(1, "BasicList");
            model.CompileInPlace();
        }
        [Fact]
        public void BasicArrayIsFine()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(NastyType), true).Add(1, "BasicArray");
            model.CompileInPlace();
        }
        [Fact]
        public void BlobIsFine()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(NastyType), true).Add(1, "Blob");
            model.CompileInPlace();
        }
        [Fact]
        public void BlobsAreFine()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(NastyType), true).Add(1, "Blobs");
            model.CompileInPlace();
        }
        [Fact]
        public void PEVerifyArraysAndLists()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(NastyType), true)
                //.Add(1, "Blobs")
                //.Add(2, "Blob")
                .Add(3, "BasicArray")
                //.Add(4, "BasicList")
                ;
            model.CompileInPlace();

            model.Compile("PEVerifyArraysAndLists","PEVerifyArraysAndLists.dll");
            PEVerify.Verify("PEVerifyArraysAndLists.dll");
        }


    }

    
    public class PackedLists
    {
        [Fact]
        public void CanCompile()
        {
            var model = CreateModel();
            model.Compile("PEVerifyPackedLists", "PEVerifyPackedLists.dll");
            PEVerify.Verify("PEVerifyPackedLists.dll");
        }

        [Fact]
        public void TestNullRoundTrip()
        {
            var model = CreateModel();

            var orig = new PackedData {ListInt32 = null, ListSingle = null, ListDouble = null};
            int len;

            var clone = RoundTrip(model, orig, "Runtime", out len);
            Assert.Equal(0, len); //, "Runtime");
            Assert.Null(clone.ListDouble);
            Assert.Null(clone.ListInt32);
            Assert.Null(clone.ListSingle);

            model.CompileInPlace();
            clone = RoundTrip(model, orig, "CompileInPlace", out len);
            Assert.Equal(0, len); //, "CompileInPlace");
            Assert.Null(clone.ListDouble);
            Assert.Null(clone.ListInt32);
            Assert.Null(clone.ListSingle);

            clone = RoundTrip(model.Compile(), orig, "Compile", out len);
            Assert.Equal(0, len); //, "Compile");
            Assert.Null(clone.ListDouble);
            Assert.Null(clone.ListInt32);
            Assert.Null(clone.ListSingle);
        }

        [Fact]
        public void TestEmptyRoundTrip()
        {
            var model = CreateModel();

            var orig = new PackedData { ListInt32 = new List<int>(), ListSingle = new List<float>(), ListDouble = new List<double>()};
            int len;

            var clone = RoundTrip(model, orig, "Runtime", out len);
            Assert.Equal(6, len); //, "Runtime");
            Assert.Empty(clone.ListDouble);
            Assert.Empty(clone.ListInt32);
            Assert.Empty(clone.ListSingle);

            model.CompileInPlace();
            clone = RoundTrip(model, orig, "CompileInPlace", out len);
            Assert.Equal(6, len); //, "CompileInPlace");
            Assert.Empty(clone.ListDouble);
            Assert.Empty(clone.ListInt32);
            Assert.Empty(clone.ListSingle);

            clone = RoundTrip(model.Compile(), orig, "Compile", out len);
            Assert.Equal(6, len); //, "Compile");
            Assert.Empty(clone.ListDouble);
            Assert.Empty(clone.ListInt32);
            Assert.Empty(clone.ListSingle);
        }
        static void CheckExpectedListContents(PackedData data, string text)
        {
            Assert.NotNull(data); //, text);
            Assert.Equal(3, data.ListInt32.Count); //, text);
            Assert.Equal(3, data.ListInt32[0]); //, text);
            Assert.Equal(5, data.ListInt32[1]); //, text);
            Assert.Equal(7, data.ListInt32[2]); //, text);
            Assert.Equal(3, data.ListSingle.Count); //, text);
            Assert.Equal(3F, data.ListSingle[0]); //, text);
            Assert.Equal(5F, data.ListSingle[1]); //, text);
            Assert.Equal(7F, data.ListSingle[2]); //, text);
            Assert.Equal(3, data.ListDouble.Count); //, text);
            Assert.Equal(3D, data.ListDouble[0]); //, text);
            Assert.Equal(5D, data.ListDouble[1]); //, text);
            Assert.Equal(7D, data.ListDouble[2]); //, text);
        }
        [Fact]
        public void TestThreeItemsRoundTrip()
        {
            var model = CreateModel();

            var orig = new PackedData { ListInt32 = new List<int> {3,5,7}, ListSingle = new List<float> {3F,5F,7F}, ListDouble = new List<double> {3D,5D,7F} };
            CheckExpectedListContents(orig, "Original");
            int len;

            var clone = RoundTrip(model, orig, "Runtime", out len);
            const int expectedLen = (1 + 1 + 1 + 1 + 1) + (1 + 1 + 4 + 4 + 4) + (1 + 1 + 8 + 8 + 8);
            Assert.Equal(expectedLen, len); //, "Runtime");
            Assert.NotNull(clone);
            CheckExpectedListContents(clone, "Runtime");

            model.CompileInPlace();
            clone = RoundTrip(model, orig, "CompileInPlace", out len);
            Assert.Equal(expectedLen, len); //, "CompileInPlace");
            Assert.NotNull(clone);
            CheckExpectedListContents(clone, "CompileInPlace");

            clone = RoundTrip(model.Compile(), orig, "Compile", out len);
            Assert.Equal(expectedLen, len); //, "Compile");
            Assert.NotNull(clone);
            CheckExpectedListContents(clone, "Compile");
        }
        static PackedData RoundTrip(TypeModel model, PackedData orig, string scenario, out int length)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    model.Serialize(ms, orig);
                    length = (int)ms.Length;
                    ms.Position = 0;
                    return (PackedData)model.Deserialize(ms, null, typeof(PackedData));
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(scenario + ": " + ex.Message, ex);
            }
        }
        static RuntimeTypeModel CreateModel()
        {
            var model = TypeModel.Create();
            model.Add(typeof (PackedData), true);
            return model;
        }
        [ProtoContract]
        public class PackedData
        {
            [ProtoMember(1, IsPacked = true)]
            public List<int> ListInt32 { get; set; }
            [ProtoMember(2, IsPacked = true)]
            public List<float> ListSingle { get; set; }
            [ProtoMember(3, IsPacked = true)]
            public List<double> ListDouble{ get; set; }
        }
    }

    
    public class PackedArrays
    {
        [Fact]
        public void CanCompile()
        {
            var model = CreateModel();
            model.Compile("PEVerifyPackedArrays", "PEVerifyPackedArrays.dll");
            PEVerify.Verify("PEVerifyPackedArrays.dll");
        }

        [Fact]
        public void TestEmptyRoundTrip()
        {
            var model = CreateModel();

            var orig = new PackedData { ArrayInt32 = new int[0], ArraySingle = new float[0], ArrayDouble = new double[0] };
            int len;

            var clone = RoundTrip(model, orig, "Runtime", out len);
            Assert.Equal(6, len); //, "Runtime");
            Assert.Empty(clone.ArrayDouble);
            Assert.Empty(clone.ArrayInt32);
            Assert.Empty(clone.ArraySingle);

            model.CompileInPlace();
            clone = RoundTrip(model, orig, "CompileInPlace", out len);
            Assert.Equal(6, len); //, "CompileInPlace");
            Assert.Empty(clone.ArrayDouble);
            Assert.Empty(clone.ArrayInt32);
            Assert.Empty(clone.ArraySingle);

            clone = RoundTrip(model.Compile(), orig, "Compile", out len);
            Assert.Equal(6, len); //, "Compile");
            Assert.Empty(clone.ArrayDouble);
            Assert.Empty(clone.ArrayInt32);
            Assert.Empty(clone.ArraySingle);
        }
        [Fact]
        public void TestNullRoundTrip()
        {
            var model = CreateModel();

            var orig = new PackedData { ArrayInt32 = null, ArraySingle = null, ArrayDouble = null };
            int len;

            var clone = RoundTrip(model, orig, "Runtime", out len);
            Assert.Equal(0, len); //, "Runtime");
            Assert.Null(clone.ArrayDouble);
            Assert.Null(clone.ArrayInt32);
            Assert.Null(clone.ArraySingle);

            model.CompileInPlace();
            clone = RoundTrip(model, orig, "CompileInPlace", out len);
            Assert.Equal(0, len); //, "CompileInPlace");
            Assert.Null(clone.ArrayDouble);
            Assert.Null(clone.ArrayInt32);
            Assert.Null(clone.ArraySingle);

            clone = RoundTrip(model.Compile(), orig, "Compile", out len);
            Assert.Equal(0, len); //, "Compile");
            Assert.Null(clone.ArrayDouble);
            Assert.Null(clone.ArrayInt32);
            Assert.Null(clone.ArraySingle);
        }
        static void CheckExpectedListContents(PackedData data, string text)
        {
            Assert.NotNull(data); //, text);
            Assert.Equal(3, data.ArrayInt32.Length); //, text);
            Assert.Equal(3, data.ArrayInt32[0]); //, text);
            Assert.Equal(5, data.ArrayInt32[1]); //, text);
            Assert.Equal(7, data.ArrayInt32[2]); //, text);
            Assert.Equal(3, data.ArraySingle.Length); //, text);
            Assert.Equal(3F, data.ArraySingle[0]); //, text);
            Assert.Equal(5F, data.ArraySingle[1]); //, text);
            Assert.Equal(7F, data.ArraySingle[2]); //, text);
            Assert.Equal(3, data.ArrayDouble.Length); //, text);
            Assert.Equal(3D, data.ArrayDouble[0]); //, text);
            Assert.Equal(5D, data.ArrayDouble[1]); //, text);
            Assert.Equal(7D, data.ArrayDouble[2]); //, text);
        }
        [Fact]
        public void TestThreeItemsRoundTrip()
        {
            var model = CreateModel();

            var orig = new PackedData { ArrayInt32 = new int[] { 3, 5, 7 }, ArraySingle = new float[] { 3F, 5F, 7F }, ArrayDouble = new double[] { 3D, 5D, 7F } };
            CheckExpectedListContents(orig, "Original");
            int len;

            var clone = RoundTrip(model, orig, "Runtime", out len);
            const int expectedLen = (1 + 1 + 1 + 1 + 1) + (1 + 1 + 4 + 4 + 4) + (1 + 1 + 8 + 8 + 8);
            Assert.Equal(expectedLen, len); //, "Runtime");
            Assert.NotNull(clone);
            CheckExpectedListContents(clone, "Runtime");

            model.CompileInPlace();
            clone = RoundTrip(model, orig, "CompileInPlace", out len);
            Assert.Equal(expectedLen, len); //, "CompileInPlace");
            Assert.NotNull(clone);
            CheckExpectedListContents(clone, "CompileInPlace");

            clone = RoundTrip(model.Compile(), orig, "Compile", out len);
            Assert.Equal(expectedLen, len); //, "Compile");
            Assert.NotNull(clone);
            CheckExpectedListContents(clone, "Compile");
        }
        static PackedData RoundTrip(TypeModel model, PackedData orig, string scenario, out int length)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    model.Serialize(ms, orig);
                    length = (int)ms.Length;
                    ms.Position = 0;
                    return (PackedData)model.Deserialize(ms, null, typeof(PackedData));
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(scenario + ": " + ex.Message, ex);
            }
        }
        static RuntimeTypeModel CreateModel()
        {
            var model = TypeModel.Create();
            model.Add(typeof(PackedData), true);
            return model;
        }
        [ProtoContract]
        public class PackedData
        {
            [ProtoMember(1, IsPacked = true)]
            public int[] ArrayInt32 { get; set; }
            [ProtoMember(2, IsPacked = true)]
            public float[] ArraySingle { get; set; }
            [ProtoMember(3, IsPacked = true)]
            public double[] ArrayDouble { get; set; }
        }
    }
}
