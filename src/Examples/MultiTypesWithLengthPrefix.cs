using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf;
using Xunit.Abstractions;

namespace Examples
{
    public class MultiTypesWithLengthPrefix
    {
        public ITestOutputHelper Output { get; }
        public MultiTypesWithLengthPrefix(ITestOutputHelper output)
        {
            Output = output;
        }

        [Fact]
        public void TestRoundTripMultiTypes()
        {
            using MemoryStream ms = new MemoryStream();
            WriteNext(ms, 123);
            Assert.Equal(4, ms.Position);
            WriteNext(ms, new Person { Name = "Fred" });
            Assert.Equal(12, ms.Position);
            WriteNext(ms, "abc");
            Assert.Equal(19, ms.Position);
            WriteNext(ms, new Address { Line1 = "12 Lamb Lane" });
            Assert.Equal(35, ms.Position);

            var hex = BitConverter.ToString(
                ms.GetBuffer(), 0, (int)ms.Length);
            Assert.Equal("0A-02-08-7B-12-06-0A-04-46-72-65-64-1A-05-0A-03-61-62-63-22-0E-0A-0C-31-32-20-4C-61-6D-62-20-4C-61-6E-65", hex);
            Output.WriteLine(hex);

            ms.Position = 0;

            Assert.Equal(123, ReadNext(ms));
            Assert.Equal(4, ms.Position);
            Assert.Equal("Fred", ((Person)ReadNext(ms)).Name);
            Assert.Equal(12, ms.Position);
            Assert.Equal("abc", ReadNext(ms));
            Assert.Equal(19, ms.Position);
            Assert.Equal("12 Lamb Lane", ((Address)ReadNext(ms)).Line1);
            Assert.Equal(35, ms.Position);
            Assert.Null(ReadNext(ms));
        }
        private static readonly IDictionary<int, Type> typeLookup = new Dictionary<int, Type>
        {
            {1, typeof(int)}, {2, typeof(Person)}, {3, typeof(string)}, {4, typeof(Address)}
        };

        private static void WriteNext(Stream stream, object obj)
        {
            Type type = obj.GetType();
            int field = typeLookup.Single(pair => pair.Value == type).Key;
#pragma warning disable CS0618
            Serializer.NonGeneric.SerializeWithLengthPrefix(stream, obj, PrefixStyle.Base128, field);
#pragma warning restore CS0618
        }
        private static object ReadNext(Stream stream)
        {
#pragma warning disable CS0618
            if (Serializer.NonGeneric.TryDeserializeWithLengthPrefix(stream, PrefixStyle.Base128, field => typeLookup[field], out object obj))
#pragma warning restore CS0618
            {
                return obj;
            }
            return null;
        }
    }
    [ProtoContract]
    internal class Person
    {
        [ProtoMember(1)]
        public string Name { get; set; }
        public override string ToString() { return "Person: " + Name; }
    }
    [ProtoContract]
    internal class Address
    {
        [ProtoMember(1)]
        public string Line1 { get; set; }
        public override string ToString() { return "Address: " + Line1; }
    }
}
