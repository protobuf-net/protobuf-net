using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace ProtoBuf.Issues
{
    public class TypeSerializedHow
    {
        [Fact]
        public void TypeRoundtrips()
        {
            var obj = new ModelWithTypeMember
            {
                Id = 123,
                SomeType = typeof(Uri) // arbitrary
            };

            var clone = Serializer.DeepClone(obj);
            Assert.NotSame(obj, clone);
            Assert.Equal(123, clone.Id);
            Assert.Same(typeof(Uri), clone.SomeType);
        }

        [Fact]
        public void ProveTypeEquivalence()
        {
            var obj = new ModelWithTypeMember
            {
                Id = 123,
                SomeType = typeof(Uri) // arbitrary
            };

            var clone = Serializer.ChangeType<ModelWithTypeMember, EquivModel>(obj);
            Assert.Equal(123, clone.Id);
            Assert.Equal(typeof(Uri).AssemblyQualifiedName, clone.SomeType);
        }

        [Fact]
        public void TypeGeneratesProto()
        {
            var proto = Serializer.GetProto<ModelWithTypeMember>();
            Assert.Equal(@"syntax = ""proto2"";
package ProtoBuf.Issues;

message ModelWithTypeMember {
   optional int32 Id = 1 [default = 0];
   optional string SomeType = 2;
}
", proto);
        }

        [Fact]
        public void GeneratesProto()
        {
            var proto = Serializer.GetProto<ProtoDataEntity>();
            var expected = @"syntax = ""proto2"";
package ProtoBuf.Issues;

            //a demo proto data entity
            message ProtoDataEntity {
                optional int32 Id = 1[default = 0];//a primary key
                optional string Name = 2;//the name property
            }
            ";

            Assert.Equal(expected, proto);
        }

        [ProtoContract]
        public class ModelWithTypeMember
        {
            [ProtoMember(1)]
            public int Id { get; set; }
            [ProtoMember(2)]
            public Type SomeType { get; set; }
        }
        [ProtoContract]
        public class EquivModel
        {
            [ProtoMember(1)]
            public int Id { get; set; }
            [ProtoMember(2)]
            public string SomeType { get; set; }
        }

        [ProtoDescription("a demo proto data entity")]
        [ProtoContract]
        public class ProtoDataEntity
        {
            [ProtoDescription("a primary key")]
            [ProtoMember(1)]
            public int Id { set; get; }

            [ProtoDescription("the name property")]
            [ProtoMember(2)]
            public string Name { set; get; }
        }
    }
}
