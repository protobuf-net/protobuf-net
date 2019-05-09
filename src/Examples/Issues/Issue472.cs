using System.IO;
using Xunit;

namespace ProtoBuf.Issues
{
    public class Issue472
    {
        [Theory]
        [InlineData(0, false)]
        [InlineData(1, true)]
        [InlineData(2, true)]
        public void Execute(byte boolvalue, bool expected)
        {
            byte[] buffer = { 8, boolvalue };
            using (var ms = new MemoryStream(buffer))
            {
                using (var protoReader = ProtoReader.Create(out var state, ms, null))
                {
                    var fieldNumber = protoReader.ReadFieldHeader(ref state);
                    var value = protoReader.ReadBoolean(ref state);

                    Assert.Equal(expected, value);
                }
            }
        }
    }
}
