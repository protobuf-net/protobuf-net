using Google.Protobuf.Reflection;
using ProtoBuf.Meta;
using System.IO;
using System.Linq;
using Xunit;

namespace ProtoBuf.Reflection.Test
{
    /// <summary>
    /// Covers <see cref="FileDescriptorSet.Serializer"/>: the model that makes the existing
    /// <see cref="FileDescriptorSet.Serialize(TypeModel, Stream, bool)"/> reachable from a project
    /// that references only this package, which brings protobuf-net.Core and so has no concrete
    /// <see cref="TypeModel"/> of its own.
    /// </summary>
    public class DescriptorSerializerTests
    {
        /// <summary>descriptor.proto, resolved from this package's own embedded copy.</summary>
        private static FileDescriptorSet ParseDescriptorProto()
        {
            var set = new FileDescriptorSet();
            Assert.True(set.Add("google/protobuf/descriptor.proto", includeInOutput: true));
            set.Process();
            Assert.DoesNotContain(set.GetErrors(), error => error.IsError);
            return set;
        }

        private static byte[] Serialize(FileDescriptorSet set, TypeModel model, bool includeImports)
        {
            using var ms = new MemoryStream();
            set.Serialize(model, ms, includeImports);
            return ms.ToArray();
        }

        [Fact]
        public void TheExposedModelRoundTripsADescriptorSet()
        {
            var bytes = Serialize(ParseDescriptorProto(), FileDescriptorSet.Serializer, includeImports: true);
            Assert.NotEmpty(bytes);

            var back = FileDescriptorSet.Serializer.Deserialize<FileDescriptorSet>(new MemoryStream(bytes));

            var file = Assert.Single(back.Files);
            Assert.Equal("google/protobuf/descriptor.proto", file.Name);
            Assert.Equal("google.protobuf", file.Package);
            Assert.Contains(file.MessageTypes, message => message.Name == "FileDescriptorSet");

            // and the bytes are stable across the round trip
            Assert.Equal(bytes, Serialize(back, FileDescriptorSet.Serializer, includeImports: true));
        }

        [Fact]
        public void TheExposedModelAgreesWithTheRuntimeModel()
        {
            var set = ParseDescriptorProto();

            Assert.Equal(
                Serialize(set, RuntimeTypeModel.Default, includeImports: true),
                Serialize(set, FileDescriptorSet.Serializer, includeImports: true));
        }

        [Fact]
        public void IncludeImportsComposesWithTheExposedModel()
        {
            // the filtering is the one thing a bare TypeModel cannot do: it works by narrowing
            // Files to those flagged for output, which is a property of the set, not the model
            var set = new FileDescriptorSet();
            Assert.True(set.Add("my.proto", includeInOutput: true, new StringReader(@"
syntax = ""proto3"";
import ""google/protobuf/descriptor.proto"";
message Holder { google.protobuf.FileDescriptorSet fds = 1; }")));
            set.Process();
            Assert.DoesNotContain(set.GetErrors(), error => error.IsError);
            Assert.Equal(2, set.Files.Count);

            var excluded = FileDescriptorSet.Serializer.Deserialize<FileDescriptorSet>(
                new MemoryStream(Serialize(set, FileDescriptorSet.Serializer, includeImports: false)));
            Assert.Equal("my.proto", Assert.Single(excluded.Files).Name);

            var included = FileDescriptorSet.Serializer.Deserialize<FileDescriptorSet>(
                new MemoryStream(Serialize(set, FileDescriptorSet.Serializer, includeImports: true)));
            Assert.Equal(
                new[] { "google/protobuf/descriptor.proto", "my.proto" },
                included.Files.Select(file => file.Name).OrderBy(name => name).ToArray());

            // the set itself is left as it was found
            Assert.Equal(2, set.Files.Count);
        }
    }
}
