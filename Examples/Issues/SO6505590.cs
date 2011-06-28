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
    public class SO6505590
    {
        public class NoRelationship {}

        [ProtoContract]
        public class ParentA { }
        public class ChildA : ParentA { }


        [ProtoContract]
        public class ParentB { }
        [ProtoContract]
        public class ChildB : ParentB { }


        [ProtoContract, ProtoInclude(1, typeof(ChildC))]
        public class ParentC { }
        [ProtoContract]
        public class ChildC : ParentC { }

        [Test, ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Type is not expected, and no contract can be inferred: Examples.Issues.SO6505590+NoRelationship")]
        public void SerializeTypeWithNoMarkersShouldFail()
        {
            var obj = new NoRelationship();
            Serializer.Serialize(Stream.Null, obj);
        }
        [Test, ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Type is not expected, and no contract can be inferred: Examples.Issues.SO6505590+NoRelationship")]
        public void DeserializeTypeWithNoMarkersShouldFail()
        {
            Serializer.Deserialize<NoRelationship>(Stream.Null);
        }

        [Test]
        public void SerializeParentWithUnmarkedChildShouldWork()
        {
            var obj = new ParentA();
            Serializer.Serialize(Stream.Null, obj);
        }
        [Test]
        public void DeserializeParentWithUnmarkedChildShouldWork()
        {
            Assert.AreEqual(typeof(ParentA), Serializer.Deserialize<ParentA>(Stream.Null).GetType());
        }

        [Test, ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Type is not expected, and no contract can be inferred: Examples.Issues.SO6505590+ChildA")]
        public void SerializeUnmarkedChildShouldFail()
        {
            var obj = new ChildA();
            Serializer.Serialize(Stream.Null, obj);
        }
        [Test, ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Type is not expected, and no contract can be inferred: Examples.Issues.SO6505590+ChildA")]
        public void DeserializeUnmarkedChildShouldFail()
        {
            Serializer.Deserialize<ChildA>(Stream.Null);
        }


        [Test]
        public void SerializeParentWithUnexpectedChildShouldWork()
        {
            var obj = new ParentB();
            Serializer.Serialize(Stream.Null, obj);
        }
        [Test]
        public void DeserializeParentWithUnexpectedChildShouldWork()
        {
            Assert.AreEqual(typeof(ParentB), Serializer.Deserialize<ParentB>(Stream.Null).GetType());
        }

        [Test]
        public void SerializeParentWithExpectedChildShouldWork()
        {
            var obj = new ParentC();
            Serializer.Serialize(Stream.Null, obj);
        }
        [Test]
        public void DeserializeParentWithExpectedChildShouldWork()
        {
            Assert.AreEqual(typeof(ParentC), Serializer.Deserialize<ParentC>(Stream.Null).GetType());
        }

        [Test]
        public void SerializeExpectedChildShouldWork()
        {
            var obj = new ChildC();
            Assert.AreEqual(typeof(ChildC), Serializer.DeepClone<ParentC>(obj).GetType());
        }
    }
}
