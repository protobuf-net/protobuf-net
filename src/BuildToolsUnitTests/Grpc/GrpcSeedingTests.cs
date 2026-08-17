using ProtoBuf.BuildTools.Generators;
using System.IO;
using Xunit;

namespace BuildToolsUnitTests.Grpc
{
    /// <summary>
    /// Seeding: a <c>[ProtoModel]</c> named by a <c>[ProtoGrpc]</c> declaration picks up the payload
    /// types of that declaration's contracts, without the consumer listing them again.
    /// </summary>
    /// <remarks>
    /// This needs its own suite because neither golden harness runs both generators - the AOT goldens
    /// run <c>ProtoModelGenerator</c> with no protobuf-net.Grpc surface in the compilation, and the gRPC
    /// goldens run <c>GrpcProxyGenerator</c> with a hand-written stand-in for the model. Seeding is
    /// precisely the seam between them, so it is invisible to both.
    /// </remarks>
    public class GrpcSeedingTests : Aot.AotGeneratorTestBase
    {
        public GrpcSeedingTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        private static readonly string SurfacePath = Path.Combine("Grpc", "Data", "_ContractSurface.cs");

        /// <param name="grpc">
        /// The <c>[ProtoGrpc]</c> declaration, or empty to leave it out - which is the control: the model
        /// itself never names a seed, so anything it serializes came from the gRPC side.
        /// </param>
        private static string Source(string grpc) => $$"""
            #nullable enable
            using ProtoBuf;
            using ProtoBuf.Grpc;
            using ProtoBuf.Grpc.Configuration;
            using ProtoBuf.Meta;
            using System.Threading.Tasks;

            namespace Seeded;

            [ProtoContract]
            public class HelloRequest
            {
                [ProtoMember(1)] public string? Name { get; set; }
            }

            [ProtoContract]
            public class HelloReply
            {
                [ProtoMember(1)] public string? Message { get; set; }
            }

            [Service]
            public interface IGreeter
            {
                Task<HelloReply> SayHelloAsync(HelloRequest request, CallContext context = default);
            }

            // deliberately NO [ProtoSerializable] - that is the whole point
            [ProtoModel]
            public partial class SeededModel : TypeModel { }

            {{grpc}}
            """;

        private const string GrpcDeclaration = """
            [ProtoGrpc(Model = typeof(SeededModel))]
            [ProtoService(typeof(IGreeter))]
            public sealed partial class SeededServices : ClientFactory { }
            """;

        [Fact]
        public void PayloadTypesAreSeededFromTheServiceContract()
        {
            var generated = Run(Source(GrpcDeclaration));

            Assert.Contains("ISerializer<global::Seeded.HelloRequest>", generated);
            Assert.Contains("ISerializer<global::Seeded.HelloReply>", generated);
        }

        /// <summary>
        /// The control, and it is load-bearing: without it, a model that serialized everything in sight
        /// would pass the test above and nothing would notice.
        /// </summary>
        [Fact]
        public void NothingIsSeededWithoutAGrpcDeclaration()
        {
            var generated = Run(Source(""));

            Assert.DoesNotContain("ISerializer<global::Seeded.HelloRequest>", generated);
            Assert.DoesNotContain("ISerializer<global::Seeded.HelloReply>", generated);
        }

        /// <summary>
        /// A declaration naming a <em>different</em> model must not seed this one; the link is by symbol,
        /// not by "there is a [ProtoGrpc] somewhere".
        /// </summary>
        [Fact]
        public void SeedsFollowTheNamedModelOnly()
        {
            var generated = Run(Source("""
                public partial class OtherModel : TypeModel
                {
                    public static OtherModel Instance { get; } = new OtherModel();
                }

                [ProtoGrpc(Model = typeof(OtherModel))]
                [ProtoService(typeof(IGreeter))]
                public sealed partial class SeededServices : ClientFactory { }
                """));

            Assert.DoesNotContain("ISerializer<global::Seeded.HelloRequest>", generated);
        }

        private string Run(string source)
            => Execute<ProtoModelGenerator>(source, fileName: "seeded.cs",
                extraSources: new[] { (SurfacePath, File.ReadAllText(SurfacePath)) }).GeneratedCode;
    }
}
