using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.unittest.Meta
{

    public class PocoListTests
    {
        public class TypeWithLists
        {
            public List<string> ListString { get; set; }
            public IList<string> IListStringTyped { get; set; }

            public List<int> ListInt32 { get; set; }
            public IList<int> IListInt32Typed { get; set; }
        }

        

        [Fact]
        public void EmitModelWithEverything()
        {
            var model = RuntimeTypeModel.Create();
            MetaType meta = model.Add(typeof(TypeWithLists), false);
            meta.Add(1, "ListString");
            meta.Add(2, "ListInt32");
            meta.Add(3, "IListStringTyped");
            meta.Add(4, "IListInt32Typed");


            model.CompileInPlace();

            model.Compile("EmitModelWithEverything", "EmitModelWithEverything.dll");
            PEVerify.Verify("EmitModelWithEverything.dll");


        }

        [Fact]
        public void AddOnTypedListShouldResolveCorrectly_String()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(1, "ListString");
            Assert.Equal(typeof(TypeWithLists), model[typeof(TypeWithLists)][1].ParentType); //, "ParentType");
            Assert.Equal(typeof(string), model[typeof(TypeWithLists)][1].ItemType); //, "ItemType");
            Assert.Equal(typeof(List<string>), model[typeof(TypeWithLists)][1].MemberType); //, "MemberType");
            Assert.Equal(typeof(List<string>), model[typeof(TypeWithLists)][1].DefaultType); //, "DefaultType");
        }

        [Fact]
        public void AddOnTypedListShouldResolveCorrectly_Int32()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(1, "ListInt32");
            Assert.Equal(typeof(TypeWithLists), model[typeof(TypeWithLists)][1].ParentType); //, "ParentType");
            Assert.Equal(typeof(int), model[typeof(TypeWithLists)][1].ItemType); //, "ItemType");
            Assert.Equal(typeof(List<int>), model[typeof(TypeWithLists)][1].MemberType); //, "MemberType");
            Assert.Equal(typeof(List<int>), model[typeof(TypeWithLists)][1].DefaultType); //, "DefaultType");
        }

        [Fact]
        public void AddOnTypedIListShouldResolveCorrectly_String()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(2, "IListStringTyped");
            Assert.Equal(typeof(TypeWithLists), model[typeof(TypeWithLists)][2].ParentType); //, "ParentType");
            Assert.Equal(typeof(string), model[typeof(TypeWithLists)][2].ItemType); //, "ItemType");
            Assert.Equal(typeof(IList<string>), model[typeof(TypeWithLists)][2].MemberType); //, "MemberType");
            Assert.Equal(typeof(IList<string>), model[typeof(TypeWithLists)][2].DefaultType); //, "DefaultType");
        }

        [Fact]
        public void AddOnTypedIListShouldResolveCorrectly_Int32()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(2, "IListInt32Typed");
            Assert.Equal(typeof(TypeWithLists), model[typeof(TypeWithLists)][2].ParentType); //, "ParentType");
            Assert.Equal(typeof(int), model[typeof(TypeWithLists)][2].ItemType); //, "ItemType");
            Assert.Equal(typeof(IList<int>), model[typeof(TypeWithLists)][2].MemberType); //, "MemberType");
            Assert.Equal(typeof(IList<int>), model[typeof(TypeWithLists)][2].DefaultType); //, "DefaultType");
        }

        [Fact]
        public void RoundTripTypedList_String()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(1, "ListString");
            TypeWithLists obj = new TypeWithLists
            {
                ListString = new List<string>
                {
                    "abc",
                    "def"
                }
            };

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
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(2, "IListStringTyped");
            TypeWithLists obj = new TypeWithLists
            {
                IListStringTyped = new List<string>
                {
                    "abc",
                    "def"
                }
            };

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
        public void RoundTripTypedList_Int32()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(1, "ListInt32");
            TypeWithLists obj = new TypeWithLists
            {
                ListInt32 = new List<int>
                {
                    123,
                    456
                }
            };

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
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(2, "IListInt32Typed");
            TypeWithLists obj = new TypeWithLists
            {
                IListInt32Typed = new List<int>
                {
                    123,
                    456
                }
            };

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

        public class NastyType
        {
            
            public List<List<int>> JaggedList { get; set; }

            public List<int[]> ListOfArray{ get; set; }

            public int[,] MultiDimArray { get; set; }

            public int[][] JaggedArray { get; set; }

            public List<int>[] ArrayOfList{ get; set; }

            public List<int> BasicList { get; set; }
            public List<int> BasicListField;

            public int[] BasicArray{ get; set; }
            public int[] BasicArrayField;

            public byte[] Blob { get; set; }
            public byte[] BlobField;

            public List<byte[]> Blobs{ get; set; }

            public List<byte[]> BlobsField;
        }
        [Fact]
        public void JaggedListShouldThrow()
        {
            try {
                var model = RuntimeTypeModel.Create();
                model.Add(typeof(NastyType), true).Add(1, nameof(NastyType.JaggedList));
                model.CompileInPlace();
                Assert.Equal(42, 0); // fail
            } catch(NotSupportedException) {  }
        }
        [Fact]
        public void ListOfArrayShouldThrow()
        {
            try {
                var model = RuntimeTypeModel.Create();
                model.Add(typeof(NastyType), true).Add(1, nameof(NastyType.ListOfArray));
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
                model.Add(typeof(NastyType), true).Add(1, nameof(NastyType.MultiDimArray));
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
                model.Add(typeof(NastyType), true).Add(1, nameof(NastyType.JaggedArray));
                model.CompileInPlace();
                Assert.Equal(42, 0); // fail
            }
            catch (NotSupportedException) { }
        }
        [Fact]
        public void ArrayOfListShouldThrow()
        {
            Assert.Throws<NotSupportedException>(() =>
            {
                var model = RuntimeTypeModel.Create();
                model.Add(typeof(NastyType), true).Add(1, nameof(NastyType.ArrayOfList));
                model.CompileInPlace();
                Assert.Equal(42, 0); // fail
            });
        }
        [Fact]
        public void BasicListIsFine()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(NastyType), true).Add(1, nameof(NastyType.BasicList));
            model.CompileInPlace();
        }
        [Fact]
        public void BasicArrayIsFine()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(NastyType), true).Add(1, nameof(NastyType.BasicArray));
            model.CompileInPlace();
        }
        [Fact]
        public void BlobIsFine()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(NastyType), true).Add(1, nameof(NastyType.Blob));
            model.CompileInPlace();
        }
        [Fact]
        public void BlobsAreFine()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(NastyType), true).Add(1, nameof(NastyType.Blobs));
            model.CompileInPlace();
        }
        [Fact]
        public void PEVerifyArraysAndLists()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(NastyType), true)
                .Add(1, nameof(NastyType.Blobs))
                .Add(2, nameof(NastyType.Blob))
                .Add(3, nameof(NastyType.BasicArray))
                .Add(4, nameof(NastyType.BasicList))

                .Add(5, nameof(NastyType.BlobsField))
                .Add(6, nameof(NastyType.BlobField))
                .Add(7, nameof(NastyType.BasicArrayField))
                .Add(8, nameof(NastyType.BasicListField))
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

            var clone = RoundTrip(model, orig, "Runtime", out int len);
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

            var clone = RoundTrip(model, orig, "Runtime", out int len);
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

