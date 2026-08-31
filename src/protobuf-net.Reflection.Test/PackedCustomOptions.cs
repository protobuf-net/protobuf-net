using Google.Protobuf.Reflection;
using System;
using ProtoBuf;
using System.IO;
using System.Linq;
using Xunit;

namespace ProtoBuf
{
    /// <summary>
    /// Custom option values that the extension field declares as packed are written packed, matching
    /// protoc; #1328 is that reading them back through the extension APIs then threw
    /// "Invalid wire-type (String)", so the parser was producing descriptors it could not itself
    /// consume. The encoding is pinned here alongside the read, since the two have to agree.
    /// </summary>
    public class PackedCustomOptions
    {
        private const string Schema = @"
syntax = ""proto3"";
package test;
import ""google/protobuf/descriptor.proto"";
enum MyEnum { UNKNOWN = 0; ALPHA = 1; BETA = 2; }
extend google.protobuf.MessageOptions {
    repeated MyEnum packed_enum = 50000;
    repeated MyEnum expanded_enum = 50002 [packed = false];
}
message Foo {
    option (packed_enum) = ALPHA;
    option (packed_enum) = BETA;
    option (expanded_enum) = ALPHA;
    option (expanded_enum) = BETA;
    string bar = 1;
}
message Unary {
    option (packed_enum) = ALPHA;
    string bar = 1;
}
";

        private static FileDescriptorSet Parse()
        {
            var set = new FileDescriptorSet();
            set.Add("test.proto", true, new StringReader(Schema));
            set.Process();
            Assert.Empty(set.GetErrors());
            return set;
        }

        private static MessageOptions OptionsFor(string message)
            => Parse().Files.Single(x => x.Name == "test.proto").MessageTypes.Single(x => x.Name == message).Options;

        [Fact]
        public void PackedOptionValuesAreReadable()
        {
            var options = OptionsFor("Foo");
            Assert.Equal(new[] { 1, 2 }, Extensible.GetValues<int>(options, 50000));
        }

        [Fact]
        public void UnaryPackedOptionValueIsReadable()
        {
            var options = OptionsFor("Unary");
            Assert.Equal(new[] { 1 }, Extensible.GetValues<int>(options, 50000));
        }

        /// <summary>
        /// google.api.field_behavior declares [packed = false] precisely because the packed
        /// switch-over broke older parsers, so declared packedness has to be honoured on write.
        /// </summary>
        [Fact]
        public void UnpackedOptionValuesAreReadable()
        {
            var options = OptionsFor("Foo");
            Assert.Equal(new[] { 1, 2 }, Extensible.GetValues<int>(options, 50002));
        }

        [Fact]
        public void EncodingMatchesTheDeclaredPackedness()
        {
            var data = DescriptorProto.GetExtensionData(OptionsFor("Foo"));

            // 50000 packed:   tag 82-B5-18 (wire type 2), then one length-delimited run per value;
            // 50002 expanded: tag 90-B5-18 (wire type 0), one varint per value.
            //
            // Note the packed field emits a run per value where protoc emits a single merged run
            // (82-B5-18-02-01-02). Both decode to the same values, and every conformant reader
            // accepts either, but it is a known byte-level divergence rather than a deliberate
            // choice - pinned here so that closing it is a visible change.
            Assert.Equal("82-B5-18-01-01-82-B5-18-01-02-90-B5-18-01-90-B5-18-02", BitConverter.ToString(data));
        }
    }
}
