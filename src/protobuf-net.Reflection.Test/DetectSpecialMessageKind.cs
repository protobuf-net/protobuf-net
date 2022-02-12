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

        [Fact]
        public void CanDetectSpecialMessageKinds()
        {
            const string schema = @"
syntax = ""proto3"";
import ""protobuf-net/protogen.proto"";
package SomePackage;
message SomeMessage {
  int32 i = 1;
}
message UsesImplicitZero {
  option (.protobuf_net.msgopt) = { messageKind: MESSAGEKIND_NULL_WRAPPER };
  int32 i = 1;
}
message UsesFieldPresence {
  option (.protobuf_net.msgopt) = { messageKind: MESSAGEKIND_NULL_WRAPPER };
  optional int32 i = 1;
}
message UsesFieldPresenceViaOneOf {
  option (.protobuf_net.msgopt) = { messageKind: MESSAGEKIND_NULL_WRAPPER };
  oneof whatever {
    int32 i = 1;
  }
}
";
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
            Assert.True(file.TryResolveMessage(".SomePackage.SomeMessage", null, out var message, true), "SomeMessage");
            Assert.Equal(DescriptorProto.SpecialKind.None, message.ParsedSpecialKind);

            Assert.True(file.TryResolveMessage(".SomePackage.UsesImplicitZero", null, out message, true), "UsesImplicitZero");
            Assert.Equal(DescriptorProto.SpecialKind.NullableWrapperImplicitZero, message.ParsedSpecialKind);

            Assert.True(file.TryResolveMessage(".SomePackage.UsesFieldPresence", null, out message, true), "UsesFieldPresence");
            Assert.Equal(DescriptorProto.SpecialKind.NullableWrapperFieldPresence, message.ParsedSpecialKind);

            Assert.True(file.TryResolveMessage(".SomePackage.UsesFieldPresenceViaOneOf", null, out message, true), "UsesFieldPresenceViaOneOf");
            Assert.Equal(DescriptorProto.SpecialKind.NullableWrapperFieldPresence, message.ParsedSpecialKind);
        }
    }

    // TODO: invalid syntax, multiple/zero fields, field number not one, invalid types
}
