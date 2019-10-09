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
            using var ms = new MemoryStream(buffer);
            using var state = ProtoReader.State.Create(ms, null);
            var fieldNumber = state.ReadFieldHeader();
            var value = state.ReadBoolean();

            Assert.Equal(expected, value);
        }
    }
}
