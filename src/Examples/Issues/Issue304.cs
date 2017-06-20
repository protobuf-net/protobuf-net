using System.ComponentModel;
using Xunit;
using ProtoBuf;

namespace Examples.Issues
{

    public class Issue304
    {
        [Fact]
        public void DefaultValuesForBoolMustBeLowerCase()
        {
            Assert.Equal(@"syntax = ""proto2"";
package Examples.Issues;

message Foo {
   optional bool Bar = 1 [default = true];
}
",

        Serializer.GetProto<Foo>()

        );
        }
        [ProtoContract]
        public class Foo
        {
            [DefaultValue(true), ProtoMember(1)]
            public bool Bar { get; set; }
        }
    }
}
