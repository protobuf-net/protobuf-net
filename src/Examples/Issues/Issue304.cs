using System.ComponentModel;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;

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

        Serializer.GetProto<Foo>(ProtoSyntax.Proto2), ignoreLineEndingDifferences: true

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
