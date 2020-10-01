using Examples;
using ProtoBuf.Meta;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace ProtoBuf.Issues
{
    public class Issue713
    {
        [Fact]
        public void CanSerializeNullableInt32Array_NoNulls()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            Test(model);
            model.CompileInPlace();
            Test(model);

            var dll = model.Compile(nameof(CanSerializeNullableInt32Array_NoNulls), nameof(CanSerializeNullableInt32Array_NoNulls) + ".dll");
            PEVerify.AssertValid(nameof(CanSerializeNullableInt32Array_NoNulls) + ".dll");
            Test(dll);

            Test(model.Compile());


            static void Test(TypeModel model)
            {
                var orig = new WithInt32 { Values = new int?[] { 0, 1, -2, 3 } };
                using var ms = new MemoryStream();
                model.Serialize(ms, orig);
                var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
                ms.Position = 0;
                var clone = model.Deserialize<WithInt32>(ms);
                Assert.True(Enumerable.SequenceEqual(orig.Values, clone.Values));
            }
        }

        [Fact]
        public void CanNotSerializeNullableInt32Array_WithNulls()
        {
            var orig = new WithInt32 { Values = new int?[] { 0, null, 1, -2, 3, null } };
            using var ms = new MemoryStream();
            var ex = Assert.Throws<NullReferenceException>(() => Serializer.Serialize(ms, orig));
            Assert.Equal("An element of type System.Nullable`1[System.Int32] was null; this might be as contents in a list/array", ex.Message);
        }

        [ProtoContract]
        public class WithInt32
        {
            [ProtoMember(1)]
            public int?[] Values { get; set; }
        }

        [Fact]
        public void CanSerializeNullableSomeEnumArray_NoNulls()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            Test(model);
            model.CompileInPlace();
            Test(model);

            var dll = model.Compile(nameof(CanSerializeNullableSomeEnumArray_NoNulls), nameof(CanSerializeNullableSomeEnumArray_NoNulls) + ".dll");
            PEVerify.AssertValid(nameof(CanSerializeNullableInt32Array_NoNulls) + ".dll");
            Test(dll);

            //Test(model.Compile());

            static void Test(TypeModel model)
            {
                var orig = new WithSomeEnum { Values = new SomeEnum?[] { SomeEnum.A, SomeEnum.B, SomeEnum.C, SomeEnum.D } };
                using var ms = new MemoryStream();
                model.Serialize(ms, orig);
                var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
                ms.Position = 0;
                var clone = model.Deserialize<WithSomeEnum>(ms);
                Assert.True(Enumerable.SequenceEqual(orig.Values, clone.Values));
            }
        }

        [Fact]
        public void CanNotSerializeNullableSomeEnumArray_WithNulls()
        {
            var orig = new WithSomeEnum { Values = new SomeEnum?[] { SomeEnum.A, null, SomeEnum.B, SomeEnum.C, SomeEnum.D, null } };
            using var ms = new MemoryStream();
            var ex = Assert.Throws<NullReferenceException>(() => Serializer.Serialize(ms, orig));
            Assert.Equal("An element of type System.Nullable`1[ProtoBuf.Issues.Issue713+SomeEnum] was null; this might be as contents in a list/array", ex.Message);
        }

        [ProtoContract]
        public class WithSomeEnum
        {
            [ProtoMember(1)]
            public SomeEnum?[] Values { get; set; }
        }

        public enum SomeEnum
        {
            A = 0,
            B = 1,
            C = -2,
            D = 3,
        }



    }
}
