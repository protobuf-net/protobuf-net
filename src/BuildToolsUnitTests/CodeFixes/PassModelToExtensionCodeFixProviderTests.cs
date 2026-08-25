using BuildToolsUnitTests.CodeFixes.Abstractions;
using Microsoft.CodeAnalysis.Testing;
using ProtoBuf.BuildTools.Analyzers;
using ProtoBuf.CodeFixes;
using System.Threading.Tasks;
using Xunit;

namespace BuildToolsUnitTests.CodeFixes
{
    /// <summary>
    /// The `PBN3014` fixer: supply a model to a `.proto` extension accessor that needs one.
    /// </summary>
    /// <remarks>
    /// gap B49. The accessors here are hand-written in the shape protogen emits - a legacy overload
    /// with no <c>this</c>, and a model-aware one that carries it - because the point under test is
    /// the SHAPE, and stubbing it keeps this a unit test of the fixer rather than of the generator.
    /// </remarks>
    public class PassModelToExtensionCodeFixProviderTests : CodeFixProviderTestsBase<PassModelToExtensionCodeFixProvider>
    {
        private const string Preamble = """
            using ProtoBuf;

            namespace ProtoBuf
            {
                internal sealed class ProtoModelAttribute : System.Attribute { }
            }

            [ProtoContract]
            public class Payload { [ProtoMember(1)] public int Id { get; set; } }

            public class Host : IExtensible
            {
                private IExtension __pbn__extensionData;
                IExtension IExtensible.GetExtensionObject(bool createIfMissing)
                    => Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);
            }

            public static class Extensions
            {
                public static Payload GetMsg(Host obj) => GetMsg(obj, null);
                public static Payload GetMsg(this Host obj, ProtoBuf.Meta.TypeModel model = null)
                    => obj == null ? default : Extensible.GetValue<Payload>(model, obj, 101);

                public static void SetMsg(Host obj, Payload value) => SetMsg(obj, value, null);
                public static void SetMsg(this Host obj, Payload value, ProtoBuf.Meta.TypeModel model = null)
                    => Extensible.AppendValue<Payload>(model, obj, 101, value);
            }

            [ProtoModel]
            public partial class MyModel : ProtoBuf.Meta.TypeModel
            {
                public static MyModel Instance { get; } = new MyModel();
            }


            """;

        [Fact]
        public async Task AppendsTheModelToAGetter()
        {
            await RunCodeFixTestAsync<AotMigrationAnalyzer>(
                Preamble + """
                public class Uses
                {
                    public Payload M(Host h) => {|#0:h.GetMsg()|};
                }
                """,
                Preamble + """
                public class Uses
                {
                    public Payload M(Host h) => h.GetMsg(MyModel.Instance);
                }
                """,
                new DiagnosticResult(AotMigrationAnalyzer.ExtensionNeedsModel).WithLocation(0)
                    .WithArguments("Extensions.GetMsg", "a message", "'MyModel.Instance'", "Payload"));
        }

        /// <summary>
        /// The setter's model is the THIRD parameter, so the fix must land after the value rather
        /// than in place of it.
        /// </summary>
        [Fact]
        public async Task AppendsTheModelAfterTheValueOnASetter()
        {
            await RunCodeFixTestAsync<AotMigrationAnalyzer>(
                Preamble + """
                public class Uses
                {
                    public void M(Host h) => {|#0:h.SetMsg(new Payload())|};
                }
                """,
                Preamble + """
                public class Uses
                {
                    public void M(Host h) => h.SetMsg(new Payload(), MyModel.Instance);
                }
                """,
                new DiagnosticResult(AotMigrationAnalyzer.ExtensionNeedsModel).WithLocation(0)
                    .WithArguments("Extensions.SetMsg", "a message", "'MyModel.Instance'", "Payload"));
        }

        /// <summary>
        /// A setter whose VALUE is null: the trailing argument must not be mistaken for the model.
        /// Blindly replacing the last argument here would silently discard the value, which is the
        /// one bug this fixer could plausibly have.
        /// </summary>
        [Fact]
        public async Task DoesNotMistakeANullValueForTheModel()
        {
            await RunCodeFixTestAsync<AotMigrationAnalyzer>(
                Preamble + """
                public class Uses
                {
                    public void M(Host h) => {|#0:h.SetMsg(null)|};
                }
                """,
                Preamble + """
                public class Uses
                {
                    public void M(Host h) => h.SetMsg(null, MyModel.Instance);
                }
                """,
                new DiagnosticResult(AotMigrationAnalyzer.ExtensionNeedsModel).WithLocation(0)
                    .WithArguments("Extensions.SetMsg", "a message", "'MyModel.Instance'", "Payload"));
        }

        /// <summary>
        /// An explicit <c>null</c> model IS replaced rather than appended to - the opposite of the
        /// case above, and told apart by which parameter the argument binds to.
        /// </summary>
        [Fact]
        public async Task ReplacesAnExplicitNullModel()
        {
            await RunCodeFixTestAsync<AotMigrationAnalyzer>(
                Preamble + """
                public class Uses
                {
                    public Payload M(Host h) => {|#0:h.GetMsg(null)|};
                }
                """,
                Preamble + """
                public class Uses
                {
                    public Payload M(Host h) => h.GetMsg(MyModel.Instance);
                }
                """,
                new DiagnosticResult(AotMigrationAnalyzer.ExtensionNeedsModel).WithLocation(0)
                    .WithArguments("Extensions.GetMsg", "a message", "'MyModel.Instance'", "Payload"));
        }
    }
}
