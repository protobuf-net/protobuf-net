using Google.Protobuf.Reflection;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Reflection.Test
{
    public class DetectSpecialMessageKind
    {
        private readonly ITestOutputHelper _log;

        public DetectSpecialMessageKind(ITestOutputHelper log)
           => _log = log;

        private void Log(string message)
            => _log?.WriteLine(message);

        [Theory]
        [InlineData(@"
syntax = ""proto3"";
message SomeMessage {
  int32 i = 1;
}", (int)DescriptorProto.SpecialKind.None)]
        [InlineData(@"
syntax = ""proto3"";
import ""protobuf-net/protogen.proto"";
message ExplicitNone {
  option (.protobuf_net.msgopt) = { messageKind: MESSAGEKIND_NONE };
  int32 i = 1;
}", (int)DescriptorProto.SpecialKind.None)]
        [InlineData(@"
syntax = ""proto3"";
import ""protobuf-net/protogen.proto"";
message UsesImplicitZero {
  option (.protobuf_net.msgopt) = { messageKind: MESSAGEKIND_NULL_WRAPPER };
  int32 i = 1;
}", (int)DescriptorProto.SpecialKind.NullableWrapperImplicitZero)]
        [InlineData(@"
syntax = ""proto3"";
import ""protobuf-net/protogen.proto"";
message UsesFieldPresence {
  option (.protobuf_net.msgopt) = { messageKind: MESSAGEKIND_NULL_WRAPPER };
  optional int32 i = 1;
}", (int)DescriptorProto.SpecialKind.NullableWrapperFieldPresence)]
        [InlineData(@"
syntax = ""proto3"";
import ""protobuf-net/protogen.proto"";
message UsesFieldPresenceViaOneOf {
  option (.protobuf_net.msgopt) = { messageKind: MESSAGEKIND_NULL_WRAPPER };
  oneof whatever {
    int32 i = 1;
  }
}", (int)DescriptorProto.SpecialKind.NullableWrapperFieldPresence)]
        public void CanDetectSpecialMessageKinds(string schema, int kind)
        {
            var fds = new FileDescriptorSet();
            Assert.True(fds.Add("my.proto", source: new StringReader(schema)));
            fds.Process();
            var errors = fds.GetErrors();
            foreach (var error in errors)
            {
                Log(error.ToString(true));
            }
            Assert.Empty(errors);
            var file = fds.Files.First();
            Assert.Equal("my.proto", file.Name);
            var message = file.MessageTypes.Single();
            Assert.Equal((DescriptorProto.SpecialKind)kind, message.ParsedSpecialKind);
        }

        //TODO: invalid types
        [Theory]
        [InlineData(@"
syntax = ""proto2"";
import ""protobuf-net/protogen.proto"";
message SomeMessage {
  option (.protobuf_net.msgopt) = { messageKind: MESSAGEKIND_NULL_WRAPPER };
  optional int32 i = 1;
}", "my.proto(4,9,4,20): warning: Null wrapper message 'SomeMessage' requires proto3 syntax", 42)]
        [InlineData(@"
syntax = ""proto3"";
import ""protobuf-net/protogen.proto"";
message SomeMessage {
  option (.protobuf_net.msgopt) = { messageKind: MESSAGEKIND_NULL_WRAPPER };
  optional int32 i = 1;
  optional int32 j = 3;
}", "my.proto(4,9,4,20): warning: Null wrapper message 'SomeMessage' requires exactly one field (found: 2)", 43)]
        [InlineData(@"
syntax = ""proto3"";
import ""protobuf-net/protogen.proto"";
message SomeMessage {
  option (.protobuf_net.msgopt) = { messageKind: MESSAGEKIND_NULL_WRAPPER };
}", "my.proto(4,9,4,20): warning: Null wrapper message 'SomeMessage' requires exactly one field (found: 0)", 43)]
        [InlineData(@"
syntax = ""proto3"";
import ""protobuf-net/protogen.proto"";
message SomeMessage {
  option (.protobuf_net.msgopt) = { messageKind: MESSAGEKIND_NULL_WRAPPER };
  optional int32 i = 2;
}", "my.proto(4,9,4,20): warning: Null wrapper message 'SomeMessage' must use field 1 (found: 2)", 44)]
        public void DetectInvalidSpecialKind(string schema, string expectedError, int errorNumber)
        {
            var fds = new FileDescriptorSet();
            Assert.True(fds.Add("my.proto", source: new StringReader(schema)));
            fds.Process();
            var errors = fds.GetErrors();
            foreach (var error in errors)
            {
                Log(error.ToString(true));
            }
            var err = Assert.Single(errors);
            Assert.Equal(errorNumber, err.ErrorNumber);
            Assert.Equal(expectedError, err.ToString(true));
        }
    }
}
