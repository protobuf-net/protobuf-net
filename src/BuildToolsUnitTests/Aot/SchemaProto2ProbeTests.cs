using Google.Protobuf.Reflection;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Reflection;
using System.Linq;
using Xunit;

namespace BuildToolsUnitTests.Aot
{
    /// <summary>
    /// What the schema front-end currently does with proto2 shapes and with <c>extend</c> —
    /// gaps C6 and C7 of <c>notes/gaps.md</c>.
    /// </summary>
    /// <remarks>
    /// A PROBE, not a gate: it reports the verdict rather than asserting a desired one, because
    /// the question being answered is "is this actually missing, or was it only assumed to be?".
    /// Three claimed gaps on this branch have already evaporated when asked directly.
    /// </remarks>
    public class SchemaProto2ProbeTests
    {
        private readonly ITestOutputHelper _output;

        public SchemaProto2ProbeTests(ITestOutputHelper output) => _output = output;

        private static FileDescriptorSet Parse(string content)
        {
            var set = new FileDescriptorSet();
            set.Add("probe.proto", true, new System.IO.StringReader(content));
            set.Process();
            Assert.DoesNotContain(set.GetErrors(), e => e.IsError);
            return set;
        }

        [Theory]
        [InlineData("required", @"message M { required int32 id = 1; }")]
        [InlineData("default", @"message M { optional int32 n = 1 [default = 7]; optional string s = 2 [default = ""hi""]; }")]
        [InlineData("default-enum", "enum E { A = 0; B = 1; }\nmessage M { optional E e = 1 [default = B]; }")]
        [InlineData("group", @"message M { optional group Detail = 1 { optional int32 depth = 1; } }")]
        [InlineData("extensions-range", @"message M { optional int32 id = 1; extensions 100 to 199; }")]
        [InlineData("extend", "message M { optional int32 id = 1; extensions 100 to 199; }\nextend M { optional string note = 100; }")]
        [InlineData("repeated-required-mix", @"message M { required int32 id = 1; repeated int32 xs = 2; optional int32 o = 3; }")]
        public void Proto2Shape(string label, string body)
        {
            var set = Parse("syntax = \"proto2\";\npackage probe;\n" + body);
            var plan = SchemaPlanBuilder.TryBuild(set, NameNormalizer.Default,
                "Probe", "ProbeModel", out var unsupported);

            if (plan is null)
            {
                _output.WriteLine($"{label,-22} REFUSED: {unsupported}");
            }
            else
            {
                var c = plan.Contracts.FirstOrDefault();
                _output.WriteLine($"{label,-22} built: {plan.Contracts.Count} contract(s)"
                    + (c is null ? "" : $", {c.Members.Count} member(s) on {c.TypeName}"));
                for (int i = 0; c is not null && i < c.Members.Count; i++)
                {
                    var m = c.Members[i];
                    _output.WriteLine($"      #{m.FieldNumber} {m.Name} kind={m.Kind} fmt={m.DataFormat}"
                        + $" req={m.IsRequired} default={m.DefaultLiteral ?? "(none)"}"
                        + $" cond={m.WriteCondition ?? "(none)"}");
                }
            }
        }
    }
}
