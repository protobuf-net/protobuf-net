using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Google.Protobuf.Reflection;
using ProtoBuf.Meta;
using Xunit;

namespace ProtoBuf.Schemas
{
    /// <summary>
    /// The schema-writer's editions dialect: emitted directly from a RuntimeTypeModel, checked
    /// as a golden string, then proven against protoc (the emitted schema must be a *valid*
    /// editions file) and against our own parser.
    /// </summary>
    public class EditionsSchemaWriterTests
    {
        [ProtoContract]
        public class Order
        {
            [ProtoMember(1)] public int Id { get; set; }
            [ProtoMember(2, IsRequired = true)] public string Reference { get; set; }
            [ProtoMember(3, DataFormat = DataFormat.Group)] public Address Home { get; set; }
            [ProtoMember(4, IsPacked = true)] public int[] Codes { get; set; }
            [ProtoMember(5)] public int[] Unpacked { get; set; }
            [ProtoMember(6), DefaultValue(42)] public int Answer { get; set; } = 42;
            [ProtoMember(7)] public Status Status { get; set; }
            [ProtoMember(8)] public Severity Level { get; set; }
        }
        [ProtoContract]
        public class Address
        {
            [ProtoMember(1)] public string Line1 { get; set; }
        }
        public enum Status { Unknown = 0, Active = 1 }
        public enum Severity { High = 3, Low = 4 }

        private static string GetSchema(ProtoSyntax syntax)
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(Order), true);
            return model.GetSchema(typeof(Order), syntax);
        }

        [Fact]
        public void Edition2023Schema()
        {
            var schema = GetSchema(ProtoSyntax.Edition2023);
            Assert.Equal(@"edition = ""2023"";
package ProtoBuf.Schemas;

message Address {
   string Line1 = 1;
}
message Order {
   int32 Id = 1;
   string Reference = 2 [features.field_presence = LEGACY_REQUIRED];
   Address Home = 3 [features.message_encoding = DELIMITED];
   repeated int32 Codes = 4;
   repeated int32 Unpacked = 5 [features.repeated_field_encoding = EXPANDED];
   int32 Answer = 6 [default = 42];
   Status Status = 7;
   Severity Level = 8;
}
enum Severity {
   option features.enum_type = CLOSED;
   High = 3;
   Low = 4;
}
enum Status {
   Unknown = 0;
   Active = 1;
}
", schema, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void Proto3SchemaWarnsAboutGroups()
        {
            var schema = GetSchema(ProtoSyntax.Proto3).Replace("\r\n", "\n");
            Assert.Contains("// warning: 'group' is not valid in proto3; DELIMITED encoding requires edition 2023 or later (features.message_encoding = DELIMITED)\n   group Address Home = 3;", schema);
        }

        [Theory]
        [InlineData(ProtoSyntax.Edition2023)]
        [InlineData(ProtoSyntax.Edition2024)]
        public void EmittedEditionsSchemaSatisfiesProtocAndOurParser(ProtoSyntax syntax)
        {
            var schema = GetSchema(syntax);

            // our own parser accepts it without errors
            var set = new FileDescriptorSet();
            Assert.True(set.Add("runtime_writer.proto", true, new StringReader(schema)));
            set.Process();
            var errors = set.GetErrors();
            Assert.Empty(errors.Where(x => x.IsError));

            // protoc accepts it as a valid editions file; note the file goes to a temp
            // directory rather than Schemas, which the comparison suite globs
            var tempDir = Path.Combine(Path.GetTempPath(), "pbn-editions-writer");
            Directory.CreateDirectory(tempDir);
            var protoPath = Path.Combine(tempDir, $"runtime_writer_{syntax}.proto".ToLowerInvariant());
            File.WriteAllText(protoPath, schema);
            string protocExe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"windows\protoc.exe" :
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? @"macosx/protoc" :
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? @"linux/protoc" : null;
            Assert.NotNull(protocExe);
            using var proc = new Process();
            var psi = proc.StartInfo;
            psi.FileName = Path.Combine(Directory.GetCurrentDirectory(), protocExe);
            psi.Arguments = $"--descriptor_set_out={Path.ChangeExtension(protoPath, "protoc.bin")} {Path.GetFileName(protoPath)}";
            psi.WorkingDirectory = tempDir;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            proc.Start();
            var stderr = proc.StandardError.ReadToEnd();
            Assert.True(proc.WaitForExit(10_000));
            Assert.True(proc.ExitCode == 0, $"protoc rejected the emitted schema: {stderr}");
        }
    }
}
