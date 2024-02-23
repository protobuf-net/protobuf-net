using Xunit;
using ProtoBuf.Meta;

namespace ProtoBuf.Test.Issues
{
    public class Issue323
    {
        const string ExpectedProto = @"syntax = ""proto3"";
package ProtoBuf.Test.Issues;

enum TokenType {
   TokenType_Temporary = 0;
   TokenType_Persistent = 1;
}
";

        [Fact]
        public void ProtoIncludesEnumNamePrefix()
        {
            string proto = Serializer.GetProto(new SchemaGenerationOptions { Types = { typeof(TokenType) },
                Syntax = ProtoSyntax.Proto3, Flags = SchemaGenerationFlags.IncludeEnumNamePrefix });

            Assert.Equal(ExpectedProto, proto, ignoreLineEndingDifferences: true);
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
