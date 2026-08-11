using BuildToolsUnitTests.CodeFixes.Abstractions;
using Microsoft.CodeAnalysis.Testing;
using ProtoBuf.BuildTools.Analyzers;
using ProtoBuf.CodeFixes;
using System.Threading.Tasks;
using Xunit;

namespace BuildToolsUnitTests.CodeFixes
{
    /// <summary>
    /// The `PBN2010` fixer: swap the receiver for a model that is already in scope.
    /// </summary>
    public class UseAotModelCodeFixProviderTests : CodeFixProviderTestsBase<UseAotModelCodeFixProvider>
    {
        /// <summary>
        /// Stubs, for the same reason the analyzer tests stub: `Serializer` lives in protobuf-net
        /// rather than protobuf-net.Core, and the real trigger attribute is behind an
        /// `[Experimental]` gate. The fixer matches on full names, so these exercise the real path.
        /// </summary>
        private const string Preamble = """
            using ProtoBuf;
            using System.IO;

            namespace ProtoBuf
            {
                internal sealed class ProtoModelAttribute : System.Attribute { }

                public static class Serializer
                {
                    public static void Serialize<T>(Stream destination, T instance) { }
                }
            }

            [ProtoContract]
            public class Order { [ProtoMember(1)] public int Id { get; set; } }

            [ProtoModel]
            public partial class MyModel : ProtoBuf.Meta.TypeModel
            {
                // the generator emits this; the fixer's fallback names it
                public static MyModel Instance { get; } = new MyModel();
            }


            """;

        [Fact]
        public async Task SwapsTheReceiverForAModelInScope()
        {
            await RunCodeFixTestAsync<AotMigrationAnalyzer>(
                Preamble + """
                public class Uses
                {
                    private readonly MyModel _model = new MyModel();
                    public void M(Stream s) => {|#0:Serializer.Serialize(s, new Order())|};
                }
                """,
                Preamble + """
                public class Uses
                {
                    private readonly MyModel _model = new MyModel();
                    public void M(Stream s) => _model.Serialize(s, new Order());
                }
                """,
                new DiagnosticResult(AotMigrationAnalyzer.UsesRuntimeModel).WithLocation(0)
                    .WithArguments("Serializer.Serialize", "the AOT model 'MyModel'", "myModel", "Serialize"));
        }

        /// <summary>
        /// With nothing in scope, the generated <c>Instance</c> is what the fix reaches for — which
        /// is the case that matters, since a codebase part-way through migrating has no model in
        /// scope anywhere yet.
        /// </summary>
        [Fact]
        public async Task FallsBackToTheGeneratedSharedInstance()
        {
            await RunCodeFixTestAsync<AotMigrationAnalyzer>(
                Preamble + """
                public class Uses
                {
                    public void M(Stream s) => {|#0:Serializer.Serialize(s, new Order())|};
                }
                """,
                Preamble + """
                public class Uses
                {
                    public void M(Stream s) => MyModel.Instance.Serialize(s, new Order());
                }
                """,
                new DiagnosticResult(AotMigrationAnalyzer.UsesRuntimeModel).WithLocation(0)
                    .WithArguments("Serializer.Serialize", "the AOT model 'MyModel'", "myModel", "Serialize"));
        }
    }
}
