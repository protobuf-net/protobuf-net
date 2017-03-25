#if !NO_INTERNAL_CONTEXT
using System.IO;
using Xunit;
using System.Reflection;
namespace ProtoBuf.unittest.Meta
{
    public struct CustomerStruct
    {
        private int id;
        public int Id { get { return id; } set { id = value; } }
        public string Name;
    }
    
    public class TestCustomerStruct
    {
        [Fact]
        public void RunStructDesrializerForEmptyStream()
        {
            var model = ProtoBuf.Meta.TypeModel.Create();
            var head = new TypeSerializer(model, typeof(CustomerStruct),
                new int[] { 1, 2 },
                new IProtoSerializer[] {
                    new PropertyDecorator(model, typeof(CustomerStruct), typeof(CustomerStruct).GetProperty("Id"), new TagDecorator(1, WireType.Variant, false, new Int32Serializer(model))),
                    new FieldDecorator(typeof(CustomerStruct), typeof(CustomerStruct).GetField("Name"), new TagDecorator(2, WireType.String, false, new StringSerializer(model)))
                }, null, false, true, null, null, null);
            var deser = CompilerContext.BuildDeserializer(head, model);

            using (var reader = new ProtoReader(Stream.Null, null, null))
            {
                Assert.IsType(typeof(CustomerStruct), deser(null, reader));
            }
            using (var reader = new ProtoReader(Stream.Null, null, null))
            {
                CustomerStruct before = new CustomerStruct { Id = 123, Name = "abc" };
                CustomerStruct after = (CustomerStruct)deser(before, reader);
                Assert.Equal(before.Id, after.Id);
                Assert.Equal(before.Name, after.Name);
            }
        }
        [Fact]
        public void GenerateTypeSerializer()
        {
            var model = ProtoBuf.Meta.TypeModel.Create();
            var head = new TypeSerializer(model, typeof(CustomerStruct),
                new int[] { 1, 2 },
                new IProtoSerializer[] {
                    new PropertyDecorator(model, typeof(CustomerStruct), typeof(CustomerStruct).GetProperty("Id"), new TagDecorator(1, WireType.Variant,false,  new Int32Serializer(model))),
                    new FieldDecorator(typeof(CustomerStruct), typeof(CustomerStruct).GetField("Name"), new TagDecorator(2, WireType.String,false,  new StringSerializer(model)))
                }, null, false, true, null, null, null);
            var ser = CompilerContext.BuildSerializer(head, model);
            var deser = CompilerContext.BuildDeserializer(head, model);
            CustomerStruct cs1 = new CustomerStruct { Id = 123, Name = "Fred" };
            using (MemoryStream ms = new MemoryStream())
            {
                using (ProtoWriter writer = new ProtoWriter(ms, null, null))
                {
                    ser(cs1, writer);
                }
                byte[] blob = ms.ToArray();
                ms.Position = 0;
                using (ProtoReader reader = new ProtoReader(ms, null, null))
                {
                    CustomerStruct? cst = (CustomerStruct?)deser(null, reader);
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