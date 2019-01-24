using System.Collections.Generic;
using ProtoBuf.Meta;
using Xunit;

namespace ProtoBuf.Issues
{
    public class Issue297
    {
        [ProtoContract(IgnoreListHandling = true)]
        public class Inner : List<int>
        {
            [ProtoMember(123)]
            public int InnerMember { get; set; }
        }

        [ProtoContract]
        public class Outer
        {
            public Inner OuterMember { get; set; }
        }

        [Fact]
        public void AddFieldRespectsIgnoreListHandling()
        {
            var typeModel = TypeModel.Create();

            typeModel.Add(typeof(Outer), true)
                .AddField(456, nameof(Outer.OuterMember));

            var actual = typeModel.GetSchema(typeof(Outer));
            var expected = @"syntax = ""proto2"";
package ProtoBuf.Issues;

message Inner {
   optional int32 InnerMember = 123 [default = 0];
}
message Outer {
   optional Inner OuterMember = 456;
}
";

            Assert.Equal(expected, actual);
        }
    }
}
