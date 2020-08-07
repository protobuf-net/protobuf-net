using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf;
using System.IO;

namespace Examples.Issues
{
    
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

        [Fact]
        public void SerializeTypeWithNoMarkersShouldFail()
        {
            Program.ExpectFailure<InvalidOperationException>(() =>
            {
                var obj = new NoRelationship();
                Serializer.Serialize(Stream.Null, obj);
            }, "Type is not expected, and no contract can be inferred: Examples.Issues.SO6505590+NoRelationship");
        }
        [Fact]
        public void DeserializeTypeWithNoMarkersShouldFail()
        {
            Program.ExpectFailure<InvalidOperationException>(() =>
            {
                Serializer.Deserialize<NoRelationship>(Stream.Null);
            }, "Type is not expected, and no contract can be inferred: Examples.Issues.SO6505590+NoRelationship");
        }

        [Fact]
        public void SerializeParentWithUnmarkedChildShouldWork()
        {
            var obj = new ParentA();
            Serializer.Serialize(Stream.Null, obj);
        }
        [Fact]
        public void DeserializeParentWithUnmarkedChildShouldWork()
        {
            Assert.Equal(typeof(ParentA), Serializer.Deserialize<ParentA>(Stream.Null).GetType());
        }

        [Fact]
        public void SerializeUnmarkedChildShouldFail()
        {
            Program.ExpectFailure<InvalidOperationException>(() =>
            {
                var obj = new ChildA();
                Serializer.Serialize(Stream.Null, obj);
            }, "Unexpected sub-type: Examples.Issues.SO6505590+ChildA");
        }
        [Fact]
        public void DeserializeUnmarkedChildShouldFail()
        {
            Program.ExpectFailure<InvalidOperationException>(() =>
            {
                Serializer.Deserialize<ChildA>(Stream.Null);
            }, "Type is not expected, and no contract can be inferred: Examples.Issues.SO6505590+ChildA");
        }


        [Fact]
        public void SerializeParentWithUnexpectedChildShouldWork()
        {
            var obj = new ParentB();
            Serializer.Serialize(Stream.Null, obj);
        }
        [Fact]
        public void DeserializeParentWithUnexpectedChildShouldWork()
        {
            Assert.Equal(typeof(ParentB), Serializer.Deserialize<ParentB>(Stream.Null).GetType());
        }

        [Fact]
        public void SerializeParentWithExpectedChildShouldWork()
        {
            var obj = new ParentC();
            Serializer.Serialize(Stream.Null, obj);
        }
        [Fact]
        public void DeserializeParentWithExpectedChildShouldWork()
        {
            Assert.Equal(typeof(ParentC), Serializer.Deserialize<ParentC>(Stream.Null).GetType());
        }

        [Fact]
        public void SerializeExpectedChildShouldWork()
        {
            var obj = new ChildC();
            Assert.Equal(typeof(ChildC), Serializer.DeepClone<ParentC>(obj).GetType());
        }
    }
}
