using System;
using ProtoBuf.Meta;
using Xunit;

namespace ProtoBuf.Test.Issues
{
#pragma warning disable CS0612 // Type or member is obsolete
    public sealed class Issue1149
    {
        [Fact]
        public void GenerateProtoWithDeprecatedOption()
        {
            const string expected = @"syntax = ""proto3"";
package DefaultPackage;

message SomeClass {
   option deprecated = true;
   int32 SomeProperty = 1;
}
enum SomeEnum {
   option deprecated = true;
   Zero = 0;
   One = 1;
   Two = 2;
}
message SomeOtherClass {
   string SomeOtherProperty = 1 [deprecated = true];
}
enum SomeOtherEnum {
   Zero = 0;
   One = 1;
   Two = 2;
   Three = 3 [deprecated = true];
}
";

            var actual = Serializer.GetProto(new SchemaGenerationOptions
            {
                Syntax = ProtoSyntax.Proto3,
                Package = "DefaultPackage",
                Types = { typeof(SomeClass), typeof(SomeOtherClass), typeof(SomeEnum), typeof(SomeOtherEnum) },
            });

            Assert.Equal(expected, actual);
        }

        [ProtoContract]
        [Obsolete]
        public class SomeClass
        {
            [ProtoMember(tag: 1)]
            public int SomeProperty { get; set; }
        }

        [ProtoContract]
        public class SomeOtherClass
        {
            [ProtoMember(tag: 1)]
            [Obsolete]
            public string SomeOtherProperty { get; set; }
        }

        [ProtoContract]
        [Obsolete]
        public enum SomeEnum
        {
            Zero,
            One,
            Two,
        }

        [ProtoContract]
        public enum SomeOtherEnum
        {
            Zero,
            One,
            Two,
            [Obsolete]
            Three,
        }
    }
#pragma warning restore CS0612 // Type or member is obsolete
}