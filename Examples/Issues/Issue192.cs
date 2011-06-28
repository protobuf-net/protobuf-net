using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf;
using System.IO;

namespace Examples.Issues
{
    [TestFixture]
    public class Issue192
    {
        [ProtoContract]
        class SomeType { }
        [ProtoContract]
        class Wrapper
        {
            [ProtoMember(1)]
            public List<SomeType>[] List { get; set; }
        }
        // the important thing is that this error is identical to the one from SerializeWrappedDeepList
        [Test, ExpectedException(typeof(NotSupportedException), ExpectedMessage = "Nested or jagged lists and arrays are not supported")]
        public void SerializeDeepList()
        {
            var list = new List<SomeType>[] { new List<SomeType> { new SomeType() }, new List<SomeType> { new SomeType() } };
            Serializer.Serialize(Stream.Null, list);
        }
        [Test, ExpectedException(typeof(NotSupportedException), ExpectedMessage = "Nested or jagged lists and arrays are not supported")]
        public void DeserializeDeepList()
        {
            Serializer.Deserialize<List<SomeType>[]>(Stream.Null);
        }
        [Test, ExpectedException(typeof(NotSupportedException), ExpectedMessage = "Nested or jagged lists and arrays are not supported")]
        public void SerializeWrappedDeepList()
        {
            var wrapped = new Wrapper();
            var clone = Serializer.DeepClone(wrapped);
        }

    }
}
