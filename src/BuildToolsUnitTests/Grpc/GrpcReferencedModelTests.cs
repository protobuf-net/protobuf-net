using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ProtoBuf.BuildTools.Generators;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace BuildToolsUnitTests.Grpc
{
    /// <summary>
    /// The other direction of seeding: when the named model lives in a <em>referenced assembly</em>,
    /// nothing can be added to it, so it is checked instead - per payload type it cannot serialize.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This cannot be a golden fixture, for the same reason <c>ProtoSurrogateReferenceTests</c> cannot:
    /// it needs genuinely separate compilations, since the entire question is what can be discovered
    /// about a model through metadata. The library here is compiled <em>with the serializer generator
    /// running</em>, so what the consumer sees is real generated output rather than a stand-in.
    /// </para>
    /// <para>
    /// That is also what makes the check possible at all: the serializers live on a nested
    /// <c>private</c> class, and Roslyn surfaces private nested types through metadata.
    /// </para>
    /// </remarks>
    public class GrpcReferencedModelTests : Aot.AotGeneratorTestBase
    {
        public GrpcReferencedModelTests(ITestOutputHelper log) : base(log) { }

        private static readonly string SurfacePath = Path.Combine("Grpc", "Data", "_ContractSurface.cs");

        /// <param name="seeds">what the library's model is told to serialize.</param>
        private static string LibrarySource(string seeds) => $$"""
            using ProtoBuf;
            using ProtoBuf.Meta;

            namespace Shared;

            [ProtoContract]
            public class HelloRequest
            {
                [ProtoMember(1)] public string Name { get; set; }
            }

            [ProtoContract]
            public class HelloReply
            {
                [ProtoMember(1)] public string Message { get; set; }
            }

            [ProtoModel]
            {{seeds}}
            public partial class SharedModel : TypeModel { }
            """;

        /// <summary>The consumer: declares the contract, and points at the library's model.</summary>
        private const string ConsumerSource = """
            #nullable enable
            using ProtoBuf.Grpc;
            using ProtoBuf.Grpc.Configuration;
            using Shared;
            using System.Threading.Tasks;

            namespace Consumer;

            [Service]
            public interface IGreeter
            {
                Task<HelloReply> SayHelloAsync(HelloRequest request, CallContext context = default);
            }

            [ProtoGrpc(Model = typeof(SharedModel))]
            [ProtoService(typeof(IGreeter))]
            public sealed partial class ConsumerServices : ClientFactory { }
            """;

        [Fact]
        public void PayloadTheReferencedModelCannotSerializeIsReported()
        {
            // seeded with the request only, so the *reply* is the gap
            var diagnostics = RunConsumer(LibrarySource("[ProtoSerializable(typeof(HelloRequest))]"));

            var reported = diagnostics.Where(static x => x.Id == "PBN4013").ToList();
            var single = Assert.Single(reported);
            Assert.Equal(DiagnosticSeverity.Warning, single.Severity);

            var message = single.GetMessage();
            Assert.Contains("Shared.HelloReply", message);
            Assert.Contains("Shared.SharedModel", message);
            Assert.DoesNotContain("HelloRequest", message);
        }

        /// <summary>
        /// The control. Without it, a check that reported every payload would pass the test above.
        /// </summary>
        [Fact]
        public void NothingIsReportedWhenTheReferencedModelCoversEveryPayload()
        {
            var diagnostics = RunConsumer(LibrarySource(
                "[ProtoSerializable(typeof(HelloRequest))] [ProtoSerializable(typeof(HelloReply))]"));

            Assert.DoesNotContain(diagnostics, static x => x.Id == "PBN4013");
        }

        /// <summary>
        /// A payload reached only as a <em>member</em> of a seed still has a serializer, and must not be
        /// reported. This is the case an attribute-based check gets wrong: `HelloReply` carries no
        /// `[ProtoSerializable]` of its own, and is perfectly serializable regardless.
        /// </summary>
        [Fact]
        public void PayloadReachedTransitivelyIsNotReported()
        {
            var diagnostics = RunConsumer($$"""
                using ProtoBuf;
                using ProtoBuf.Meta;

                namespace Shared;

                [ProtoContract]
                public class HelloRequest
                {
                    [ProtoMember(1)] public string Name { get; set; }
                }

                [ProtoContract]
                public class HelloReply
                {
                    [ProtoMember(1)] public string Message { get; set; }
                }

                // the only seed, and it reaches both payloads through its members
                [ProtoContract]
                public class Envelope
                {
                    [ProtoMember(1)] public HelloRequest Request { get; set; }
                    [ProtoMember(2)] public HelloReply Reply { get; set; }
                }

                [ProtoModel]
                [ProtoSerializable(typeof(Envelope))]
                public partial class SharedModel : TypeModel { }
                """);

            Assert.DoesNotContain(diagnostics, static x => x.Id == "PBN4013");
        }

        /// <summary>
        /// A hand-written <c>TypeModel</c> in another assembly carries no <c>[ProtoModel]</c>, cannot be
        /// inspected this way, and is not ours to judge - so nothing is said about it either way.
        /// </summary>
        [Fact]
        public void AHandWrittenReferencedModelIsLeftAlone()
        {
            var diagnostics = RunConsumer("""
                using ProtoBuf.Meta;

                namespace Shared;

                public class HelloRequest { public string Name { get; set; } }
                public class HelloReply { public string Message { get; set; } }

                public class SharedModel : TypeModel
                {
                    public static SharedModel Instance { get; } = new SharedModel();
                }
                """);

            Assert.DoesNotContain(diagnostics, static x => x.Id == "PBN4013");
            Assert.DoesNotContain(diagnostics, static x => x.Id == "PBN4012");
        }

        /// <summary>
        /// Compile the library (running the serializer generator over it, so the emitted
        /// <c>ISerializer&lt;T&gt;</c> set is real), then run the gRPC generator over a consumer that
        /// references it.
        /// </summary>
        private List<Diagnostic> RunConsumer(string librarySource)
        {
            var library = CompileWithModelGenerator("Shared", librarySource);

            var parseOptions = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse);
            var compilation = CSharpCompilation.Create("Consumer",
                new[]
                {
                    CSharpSyntaxTree.ParseText(ConsumerSource, parseOptions, path: "consumer.cs"),
                    CSharpSyntaxTree.ParseText(File.ReadAllText(SurfacePath), parseOptions, path: SurfacePath),
                },
                MetadataReferenceHelpers.WellKnownReferences
                    .Concat(MetadataReferenceHelpers.ProtoBufReferences)
                    .Append(library),
                CompilationOptions);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                new[] { new GrpcProxyGenerator().AsSourceGenerator() }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);
            return diagnostics.ToList();
        }

        private static MetadataReference CompileWithModelGenerator(string assemblyName, string source)
        {
            var parseOptions = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse);
            var compilation = CSharpCompilation.Create(assemblyName,
                new[] { CSharpSyntaxTree.ParseText(source, parseOptions) },
                MetadataReferenceHelpers.WellKnownReferences.Concat(MetadataReferenceHelpers.ProtoBufReferences),
                CompilationOptions);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                new[] { new ProtoModelGenerator().AsSourceGenerator() }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var withGenerated, out _);

            using var peStream = new MemoryStream();
            var emitted = withGenerated.Emit(peStream);
            Assert.True(emitted.Success, string.Join("\n", emitted.Diagnostics
                .Where(static x => x.Severity == DiagnosticSeverity.Error)
                .Select(static x => x.ToString())));

            return MetadataReference.CreateFromImage(peStream.ToArray());
        }
    }
}
