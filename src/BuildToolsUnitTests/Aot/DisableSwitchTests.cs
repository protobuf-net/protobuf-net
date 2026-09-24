using ProtoBuf.BuildTools.Generators;
using ProtoBuf.BuildTools.Internal;
using System.Collections.Immutable;
using System.IO;
using Xunit;

namespace BuildToolsUnitTests.Aot
{
    /// <summary>
    /// <c>&lt;ProtoBufDisableBuildTools&gt;true&lt;/ProtoBufDisableBuildTools&gt;</c> turns off every
    /// analyzer and generator in this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It exists so that shipping the tooling <em>by default</em> is cheap to decline, which is the thing
    /// that makes shipping-by-default arguable at all - so it is load-bearing for the packaging decision
    /// rather than a convenience. Every entry point calls <c>Utils.BuildToolsDisabled()</c> as its opening
    /// line, and until this suite existed nothing checked that any of them did.
    /// </para>
    /// <para>
    /// Both generators are covered, and both directions each: silent when disabled, and - the half that
    /// makes the first half mean anything - loud when not. Without the enabled case, a generator that had
    /// simply stopped working would pass.
    /// </para>
    /// </remarks>
    public class DisableSwitchTests : AotGeneratorTestBase
    {
        public DisableSwitchTests(ITestOutputHelper log) : base(log) { }

        private static readonly ImmutableDictionary<string, string> Disabled
            = ImmutableDictionary<string, string>.Empty.Add(Literals.DisableProperty, "true");

        private const string ModelSource = """
            using ProtoBuf;
            using ProtoBuf.Meta;

            namespace Disabled;

            [ProtoContract]
            public class Thing
            {
                [ProtoMember(1)] public int Value { get; set; }
            }

            [ProtoModel]
            [ProtoSerializable(typeof(Thing))]
            public partial class TheModel : TypeModel { }
            """;

        [Fact]
        public void SerializerGeneratorIsSilentWhenDisabled()
            => Assert.Empty(RunModel(Disabled));

        [Fact]
        public void SerializerGeneratorEmitsWhenNotDisabled()
            => Assert.Contains("ISerializer<global::Disabled.Thing>", RunModel(globalOptions: null));

        [Fact]
        public void GrpcGeneratorIsSilentWhenDisabled()
            => Assert.Empty(RunGrpc(Disabled));

        [Fact]
        public void GrpcGeneratorEmitsWhenNotDisabled()
            => Assert.Contains("_ClientProxy", RunGrpc(globalOptions: null));

        private string RunModel(ImmutableDictionary<string, string>? globalOptions)
            => Execute<ProtoModelGenerator>(ModelSource, fileName: "disabled.cs",
                globalOptions: globalOptions).GeneratedCode;

        private string RunGrpc(ImmutableDictionary<string, string>? globalOptions)
        {
            var surfacePath = Path.Combine("Grpc", "Data", "_ContractSurface.cs");
            var source = """
                #nullable enable
                using ProtoBuf;
                using ProtoBuf.Grpc;
                using ProtoBuf.Grpc.Configuration;
                using ProtoBuf.Meta;
                using System.Threading.Tasks;

                namespace Disabled;

                [ProtoContract]
                public class Request
                {
                    [ProtoMember(1)] public string? Name { get; set; }
                }

                [Service]
                public interface IThing
                {
                    Task<Request> EchoAsync(Request request, CallContext context = default);
                }

                [ProtoModel]
                public partial class GrpcModel : TypeModel
                {
                    public static GrpcModel Instance { get; } = new GrpcModel();
                }

                [ProtoGrpc(Model = typeof(GrpcModel))]
                [ProtoService(typeof(IThing))]
                public sealed partial class TheServices : ClientFactory { }
                """;

            return Execute<GrpcProxyGenerator>(source, fileName: "disabled.cs",
                extraSources: new[] { (surfacePath, File.ReadAllText(surfacePath)) },
                globalOptions: globalOptions).GeneratedCode;
        }
    }
}
