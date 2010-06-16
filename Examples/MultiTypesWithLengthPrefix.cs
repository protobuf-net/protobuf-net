using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf;

namespace Examples
{
    class MultiTypesWithLengthPrefix
    {
        public void TestRoundTripMultiTypes()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                WriteNext(ms, 123);
                WriteNext(ms, new Person { Name = "Fred" });
                WriteNext(ms, "abc");
                WriteNext(ms, new Address { Line1 = "12 Lamb Lane" });

                ms.Position = 0;

                Assert.AreEqual(123, ReadNext(ms));
                Assert.AreEqual("Fred", ((Person)ReadNext(ms)).Name);
                Assert.AreEqual("abc", ReadNext(ms));
                Assert.AreEqual("12 Lamb Lane", ((Address)ReadNext(ms)).Line1);
                Assert.IsNull(ReadNext(ms));
            }
        }
        static readonly IDictionary<int, Type> typeLookup = new Dictionary<int, Type>
    {
        {1, typeof(int)}, {2, typeof(Person)}, {3, typeof(string)}, {4, typeof(Address)}
    };
        static void WriteNext(Stream stream, object obj)
        {
            Type type = obj.GetType();
            int field = typeLookup.Single(pair => pair.Value == type).Key;
            Serializer.NonGeneric.SerializeWithLengthPrefix(stream, obj, PrefixStyle.Base128, field);
        }
        static object ReadNext(Stream stream)
        {
            object obj;
            if (Serializer.NonGeneric.TryDeserializeWithLengthPrefix(stream, PrefixStyle.Base128, field => typeLookup[field], out obj))
            {
                return obj;
            }
            return null;
        }
    }
    [ProtoContract]
    class Person
    {
        [ProtoMember(1)]
        public string Name { get; set; }
        public override string ToString() { return "Person: " + Name; }
    }
    [ProtoContract]
    class Address
    {
        [ProtoMember(1)]
        public string Line1 { get; set; }
        public override string ToString() { return "Address: " + Line1; }
    }
}
