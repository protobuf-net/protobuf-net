using BuildToolsUnitTests.CodeFixes.Abstractions;
using Microsoft.CodeAnalysis.Testing;
using ProtoBuf.BuildTools.Analyzers;
using ProtoBuf.CodeFixes;
using System.Threading.Tasks;
using Xunit;

namespace BuildToolsUnitTests.CodeFixes
{
    /// <summary>
    /// The fix that turns "you have contracts and no model" into a model.
    /// </summary>
    public class AddProtoModelCodeFixProviderTests : CodeFixProviderTestsBase<AddProtoModelCodeFixProvider>
    {
        /// <summary>
        /// Stubbed to dodge the real attributes' <c>[Experimental]</c> gate; the analyzer matches by full name.
        /// </summary>
        private const string Preamble = @"
using ProtoBuf;
namespace ProtoBuf {
    internal sealed class ProtoModelAttribute : System.Attribute { }
    public sealed class ProtoContractAttribute : System.Attribute { }
    public sealed class ProtoMemberAttribute : System.Attribute { public ProtoMemberAttribute(int tag) { } }
    internal sealed class ProtoSerializableAttribute : System.Attribute { public ProtoSerializableAttribute(System.Type type) { } } }
namespace ProtoBuf.Meta { public abstract class TypeModel { } }
";

        [Fact]
        public async Task OffersToAddAModelWhenThereIsNone()
        {
            var test = new Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixTest<
                AotMigrationAnalyzer, AddProtoModelCodeFixProvider, DefaultVerifier>
            {
                TestState =
                {
                    Sources = { Preamble + @"
[ProtoContract] public class {|#0:Order|} { [ProtoMember(1)] public int Id { get; set; } }" },
                },
            };
            test.TestState.ExpectedDiagnostics.Add(
                new DiagnosticResult(AotMigrationAnalyzer.NoModel).WithLocation(0));

            // the fix adds a file; asserting its presence and shape is the point
            test.FixedState.Sources.Add(Preamble + @"
[ProtoContract] public class Order { [ProtoMember(1)] public int Id { get; set; } }");
            test.FixedState.Sources.Add(("ProtoModel.cs", @"using ProtoBuf;
using ProtoBuf.Meta;

// Compile-time serializers for this project. Name the types you serialize *directly*; everything
// reachable from those - member types, collection elements, map keys and values, [ProtoInclude]
// sub-types - is included automatically.
//
// See https://protobuf-net.github.io/protobuf-net/aot
[ProtoModel]
[ProtoSerializable(typeof(global::Order))]
internal partial class ProtoModel : TypeModel
{
}
"));
            // deliberately no expected diagnostic here: the fix adds the model, which is exactly what
            // the announcement was asking for, so it stops firing

            test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
            await test.RunAsync();
        }

        /// <summary>
        /// The console-app shape: top-level statements put the contracts in the global namespace,
        /// so the contract's namespace says nothing - but RootNamespace (which MSBuild defaults to
        /// the project name, and the SDK makes compiler-visible) still names the right home. A
        /// project ConsoleApp11 gets its model in namespace ConsoleApp11.
        /// </summary>
        [Fact]
        public async Task GeneratedModelUsesRootNamespaceWhenContractIsGlobal()
        {
            var test = new Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixTest<
                AotMigrationAnalyzer, AddProtoModelCodeFixProvider, DefaultVerifier>
            {
                TestState =
                {
                    Sources = { Preamble + @"
[ProtoContract] public class {|#0:Order|} { [ProtoMember(1)] public int Id { get; set; } }" },
                    AnalyzerConfigFiles = { ("/.globalconfig", @"is_global = true
build_property.RootNamespace = ConsoleApp11") },
                },
            };
            test.TestState.ExpectedDiagnostics.Add(
                new DiagnosticResult(AotMigrationAnalyzer.NoModel).WithLocation(0));

            test.FixedState.Sources.Add(Preamble + @"
[ProtoContract] public class Order { [ProtoMember(1)] public int Id { get; set; } }");
            test.FixedState.AnalyzerConfigFiles.Add(("/.globalconfig", @"is_global = true
build_property.RootNamespace = ConsoleApp11"));
            test.FixedState.Sources.Add(("ProtoModel.cs", @"using ProtoBuf;
using ProtoBuf.Meta;

// Compile-time serializers for this project. Name the types you serialize *directly*; everything
// reachable from those - member types, collection elements, map keys and values, [ProtoInclude]
// sub-types - is included automatically.
//
// See https://protobuf-net.github.io/protobuf-net/aot
namespace ConsoleApp11
{
    [ProtoModel]
    [ProtoSerializable(typeof(global::Order))]
    internal partial class ProtoModel : TypeModel
    {
    }
}
"));

            test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
            await test.RunAsync();
        }

        /// <summary>
        /// The model must not land in the global root: with no project default namespace to hand
        /// (this harness supplies none), the anchor contract's namespace is used.
        /// </summary>
        [Fact]
        public async Task GeneratedModelRespectsTheContractNamespace()
        {
            var test = new Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixTest<
                AotMigrationAnalyzer, AddProtoModelCodeFixProvider, DefaultVerifier>
            {
                TestState =
                {
                    Sources = { Preamble + @"
namespace Shop.Data { [ProtoContract] public class {|#0:Order|} { [ProtoMember(1)] public int Id { get; set; } } }" },
                },
            };
            test.TestState.ExpectedDiagnostics.Add(
                new DiagnosticResult(AotMigrationAnalyzer.NoModel).WithLocation(0));

            test.FixedState.Sources.Add(Preamble + @"
namespace Shop.Data { [ProtoContract] public class Order { [ProtoMember(1)] public int Id { get; set; } } }");
            test.FixedState.Sources.Add(("ProtoModel.cs", @"using ProtoBuf;
using ProtoBuf.Meta;

// Compile-time serializers for this project. Name the types you serialize *directly*; everything
// reachable from those - member types, collection elements, map keys and values, [ProtoInclude]
// sub-types - is included automatically.
//
// See https://protobuf-net.github.io/protobuf-net/aot
namespace Shop.Data
{
    [ProtoModel]
    [ProtoSerializable(typeof(global::Shop.Data.Order))]
    internal partial class ProtoModel : TypeModel
    {
    }
}
"));

            test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
            await test.RunAsync();
        }
    }
}
