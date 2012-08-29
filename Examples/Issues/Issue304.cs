using System.ComponentModel;
using NUnit.Framework;
using ProtoBuf;

namespace Examples.Issues
{
    [TestFixture]
    public class Issue304
    {
        [Test]
        public void DefaultValuesForBoolMustBeLowerCase()
        {
            Assert.AreEqual(@"package Examples.Issues;

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
