using Google.Protobuf.Reflection;
using System.IO;
using Xunit;

namespace ProtoBuf.Test.Issues
{
    public class Issue1010
    {
        [Fact]
        public void Execute()
        {
            var schema = @"syntax = ""proto2""; message StackFrame { /** The line within the file of the frame. If source is null or doesn't exist, line is 0 and must be ignored. */ required int32 line = 1; /** The column within the line. If source is null or doesn't exist, column is 0 and must be ignored. */ required int32 column = 2; /** An optional end line of the range covered by the stack frame. */ optional int32 endLine = 3; }";
            var set = new FileDescriptorSet();
            Assert.True(set.Add("schema.proto", source: new StringReader(schema)));
            set.Process();
            Assert.Empty(set.GetErrors());
            var msg = Assert.Single(Assert.Single(set.Files).MessageTypes);
            Assert.Equal(3, msg.Fields.Count);
        }
    }
}
