using Google.Protobuf.Reflection;
using ProtoBuf.Meta;
using ProtoBuf.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Issues
{
    public class Issue855
    {
        private readonly ITestOutputHelper _log;
        private void Log(string message) => _log?.WriteLine(message);
        public Issue855(ITestOutputHelper log) => _log = log;

        const string Proto2Input = @"syntax = ""proto2"";

message BarClass {
    optional string string = 1;
    optional double double = 2;
    optional float float = 3;
    optional bool bool = 4;
    optional int32 int32 = 5;
    optional sint32 sint32 = 6;
    optional uint32 uint32 = 7;
    optional int64 int64 = 8;
    optional sint64 sint64 = 9;
    optional uint64 uint64 = 10;
    optional FooClass fooclass = 11;
    message FooClass {
    }
}
";

        const string Proto3Input = @"syntax = ""proto3"";

message BarClass {
    string string = 1;
    double double = 2;
    float float = 3;
    bool bool = 4;
    int32 int32 = 5;
    sint32 sint32 = 6;
    uint32 uint32 = 7;
    int64 int64 = 8;
    sint64 sint64 = 9;
    uint64 uint64 = 10;
    FooClass fooclass = 11;
    message FooClass {
    }
}
";

        [Fact]
        public void CSharpGeneratedCodeNullableProto2()
        {
            var set = new FileDescriptorSet();
            set.Add("my.proto", true, new StringReader(Proto2Input));
            set.Process();
            Assert.Empty(set.GetErrors());
            var actualCsharp = CSharpCodeGenerator.Default.Generate(set, options: new Dictionary<string, string>
            {
                { "nullablevaluetype", "true" },
            }).Single().Text;
            Log(actualCsharp);

            var expectedCsharp = File.ReadAllText(@"Issues/Issue855Proto2CSharp.Generated.cs");
            Assert.Equal(expectedCsharp, actualCsharp, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void CSharpGeneratedCodeNullableProto3()
        {
            var set = new FileDescriptorSet();
            set.Add("my.proto", true, new StringReader(Proto3Input));
            set.Process();
            Assert.Empty(set.GetErrors());
            var actualCsharp = CSharpCodeGenerator.Default.Generate(set, options: new Dictionary<string, string>
            {
                { "nullablevaluetype", "true" },
            }).Single().Text;
            Log(actualCsharp);

            var expectedCsharp = File.ReadAllText(@"Issues/Issue855Proto3CSharp.Generated.cs");
            Assert.Equal(expectedCsharp, actualCsharp, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void VBGeneratedCodeNullableProto2()
        {
            var set = new FileDescriptorSet();
            set.Add("my.proto", true, new StringReader(Proto2Input));
            set.Process();
            Assert.Empty(set.GetErrors());
            var actualVB = VBCodeGenerator.Default.Generate(set, options: new Dictionary<string, string>
            {
                { "nullablevaluetype", "true" },
            }).Single().Text;
            Log(actualVB);

            var expectedVB = File.ReadAllText(@"Issues/Issue855Proto2VB.Generated.vb");
            Assert.Equal(expectedVB, actualVB, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void VBGeneratedCodeNullableProto3()
        {
            var set = new FileDescriptorSet();
            set.Add("my.proto", true, new StringReader(Proto3Input));
            set.Process();
            Assert.Empty(set.GetErrors());
            var actualVB = VBCodeGenerator.Default.Generate(set, options: new Dictionary<string, string>
            {
                { "nullablevaluetype", "true" },
            }).Single().Text;
            Log(actualVB);

            var expectedVB = File.ReadAllText(@"Issues/Issue855Proto3VB.Generated.vb");
            Assert.Equal(expectedVB, actualVB, ignoreLineEndingDifferences: true);
        }

    }
}
