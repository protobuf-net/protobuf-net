using Xunit;
using ProtoBuf.Meta;

namespace ProtoBuf.Test.Issues
{
    public class Issue323
    {
        const string ExpectedProtoWithPrefixedEnumMembers = @"syntax = ""proto3"";
package ProtoBuf.Test.Issues;

enum TokenType {
   TokenType_Temporary = 0;
   TokenType_Persistent = 1;
}
";

        const string ExpectedProtoWithoutPrefixedEnumMembers = @"syntax = ""proto3"";
package ProtoBuf.Test.Issues;

enum TokenType {
   Temporary = 0;
   Persistent = 1;
}
";

        [Fact]
        public void GeneratesProtoWithPrefixedEnumMembers()
        {
            string proto = Serializer.GetProto(new SchemaGenerationOptions { Types = { typeof(TokenType) },
                Syntax = ProtoSyntax.Proto3, Flags = SchemaGenerationFlags.IncludeEnumNamePrefix });

            Assert.Equal(ExpectedProtoWithPrefixedEnumMembers, proto, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void GeneratesProtoWithoutPrefixedEnumMembers()
        {
            string proto = Serializer.GetProto(new SchemaGenerationOptions
            {
                Types = { typeof(TokenType) },
                Syntax = ProtoSyntax.Proto3,
                Flags = SchemaGenerationFlags.None,
            });

            Assert.Equal(ExpectedProtoWithoutPrefixedEnumMembers, proto, ignoreLineEndingDifferences: true);
        }

        [ProtoContract]
        public enum TokenType
        {
            [ProtoEnum]
            Temporary,
            [ProtoEnum]
            Persistent,
        }
    }
}
