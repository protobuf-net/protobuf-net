using ProtoBuf.BuildTools.Generators;
using System.Collections.Immutable;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace BuildToolsUnitTests.Grpc
{
    /// <summary>
    /// <c>PBN4018</c>: a dropped contract is a degradation on a JIT build and a failure under AOT, and only
    /// the second needs saying twice.
    /// </summary>
    /// <remarks>
    /// The drop diagnostics say "the runtime proxy will be used", which is accurate and proportionate when
    /// there *is* one. Under <c>PublishAot</c> there is not - <c>ProxyEmitter</c> needs ref-emit and the
    /// marshallers need <c>MakeGenericType</c> - so the same contract throws on first use instead. That
    /// depends on a build property, which is why it cannot be decided where the drop is decided.
    /// </remarks>
    public class GrpcDroppedUnderAotTests : Aot.AotGeneratorTestBase
    {
        public GrpcDroppedUnderAotTests(ITestOutputHelper log) : base(log) { }

        private static readonly string SurfacePath = Path.Combine("Grpc", "Data", "_ContractSurface.cs");

        /// <summary>A contract with an unsupported member, so it is dropped with PBN4002.</summary>
        private const string Source = """
            #nullable enable
            using ProtoBuf;
            using ProtoBuf.Grpc;
            using ProtoBuf.Grpc.Configuration;
            using ProtoBuf.Meta;
            using System;
            using System.Threading.Tasks;

            namespace Dropped;

            [ProtoContract]
            public class Request
            {
                [ProtoMember(1)] public string? Name { get; set; }
            }

            [Service]
            public interface IThing
            {
                // IObservable<T> is a runtime-path shape, so the whole contract is dropped.
                //
                // This used to be Task<Stream>, which stopped working as a *dropped* example the moment
                // the byte-stream shapes were implemented - Task<Stream> is now emitted. IObservable is
                // the durable choice here: it is reshaped at run time and is not on the roadmap.
                IObservable<Request> DownloadAsync(Request request, CallContext context = default);
            }

            [ProtoModel]
            public partial class DroppedModel : TypeModel
            {
                public static DroppedModel Instance { get; } = new DroppedModel();
            }

            [ProtoGrpc(Model = typeof(DroppedModel))]
            [ProtoService(typeof(IThing))]
            public sealed partial class DroppedServices : ClientFactory { }
            """;

        [Theory]
        [InlineData("PublishAot")]
        [InlineData("PublishTrimmed")]
        [InlineData("IsAotCompatible")]
        [InlineData("IsTrimmable")]
        public void DroppedContractIsEscalatedWhenAotIsRequested(string property)
        {
            var output = Run(ImmutableDictionary<string, string>.Empty.Add("build_property." + property, "true"));

            Assert.Contains("PBN4018", output);
            Assert.Contains("Dropped.IThing", output);
            // it names which property was set, so the reader knows why they are being told
            Assert.Contains(property, output);
            // ...and the original drop reason is still there; this adds to it rather than replacing it
            Assert.Contains("PBN4002", output);
        }

        /// <summary>
        /// The control, and the whole reason this is a separate id: on a JIT build the drop really is a
        /// fallback, so saying it twice would be noise.
        /// </summary>
        [Fact]
        public void NothingExtraIsSaidWithoutAnAotRequest()
        {
            var output = Run(globalOptions: null);

            Assert.DoesNotContain("PBN4018", output);
            Assert.Contains("PBN4002", output);
        }

        private string Run(ImmutableDictionary<string, string>? globalOptions)
        {
            var sb = new StringBuilder();
            Execute<GrpcProxyGenerator>(Source, sb, fileName: "dropped.cs",
                extraSources: new[] { (SurfacePath, File.ReadAllText(SurfacePath)) },
                globalOptions: globalOptions);
            return sb.ToString();
        }
    }
}
