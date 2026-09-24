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
        /// Nullable is <b>enabled</b> for these compilations, and that is load-bearing rather than
        /// tidiness.
        /// </summary>
        /// <remarks>
        /// The shipped protobuf-net.Grpc annotates <c>CreateGrpcService</c>'s factory parameter, and a
        /// metadata annotation is honoured whatever the consumer's context - so Roslyn reports that
        /// parameter's type as <c>ClientFactory?</c>. A stub declared in a nullable-<em>disabled</em>
        /// compilation has its <c>?</c> erased, which is exactly why PBN4016's false positive was
        /// invisible here while reproducing in every real project.
        /// </remarks>
        protected override Microsoft.CodeAnalysis.Project SetupProject(Microsoft.CodeAnalysis.Project project)
            => project.WithCompilationOptions(
                ((Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions)project.CompilationOptions!)
                    .WithNullableContextOptions(Microsoft.CodeAnalysis.NullableContextOptions.Enable));

        /// <summary>
        /// Stubs, matched by full name exactly as the analyzer does - protobuf-net.Grpc cannot be
        /// referenced here, because BuildTools compiles protobuf-net.Core's sources in and every type in
        /// Core would become ambiguous.
        /// </summary>
        private const string Preamble = """
            // extension methods need their namespace in scope, and a using must precede everything
            // else in the file - so it lives here rather than in each body. Without it the REDUCED
            // form does not compile at all, and a test asserting "no diagnostic" then passes because
            // the compilation is broken rather than because the analyzer is quiet.
            using ProtoBuf.Grpc.Client;

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

                    // The ChannelBase overload, and NULLABLE-ANNOTATED as the shipped package's is.
                    // Both details are load-bearing: the annotation is what made PBN4016 fire on a
                    // call that already passed a factory, because the analyzer compared the parameter
                    // type by *display string* and metadata renders it "ClientFactory?".
                    #nullable enable
                    public static TService CreateGrpcService<TService>(this global::Grpc.Core.ChannelBase channel,
                        global::ProtoBuf.Grpc.Configuration.ClientFactory? clientFactory = null)
                        where TService : class => null!;
                    #nullable disable
                }
            }

            namespace Grpc.Core { public class ChannelBase { } }

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


        /// <summary>
        /// A <c>[ProtoGrpc]</c> container, which is what <c>PBN4016</c> needs before it fires at all.
        /// </summary>
        private const string WithContainer = """
            namespace ProtoBuf.Grpc.Configuration
            {
                [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
                public sealed class ProtoServiceAttribute : System.Attribute
                {
                    public ProtoServiceAttribute(System.Type contract) { }
                }
            }

            [global::ProtoBuf.Grpc.Configuration.ProtoGrpc]
            [global::ProtoBuf.Grpc.Configuration.ProtoService(typeof(IGreeter))]
            public sealed partial class MyServices : global::ProtoBuf.Grpc.Configuration.ClientFactory
            {
                public static MyServices Instance => null;
            }

            """;

        /// <summary>
        /// <c>PBN4016</c> must stay silent when the factory is passed - in <b>either</b> call form.
        /// </summary>
        /// <remarks>
        /// The extension-method spelling is the one people actually write
        /// (<c>channel.CreateGrpcService&lt;T&gt;(factory)</c>) and the one that was never covered: the
        /// existing "factory is passed" test uses the unreduced static form and asserts on PBN4015, so
        /// a false positive here went unnoticed until a project without interceptors enabled hit it.
        /// </remarks>
        [Theory]
        [InlineData("global::ProtoBuf.Grpc.Client.GrpcClientFactory.CreateGrpcService<IGreeter>(invoker, MyServices.Instance)")]
        [InlineData("invoker.CreateGrpcService<IGreeter>(MyServices.Instance)")]
        [InlineData("channel.CreateGrpcService<IGreeter>(MyServices.Instance)")]
        public async Task FactoryPassedIsNotReportedInEitherCallForm(string call)
        {
            var diagnostics = await RunAsync(WithContainer + $$"""
                public static class Consumer
                {
                    public static IGreeter Get(global::Grpc.Core.CallInvoker invoker,
                        global::Grpc.Core.ChannelBase channel)
                        => {{call}};
                }
                """);

            Assert.DoesNotContain(diagnostics, x => x.Id == "PBN4016");
        }


        /// <summary>Positive control: without a factory, PBN4016 must fire - else the tests above are vacuous.</summary>
        [Theory]
        [InlineData("global::ProtoBuf.Grpc.Client.GrpcClientFactory.CreateGrpcService<IGreeter>(invoker)")]
        [InlineData("invoker.CreateGrpcService<IGreeter>()")]
        [InlineData("channel.CreateGrpcService<IGreeter>()")]
        public async Task PlainCallIsReportedInEveryCallForm(string call)
        {
            var diagnostics = await RunAsync(WithContainer + $$"""
                public static class Consumer
                {
                    public static IGreeter Get(global::Grpc.Core.CallInvoker invoker,
                        global::Grpc.Core.ChannelBase channel)
                        => {{call}};
                }
                """);

            Assert.Contains(diagnostics, x => x.Id == "PBN4016");
        }

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
