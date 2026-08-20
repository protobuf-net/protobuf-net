using ProtoBuf.BuildTools.Analyzers;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BuildToolsUnitTests
{
    /// <summary>
    /// <c>PBN4015</c>: the project asks for AOT or trimming, uses protobuf-net.Grpc, and has no
    /// <c>[ProtoGrpc]</c> - so its proxies and marshallers will be built by reflection.
    /// </summary>
    public class GrpcMigrationAnalyzerTests : AnalyzerTestBase<GrpcMigrationAnalyzer>
    {
        public GrpcMigrationAnalyzerTests(ITestOutputHelper log) : base(log) { }

        /// <summary>
        /// Stubs, matched by full name exactly as the analyzer does - protobuf-net.Grpc cannot be
        /// referenced here, because BuildTools compiles protobuf-net.Core's sources in and every type in
        /// Core would become ambiguous.
        /// </summary>
        private const string Preamble = """
            namespace Grpc.Core { public class CallInvoker { } }

            namespace ProtoBuf.Grpc.Configuration
            {
                public abstract class ClientFactory
                {
                    public static ClientFactory Default => null;
                }

                [System.AttributeUsage(System.AttributeTargets.Class)]
                public sealed class ProtoGrpcAttribute : System.Attribute
                {
                    public System.Type Model { get; set; }
                }

                [System.AttributeUsage(System.AttributeTargets.Interface)]
                public sealed class ServiceAttribute : System.Attribute { }
            }

            namespace ProtoBuf.Grpc.Client
            {
                public static class GrpcClientFactory
                {
                    // fully qualified throughout: inside ProtoBuf.Grpc.*, a bare "Grpc.Core" binds to
                    // ProtoBuf.Grpc.Core, which is the kind of shadowing the generator's own emit avoids
                    // by qualifying everything
                    public static TService CreateGrpcService<TService>(this global::Grpc.Core.CallInvoker client,
                        global::ProtoBuf.Grpc.Configuration.ClientFactory clientFactory = null)
                        where TService : class => null;
                }
            }

            namespace Microsoft.Extensions.DependencyInjection { public interface IServiceCollection { } }

            namespace ProtoBuf.Grpc.Server
            {
                public static class ServicesExtensions
                {
                    public static void AddCodeFirstGrpc(
                        this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services) { }
                }
            }

            [global::ProtoBuf.Grpc.Configuration.Service]
            public interface IGreeter { System.Threading.Tasks.Task<string> HelloAsync(string name); }

            """;

        private Task<System.Collections.Generic.ICollection<Microsoft.CodeAnalysis.Diagnostic>> RunAsync(
            string body, bool publishAot = true)
        {
            if (publishAot) GlobalOptions["build_property.PublishAot"] = "true";
            return AnalyzeAsync(Preamble + body);
        }

        private const string PlainClientCall = """
            public static class Consumer
            {
                public static IGreeter Get(global::Grpc.Core.CallInvoker invoker)
                    => global::ProtoBuf.Grpc.Client.GrpcClientFactory.CreateGrpcService<IGreeter>(invoker);
            }
            """;

        [Fact]
        public async Task PlainCreateGrpcServiceIsReported()
        {
            var diagnostics = await RunAsync(PlainClientCall);
            var single = Assert.Single(diagnostics.Where(x => x.Id == "PBN4015"));
            Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning, single.Severity);
            Assert.Contains("PublishAot", single.GetMessage());
        }

        /// <summary>The server side, which has no client call site to flag at all.</summary>
        [Fact]
        public async Task ServerConfigurationIsReported()
        {
            var diagnostics = await RunAsync("""
                public static class Startup
                {
                    public static void Configure(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)
                        => global::ProtoBuf.Grpc.Server.ServicesExtensions.AddCodeFirstGrpc(services);
                }
                """);

            Assert.Single(diagnostics.Where(x => x.Id == "PBN4015"));
        }

        /// <summary>A call that already passes a factory is the shape we would ask for.</summary>
        [Fact]
        public async Task CallPassingAFactoryIsNotReported()
        {
            var diagnostics = await RunAsync("""
                public static class Consumer
                {
                    public static IGreeter Get(global::Grpc.Core.CallInvoker invoker,
                        global::ProtoBuf.Grpc.Configuration.ClientFactory factory)
                        => global::ProtoBuf.Grpc.Client.GrpcClientFactory.CreateGrpcService<IGreeter>(invoker, factory);
                }
                """);

            Assert.DoesNotContain(diagnostics, x => x.Id == "PBN4015");
        }

        [Fact]
        public async Task NothingIsReportedOnceAProtoGrpcExists()
        {
            var diagnostics = await RunAsync(PlainClientCall + """

                [global::ProtoBuf.Grpc.Configuration.ProtoGrpc]
                public sealed partial class MyServices { }
                """);

            Assert.DoesNotContain(diagnostics, x => x.Id == "PBN4015");
        }

        /// <summary>
        /// The control that makes the rest mean anything: without an AOT or trimming request there is
        /// nothing to say, because the runtime model is a perfectly good way to use protobuf-net.Grpc.
        /// </summary>
        [Fact]
        public async Task NothingIsReportedWithoutAnAotRequest()
        {
            var diagnostics = await RunAsync(PlainClientCall, publishAot: false);
            Assert.DoesNotContain(diagnostics, x => x.Id == "PBN4015");
        }

        /// <summary>
        /// The reason the trigger is consumer-side usage rather than the presence of service contracts:
        /// shipping <c>[Service]</c> interfaces in a shared package is the recommended layout, and such a
        /// package needs no <c>[ProtoGrpc]</c> of its own. Triggering on declarations would nag hardest at
        /// the project laid out correctly.
        /// </summary>
        [Fact]
        public async Task AContractOnlyLibraryIsNotReported()
        {
            // the preamble alone declares [Service] IGreeter and nothing else
            var diagnostics = await RunAsync("");
            Assert.DoesNotContain(diagnostics, x => x.Id == "PBN4015");
        }
    }
}
