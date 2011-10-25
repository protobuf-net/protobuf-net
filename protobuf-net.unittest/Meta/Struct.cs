using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf.Serializers;
using ProtoBuf.unittest.Serializers;
using ProtoBuf.Compiler;
using System.IO;
namespace ProtoBuf.unittest.Meta
{
    public struct CustomerStruct
    {
        private int id;
        public int Id { get { return id; } set { id = value; } }
        public string Name;
    }
    [TestFixture]
    public class TestCustomerStruct
    {
        [Test]
        public void RunStructDesrializerForEmptyStream()
        {
            var head = new TypeSerializer(typeof(CustomerStruct),
                new int[] { 1, 2 },
                new IProtoSerializer[] {
                    new PropertyDecorator(typeof(CustomerStruct), typeof(CustomerStruct).GetProperty("Id"), new TagDecorator(1, WireType.Variant, false, new Int32Serializer())),
                    new FieldDecorator(typeof(CustomerStruct), typeof(CustomerStruct).GetField("Name"), new TagDecorator(2, WireType.String, false, new StringSerializer()))
                }, null, false, true, null, null, null);
            var deser = CompilerContext.BuildDeserializer(head);

            using (var reader = new ProtoReader(Stream.Null, null, null))
            {
                Assert.IsInstanceOfType(typeof(CustomerStruct), deser(null, reader));
            }
            using (var reader = new ProtoReader(Stream.Null, null, null))
            {
                CustomerStruct before = new CustomerStruct { Id = 123, Name = "abc" };
                CustomerStruct after = (CustomerStruct)deser(before, reader);
                Assert.AreEqual(before.Id, after.Id);
                Assert.AreEqual(before.Name, after.Name);
            }
        }
        [Test]
        public void GenerateTypeSerializer()
        {
            var head = new TypeSerializer(typeof(CustomerStruct),
                new int[] { 1, 2 },
                new IProtoSerializer[] {
                    new PropertyDecorator(typeof(CustomerStruct), typeof(CustomerStruct).GetProperty("Id"), new TagDecorator(1, WireType.Variant,false,  new Int32Serializer())),
                    new FieldDecorator(typeof(CustomerStruct), typeof(CustomerStruct).GetField("Name"), new TagDecorator(2, WireType.String,false,  new StringSerializer()))
                }, null, false, true, null, null, null);
            var ser = CompilerContext.BuildSerializer(head);
            var deser = CompilerContext.BuildDeserializer(head);
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
                    Assert.IsTrue(cst.HasValue);
                    CustomerStruct cs2 = cst.Value;
                    Assert.AreEqual(cs1.Id, cs2.Id);
                    Assert.AreEqual(cs1.Name, cs2.Name);
                }
            }
        }
    }
}
