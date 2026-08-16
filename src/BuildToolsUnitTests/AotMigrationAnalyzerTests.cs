using ProtoBuf.BuildTools.Analyzers;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BuildToolsUnitTests
{
    /// <summary>
    /// The migration analyzer: once a <c>[ProtoModel]</c> exists, existing call sites that still go
    /// through the runtime model are flagged, because turning the generator on does not move them.
    /// </summary>
    public class AotMigrationAnalyzerTests : AnalyzerTestBase<AotMigrationAnalyzer>
    {
        public AotMigrationAnalyzerTests(ITestOutputHelper log) : base(log) { }

        /// <summary>
        /// The trigger attribute is real Core API but gated by <c>[Experimental]</c>, and
        /// `Serializer`/`RuntimeTypeModel` live in protobuf-net rather than protobuf-net.Core —
        /// which is what this harness references, since BuildTools compiles Core's sources in.
        /// All three are therefore stubbed here.
        /// </summary>
        /// <remarks>
        /// Faithful for the purpose: the analyzer matches on the *full name* of the containing type,
        /// so a stub of the same name exercises exactly the same path. The real-build behaviour is
        /// pinned separately by `AotSmoke`, where analyzer and generator actually run together.
        /// </remarks>
        private const string Preamble = """
            using ProtoBuf;
            using ProtoBuf.Meta;
            using System.IO;

            namespace ProtoBuf
            {
                internal sealed class ProtoModelAttribute : System.Attribute { }

                public static class Serializer
                {
                    public static void Serialize<T>(Stream destination, T instance) { }
                    public static T DeepClone<T>(T instance) => instance;
                    public static long Measure<T>(T instance) => 0;

                    public static class NonGeneric
                    {
                        public static void Serialize(Stream destination, object instance) { }
                    }
                }
            }

            namespace ProtoBuf.Meta
            {
                public class RuntimeTypeModel
                {
                    public static RuntimeTypeModel Default => null;
                    public static RuntimeTypeModel Create() => null;
                    public void Serialize<T>(Stream destination, T instance) { }
                }
            }

            [ProtoContract]
            public class Order { [ProtoMember(1)] public int Id { get; set; } }
            """;

        private const string WithModel = Preamble + """

            [ProtoModel]
            public partial class MyModel : ProtoBuf.Meta.TypeModel { }
            """;

        [Fact]
        public async Task GenericCallThroughTheFacadeIsFlagged()
        {
            var diagnostics = await AnalyzeAsync(WithModel + """

                public class Uses
                {
                    public void M(Stream s) => Serializer.Serialize(s, new Order());
                }
                """);

            var hit = Assert.Single(diagnostics.Where(static x => x.Id == "PBN3010"));
            Assert.Contains("MyModel", hit.GetMessage());
            // the example must name something that exists: the generated static accessor, not a
            // camel-cased local that was never declared
            Assert.Contains("'MyModel.Instance.Serialize'", hit.GetMessage());
        }

        [Fact]
        public async Task NonGenericCallGetsTheOtherDiagnostic()
        {
            var diagnostics = await AnalyzeAsync(WithModel + """

                public class Uses
                {
                    public void M(Stream s, object o) => Serializer.NonGeneric.Serialize(s, o);
                }
                """);

            Assert.Single(diagnostics.Where(static x => x.Id == "PBN3011"));
            Assert.Empty(diagnostics.Where(static x => x.Id == "PBN3010"));
        }

        /// <summary>
        /// The whole point of the trigger: the runtime model is a perfectly good way to use
        /// protobuf-net, and this has nothing to say to anyone who has not opted into AOT.
        /// </summary>
        [Fact]
        public async Task NothingIsFlaggedWithoutAModel()
        {
            var diagnostics = await AnalyzeAsync(Preamble + """

                public class Uses
                {
                    public void M(Stream s, object o)
                    {
                        Serializer.Serialize(s, new Order());
                        Serializer.NonGeneric.Serialize(s, o);
                    }
                }
                """);

            Assert.Empty(diagnostics.Where(static x => x.Id is "PBN3010" or "PBN3011"));
        }

        /// <summary>
        /// A call on *some other* model is the thing we are asking people to write, so it must not
        /// be flagged — including a `RuntimeTypeModel.Create()` they configured deliberately.
        /// </summary>
        [Fact]
        public async Task CallsOnAnExplicitModelAreLeftAlone()
        {
            var diagnostics = await AnalyzeAsync(WithModel + """

                public class Uses
                {
                    private readonly RuntimeTypeModel _mine = RuntimeTypeModel.Create();

                    public void M(Stream s) => _mine.Serialize(s, new Order());
                }
                """);

            Assert.Empty(diagnostics.Where(static x => x.Id is "PBN3010" or "PBN3011"));
        }

        [Fact]
        public async Task TheDefaultModelReachedDirectlyIsFlagged()
        {
            var diagnostics = await AnalyzeAsync(WithModel + """

                public class Uses
                {
                    public void M(Stream s) => RuntimeTypeModel.Default.Serialize(s, new Order());
                }
                """);

            Assert.Single(diagnostics.Where(static x => x.Id == "PBN3010"));
        }

        /// <summary>
        /// DeepClone and Measure are on the serialization path too, and are easy to forget.
        /// </summary>
        /// <summary>
        /// With contracts and no model, the project is told once — at Info, because for a non-AOT
        /// consumer the argument is cold start rather than correctness.
        /// </summary>
        [Fact]
        public async Task AnnouncesOnceWhenThereAreContractsButNoModel()
        {
            var diagnostics = await AnalyzeAsync(Preamble);

            var hit = Assert.Single(diagnostics.Where(static x => x.Id == "PBN3013"));
            Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Info, hit.Severity);
            Assert.Empty(diagnostics.Where(static x => x.Id == "PBN3012"));
        }

        /// <summary>...and nothing at all once a model exists, which is the point of announcing.</summary>
        [Fact]
        public async Task SaysNothingOnceAModelExists()
        {
            var diagnostics = await AnalyzeAsync(WithModel);
            Assert.Empty(diagnostics.Where(static x => x.Id is "PBN3012" or "PBN3013"));
        }

        [Fact]
        public async Task TheOtherEntryPointsAreCoveredToo()
        {
            var diagnostics = await AnalyzeAsync(WithModel + """

                public class Uses
                {
                    public void M()
                    {
                        Serializer.DeepClone(new Order());
                        Serializer.Measure(new Order());
                    }
                }
                """);

            Assert.Equal(2, diagnostics.Count(static x => x.Id == "PBN3010"));
        }
    }
}
