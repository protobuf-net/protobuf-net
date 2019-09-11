#if !NO_INTERNAL_CONTEXT
using System.IO;
using Xunit;
using System.Reflection;
using ProtoBuf.Serializers;
using ProtoBuf.Compiler;

namespace ProtoBuf.unittest.Meta
{
    public struct CustomerStruct
    {
        public int Id { get; set; }
        public string Name;
    }

    public class TestCustomerStruct
    {
        [Fact]
        public void RunStructDesrializerForEmptyStream()
        {
            var model = ProtoBuf.Meta.RuntimeTypeModel.Create();
            var head = new TypeSerializer(typeof(CustomerStruct),
                new int[] { 1, 2 },
                new IProtoSerializer[] {
                    new PropertyDecorator(typeof(CustomerStruct), typeof(CustomerStruct).GetProperty("Id"), new TagDecorator(1, WireType.Variant, false, PrimitiveSerializer<Int32Serializer>.Singleton)),
                    new FieldDecorator(typeof(CustomerStruct), typeof(CustomerStruct).GetField("Name"), new TagDecorator(2, WireType.String, false, PrimitiveSerializer<StringSerializer>.Singleton))
                }, null, false, true, null, null, null);
            var deser = CompilerContext.BuildDeserializer(head, model);

            using (var reader = ProtoReader.Create(out var state, Stream.Null, null, null))
            {
                Assert.IsType<CustomerStruct>(deser(reader, ref state, null));
            }
            using (var reader = ProtoReader.Create(out var state, Stream.Null, null, null))
            {
                CustomerStruct before = new CustomerStruct { Id = 123, Name = "abc" };
                CustomerStruct after = (CustomerStruct)deser(reader, ref state, before);
                Assert.Equal(before.Id, after.Id);
                Assert.Equal(before.Name, after.Name);
            }
        }
        [Fact]
        public void GenerateTypeSerializer()
        {
            var model = ProtoBuf.Meta.RuntimeTypeModel.Create();
            var head = new TypeSerializer(typeof(CustomerStruct),
                new int[] { 1, 2 },
                new IProtoSerializer[] {
                    new PropertyDecorator(typeof(CustomerStruct), typeof(CustomerStruct).GetProperty("Id"), new TagDecorator(1, WireType.Variant,false,  PrimitiveSerializer<Int32Serializer>.Singleton)),
                    new FieldDecorator(typeof(CustomerStruct), typeof(CustomerStruct).GetField("Name"), new TagDecorator(2, WireType.String,false,  PrimitiveSerializer<StringSerializer>.Singleton))
                }, null, false, true, null, null, null);
            var ser = CompilerContext.BuildSerializer(head, model);
            var deser = CompilerContext.BuildDeserializer(head, model);
            CustomerStruct cs1 = new CustomerStruct { Id = 123, Name = "Fred" };
            using (MemoryStream ms = new MemoryStream())
            {
                using (ProtoWriter writer = ProtoWriter.Create(out var state, ms, null, null))
                {
                    ser(writer, ref state, cs1);
                    writer.Close(ref state);
                }
                byte[] blob = ms.ToArray();
                ms.Position = 0;
                using (ProtoReader reader = ProtoReader.Create(out var state, ms, null, null))
                {
                    CustomerStruct? cst = (CustomerStruct?)deser(reader, ref state, null);
                    Assert.True(cst.HasValue);
                    CustomerStruct cs2 = cst.Value;
                    Assert.Equal(cs1.Id, cs2.Id);
                    Assert.Equal(cs1.Name, cs2.Name);
                }
            }
        }
    }
}
#endif