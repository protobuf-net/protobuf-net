using ProtoBuf.Meta;
using System;
using System.IO;
using Xunit;

namespace ProtoBuf.Test.Issues
{
    public class Issue479
    {
        [ProtoContract]
        public class Person
        {
            [ProtoMember(1)] public int Id { get; set; }
            [ProtoMember(2)] public string Name { get; set; }
            [ProtoMember(3)] public Address Address { get; set; }
        }

        [ProtoContract]
        public class Address
        {
            [ProtoMember(1)] public string Line1 { get; set; }
            [ProtoMember(2)] public string Line2 { get; set; }
        }

        [Fact]
        public void Execute()
        {
            var bytes = new byte[]
              {
                0xF2, 0xF2, 0xF2, 0x02,
                0xF2, 0xFF, 0xFF, 0xFF,
                0xFF, 0xF2, 0xF2, 0xF2,
                0xF2, 0x01, 0xF2, 0x00
              };

            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                var reader = ProtoReader.State.Create(new ReadOnlyMemory<byte>(bytes), RuntimeTypeModel.Default);
                try
                {
                    int count = 0, field;
                    while ((field = reader.ReadFieldHeader()) > 0)
                    {
                        Console.WriteLine(field);
                        reader.SkipField();

                        Assert.True(++count <= 20, "too many times");
                    }
                }
                finally
                {
                    reader.Dispose();
                }
            });
            Assert.Equal("Invalid length: -944124693168783374", ex.Message);

            ex = Assert.Throws<InvalidOperationException>(() =>
            {
                Serializer.Deserialize<Person>(new ReadOnlyMemory<byte>(bytes));
            });
            Assert.Equal("Invalid length: -944124693168783374", ex.Message);

            ex = Assert.Throws<InvalidOperationException>(() =>
            {
                Serializer.Deserialize<Person>(new MemoryStream(bytes));
            });
            Assert.Equal("Invalid length: -944124693168783374", ex.Message);
        }
    }
}
