#if !NO_INTERNAL_CONTEXT
using System.IO;
using Xunit;
using System.Reflection;
using ProtoBuf.Serializers;
using ProtoBuf.Compiler;
using System;
using ProtoBuf.Meta;
using ProtoBuf.Internal;
using ProtoBuf.Internal.Serializers;

namespace ProtoBuf.unittest.Meta
{
    public struct CustomerStruct
    {
        public int Id { get; set; }
        public string Name;
    }

    public class TestCustomerStruct
    {
        public enum Foo
        {
            A, B, C
        }

        static RuntimeTypeModel CreateEnumModel()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add(typeof(Foo), true);
            return model;
        }
        [Fact]
        public void TestEnumTopLevel_Runtime() => TestEnumModel(CreateEnumModel());

        [Fact]
        public void TestEnumTopLevel_CompileInPlace()
        {
            var model = CreateEnumModel();
            model.CompileInPlace();
            TestEnumModel(model);
        }

        [Fact]
        public void TestEnumTopLevel_Compile() => TestEnumModel(CreateEnumModel().Compile());

        [Fact]
        public void TestEnumTopLevel_CompileFull()
        {
            var model = CreateEnumModel();
            var compiled = model.Compile("TestEnumTopLevel_CompileFull", "TestEnumTopLevel_CompileFull.dll");
            PEVerify.Verify("TestEnumTopLevel_CompileFull.dll");
            TestEnumModel(compiled);
        }

        static void TestEnumModel(TypeModel model)
        {
            var obj = model.GetSerializerCore<Foo>(default);
            Assert.NotNull(obj);

            Assert.True(obj.Features.GetCategory() == SerializerFeatures.CategoryScalar, "should be a scalar serializer; is " + obj.GetType().NormalizeName());

            using var ms = new MemoryStream();
            Assert.False(model.CanSerializeContractType(typeof(Foo)), "should not be a contract type");
            Assert.True(model.CanSerializeBasicType(typeof(Foo)), "should be a basic type");
            model.Serialize(ms, Foo.B);
            
            var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            Assert.Equal("08-01", hex);
            ms.Position = 0;
            var val = model.Deserialize<Foo>(ms);
            Assert.Equal(Foo.B, val);

            val = model.DeepClone(Foo.B);
            Assert.Equal(Foo.B, val);
        }

        [Fact]
        public void RunStructDesrializerForEmptyStream()
        {
            var model = ProtoBuf.Meta.RuntimeTypeModel.Create();
            var head = TypeSerializer.Create(typeof(CustomerStruct),
                new int[] { 1, 2 },
                new IRuntimeProtoSerializerNode[] {
                    new PropertyDecorator(typeof(CustomerStruct), typeof(CustomerStruct).GetProperty(nameof(CustomerStruct.Id)), new TagDecorator(1, WireType.Varint, false, Int32Serializer.Instance)),
                    new FieldDecorator(typeof(CustomerStruct), typeof(CustomerStruct).GetField(nameof(CustomerStruct.Name)), new TagDecorator(2, WireType.String, false, StringSerializer.Instance))
                }, null, false, true, true, null, null, null, null, SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage);
            var deser = CompilerContext.BuildDeserializer<CustomerStruct>(model.Scope, head, model);

            var state = ProtoReader.State.Create(Stream.Null, null, null);
            try
            {
                var result = deser(ref state, default);
                Assert.IsType<CustomerStruct>(result);
            }
            finally
            {
                state.Dispose();
            }

            state = ProtoReader.State.Create(Stream.Null, null, null);
            try
            {
                CustomerStruct before = new CustomerStruct { Id = 123, Name = "abc" };
                CustomerStruct after = (CustomerStruct)deser(ref state, before);
                Assert.Equal(before.Id, after.Id);
                Assert.Equal(before.Name, after.Name);
            }
            finally
            {
                state.Dispose();
            }
        }
        [Fact]
        public void GenerateTypeSerializer()
        {
            var model = ProtoBuf.Meta.RuntimeTypeModel.Create();
            var head = TypeSerializer.Create(typeof(CustomerStruct),
                new int[] { 1, 2 },
                new IRuntimeProtoSerializerNode[] {
                    new PropertyDecorator(typeof(CustomerStruct), typeof(CustomerStruct).GetProperty(nameof(CustomerStruct.Id)), new TagDecorator(1, WireType.Varint,false, Int32Serializer.Instance)),
                    new FieldDecorator(typeof(CustomerStruct), typeof(CustomerStruct).GetField(nameof(CustomerStruct.Name)), new TagDecorator(2, WireType.String,false, StringSerializer.Instance))
                }, null, false, true, true, null, null, null, null, SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage);
            var ser = CompilerContext.BuildSerializer<CustomerStruct>(model.Scope, head, model);
            var deser = CompilerContext.BuildDeserializer<CustomerStruct>(model.Scope, head, model);
            CustomerStruct cs1 = new CustomerStruct { Id = 123, Name = "Fred" };
            using MemoryStream ms = new MemoryStream();
            var writeState = ProtoWriter.State.Create(ms, null, null);
            try
            {
                ser(ref writeState, cs1);
                writeState.Close();
            }
            finally
            {
                writeState.Dispose();
            }
            byte[] blob = ms.ToArray();
            ms.Position = 0;
            var state = ProtoReader.State.Create(ms, null, null);
            try
            {
                CustomerStruct? cst = (CustomerStruct?)deser(ref state, default);
                Assert.True(cst.HasValue);
                CustomerStruct cs2 = cst.Value;
                Assert.Equal(cs1.Id, cs2.Id);
                Assert.Equal(cs1.Name, cs2.Name);
            }
            finally
            {
                state.Dispose();
            }
        }
    }
}
#endif