using Google.Protobuf.Reflection;
using ProtoBuf.Reflection;
using System.IO;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test.Issues
{
    public class Issue1010
    {
        private readonly ITestOutputHelper _log;

        public Issue1010(ITestOutputHelper log)
            => _log = log;

        [Theory]
        [InlineData(@"syntax = ""proto2""; message StackFrame { /** The line within the file of the frame. If source is null or doesn't exist, line is 0 and must be ignored. */ required int32 line = 1; /** The column within the line. If source is null or doesn't exist, column is 0 and must be ignored. */ required int32 column = 2; /** An optional end line of the range covered by the stack frame. */ optional int32 endLine = 3; }")]
        [InlineData(@"syntax = ""proto2""; message StackFrame { /** The line within the file of the frame. If source is null or doesn't exist, line is 0 and must be ignored. */
required int32 line = 1; /** The column within the line. If source is null or doesn't exist, column is 0 and must be ignored. */ required int32 column = 2; /** An optional end line of the range covered by the stack frame. */ optional int32 endLine = 3; }")]
        [InlineData(@"syntax = ""proto2"";
message StackFrame {
    /** The line within the file of the frame. If source is null or doesn't exist, line is 0 and must be ignored. */
    required int32 line = 1;
    /** The column within the line. If source is null or doesn't exist, column is 0 and must be ignored. */
    required int32 column = 2;
    /** An optional end line of the range covered by the stack frame. */
    optional int32 endLine = 3;
}")]
        public void Execute(string schema)
        {
            var set = new FileDescriptorSet();
            Assert.True(set.Add("schema.proto", source: new StringReader(schema)));
            set.Process();
            Assert.Empty(set.GetErrors());
            var msg = Assert.Single(Assert.Single(set.Files).MessageTypes);
            Assert.Equal(3, msg.Fields.Count);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("a", "1,1 AlphaNumeric a")]
        [InlineData("a//", @"1,1 AlphaNumeric a
1,2 Comment //")]
        [InlineData("//", "1,1 Comment //")]
        [InlineData("//a", @"1,1 Comment //
1,3 Comment a")]
        [InlineData("//a//b", @"1,1 Comment //
1,3 Comment a//b")]
        [InlineData(@"//
a", @"1,1 Comment //
2,1 AlphaNumeric a")]
        [InlineData(@"/**/", @"1,1 Comment /*
1,3 Comment */")]
        [InlineData(@"/*/a", @"1,1 Comment /*
1,3 Comment /a")]
        [InlineData(@"/*a*/b", @"1,1 Comment /*
1,3 Comment a
1,4 Comment */
1,6 AlphaNumeric b")]
        [InlineData(@"/*/*a*/b", @"1,1 Comment /*
1,3 Comment /*a
1,6 Comment */
1,8 AlphaNumeric b")]
        [InlineData(@"/*a
bcd
//e
f*/g", @"1,1 Comment /*
1,3 Comment a
2,1 Comment bcd
3,1 Comment //e
4,1 Comment f
4,2 Comment */
4,4 AlphaNumeric g")]
        public void VerifyTokens(string schema, string expected)
        {
            // issue 1010 was a failure to correctly tokenize comments; hence we focus on those here
            using var reader = new StringReader(schema);
            var sb = new StringBuilder();
            foreach (var token in reader.Tokenize("schema.proto"))
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append(token.LineNumber).Append(',').Append(token.ColumnNumber).Append(' ').Append(token.Type).Append(' ').Append(token.Value);
            }
            var actual = sb.ToString();
            _log.WriteLine(actual);
            Assert.Equal(expected, actual, ignoreLineEndingDifferences: true);
        }
    }
}
