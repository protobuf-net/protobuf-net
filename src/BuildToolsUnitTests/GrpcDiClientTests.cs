using ProtoBuf.BuildTools.Analyzers;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BuildToolsUnitTests
{
    /// <summary>
    /// <c>PBN4017</c>: clients registered with <c>AddCodeFirstGrpcClient</c> take their factory from the
    /// container, so a model that is never registered there is never used by them.
    /// </summary>
    /// <remarks>
    /// The mainstream ASP.NET Core way to get a gRPC client, and the one route where there is neither an
    /// argument to fix nor a call site to intercept - the seam is DI. One registration covers every client
    /// in the container, which is why the suggestion names a single line.
    /// </remarks>
    public class GrpcDiClientTests : AnalyzerTestBase<GrpcMigrationAnalyzer>
    {
        public GrpcDiClientTests(ITestOutputHelper log) : base(log) { }

        private const string Preamble = """
            namespace Microsoft.Extensions.DependencyInjection
            {
                public interface IServiceCollection { }
                public interface IHttpClientBuilder { }

                public static class Registrations
                {
                    public static IServiceCollection AddSingleton<T>(this IServiceCollection services, T instance)
                        => services;
                }
            }

            namespace ProtoBuf.Grpc.Configuration
            {
                public abstract class ClientFactory { }

                [System.AttributeUsage(System.AttributeTargets.Class)]
                public sealed class ProtoGrpcAttribute : System.Attribute { }

                [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
                public sealed class ProtoServiceAttribute : System.Attribute
                {
                    public ProtoServiceAttribute(System.Type contract) { }
                }

                [System.AttributeUsage(System.AttributeTargets.Interface)]
                public sealed class ServiceAttribute : System.Attribute { }
            }

            namespace ProtoBuf.Grpc.ClientFactory
            {
                using global::Microsoft.Extensions.DependencyInjection;

                public static class ServicesExtensions
                {
                    public static IHttpClientBuilder AddCodeFirstGrpcClient<T>(this IServiceCollection services)
                        where T : class => null;
                }
            }

            [global::ProtoBuf.Grpc.Configuration.Service]
            public interface IGreeter { string Hello(); }

            [global::ProtoBuf.Grpc.Configuration.ProtoGrpc]
            [global::ProtoBuf.Grpc.Configuration.ProtoService(typeof(IGreeter))]
            public sealed partial class MyServices : global::ProtoBuf.Grpc.Configuration.ClientFactory
            {
                public static MyServices Instance => null;
            }

            """;

        [Fact]
        public async Task DiRegisteredClientWithoutTheFactoryIsReported()
        {
            var diagnostics = await AnalyzeAsync(Preamble + """
                public static class Startup
                {
                    public static void Configure(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)
                        => global::ProtoBuf.Grpc.ClientFactory.ServicesExtensions.AddCodeFirstGrpcClient<IGreeter>(services);
                }
                """);

            var single = Assert.Single(diagnostics.Where(x => x.Id == "PBN4017"));
            Assert.Contains("MyServices", single.GetMessage());
            Assert.Contains("AddSingleton<ClientFactory>", single.GetMessage());
        }

        /// <summary>
        /// The suppression: someone who has already registered it must not be nagged. Deliberately
        /// biased this way - the check is a dynamic question answered statically, so it errs toward
        /// silence rather than toward noise.
        /// </summary>
        [Fact]
        public async Task NothingIsReportedOnceTheFactoryIsRegistered()
        {
            var diagnostics = await AnalyzeAsync(Preamble + """
                public static class Startup
                {
                    public static void Configure(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)
                    {
                        services.AddSingleton<global::ProtoBuf.Grpc.Configuration.ClientFactory>(MyServices.Instance);
                        global::ProtoBuf.Grpc.ClientFactory.ServicesExtensions.AddCodeFirstGrpcClient<IGreeter>(services);
                    }
                }
                """);

            Assert.DoesNotContain(diagnostics, x => x.Id == "PBN4017");
        }

        /// <summary>A contract no model covers has nothing to suggest.</summary>
        [Fact]
        public async Task AnUncoveredContractIsNotReported()
        {
            var diagnostics = await AnalyzeAsync(Preamble + """
                [global::ProtoBuf.Grpc.Configuration.Service]
                public interface IOther { string Hello(); }

                public static class Startup
                {
                    public static void Configure(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)
                        => global::ProtoBuf.Grpc.ClientFactory.ServicesExtensions.AddCodeFirstGrpcClient<IOther>(services);
                }
                """);

            Assert.DoesNotContain(diagnostics, x => x.Id == "PBN4017");
        }
    }
}