#pragma warning disable IDE0060
        static void CheckExpectedListContents(PackedData data, string text)
#pragma warning restore IDE0060
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

            var clone = RoundTrip(model, orig, "Runtime", out int len);
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
                using MemoryStream ms = new MemoryStream();
                model.Serialize(ms, orig);
                length = (int)ms.Length;
                ms.Position = 0;
#pragma warning disable CS0618
                return (PackedData)model.Deserialize(ms, null, typeof(PackedData));
#pragma warning restore CS0618
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(scenario + ": " + ex.Message, ex);
            }
        }
        static RuntimeTypeModel CreateModel()
        {
            var model = RuntimeTypeModel.Create();
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

            var clone = RoundTrip(model, orig, "Runtime", out int len);
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

            var clone = RoundTrip(model, orig, "Runtime", out int len);
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

#pragma warning disable IDE0060
        static void CheckExpectedListContents(PackedData data, string text)
#pragma warning restore IDE0060
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

        public PackedArrays(ITestOutputHelper log)
            => _log = log;
        private readonly ITestOutputHelper _log;

        [Fact]
        public void TestThreeItemsRoundTrip()
        {
            var model = CreateModel();

            var orig = new PackedData { ArrayInt32 = new int[] { 3, 5, 7 }, ArraySingle = new float[] { 3F, 5F, 7F }, ArrayDouble = new double[] { 3D, 5D, 7F } };
            CheckExpectedListContents(orig, "Original");

            var clone = RoundTrip(model, orig, "Runtime", out int len);
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
        PackedData RoundTrip(TypeModel model, PackedData orig, string scenario, out int length)
        {
            try
            {
                using MemoryStream ms = new MemoryStream();
                model.Serialize(ms, orig);
                _log?.WriteLine($"r64: {orig.ArrayDouble?.Length}, i32: {orig.ArrayInt32?.Length}, {orig.ArraySingle?.Length}");
                _log?.WriteLine($"{ms.Length} bytes: {BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length)}");
                length = (int)ms.Length;
                ms.Position = 0;
#pragma warning disable CS0618
                return (PackedData)model.Deserialize(ms, null, typeof(PackedData));
#pragma warning restore CS0618
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(scenario + ": " + ex.Message, ex);
            }
        }
        static RuntimeTypeModel CreateModel()
        {
            var model = RuntimeTypeModel.Create();
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
