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
        public void GenerateTypeSerializer()
        {
            var head = new TypeSerializer(typeof(CustomerStruct),
                new int[] { 1, 2 },
                new IProtoSerializer[] {
                    new PropertyDecorator(typeof(CustomerStruct).GetProperty("Id"), new TagDecorator(1, WireType.Variant, new Int32Serializer())),
                    new FieldDecorator(typeof(CustomerStruct).GetField("Name"), new TagDecorator(2, WireType.String, new StringSerializer()))
                });
            var ser = CompilerContext.BuildSerializer(head);
            object obj = new CustomerStruct { Id = 123, Name = "Fred" };
            using (MemoryStream ms = new MemoryStream())
            {
                using (ProtoWriter writer = new ProtoWriter(ms, null))
                {
                    ser(obj, writer);
                }
                byte[] blob = ms.ToArray();
            }
        }
    }
}
