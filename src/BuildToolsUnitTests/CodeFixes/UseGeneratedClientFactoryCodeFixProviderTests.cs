using BuildToolsUnitTests.CodeFixes.Abstractions;
using Microsoft.CodeAnalysis.Testing;
using ProtoBuf.BuildTools.Analyzers;
using ProtoBuf.BuildTools.CodeFixes;
using System.Threading.Tasks;
using Xunit;

namespace BuildToolsUnitTests.CodeFixes
{
    /// <summary>
    /// <c>PBN4016</c> and its fix: pass the generated factory to a plain <c>CreateGrpcService</c>.
    /// </summary>
    /// <remarks>
    /// The fix is one argument, which is the point rather than a limitation - the explicit form and the
    /// interceptor produce the same program, so this is the ordinary-C# equivalent of the magic, available
    /// to anyone who has not enabled interceptors.
    /// </remarks>
    public class UseGeneratedClientFactoryCodeFixProviderTests
        : CodeFixProviderTestsBase<UseGeneratedClientFactoryCodeFixProvider>
    {
        /// <summary>
        /// Stubs matched by full name, as the analyzer does; protobuf-net.Grpc cannot be referenced here
        /// because BuildTools compiles protobuf-net.Core's sources in.
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

                [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
                public sealed class ProtoServiceAttribute : System.Attribute
                {
                    public ProtoServiceAttribute(System.Type contract) { }
                }

                [System.AttributeUsage(System.AttributeTargets.Interface)]
                public sealed class ServiceAttribute : System.Attribute { }
            }

            namespace ProtoBuf.Grpc.Client
            {
                public static class GrpcClientFactory
                {
                    public static TService CreateGrpcService<TService>(this global::Grpc.Core.CallInvoker client,
                        global::ProtoBuf.Grpc.Configuration.ClientFactory clientFactory = null)
                        where TService : class => null;
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
        public async Task PlainCallGainsTheFactoryArgument()
        {
            await RunCodeFixTestAsync<GrpcMigrationAnalyzer>(
                Preamble + """
                    public static class Consumer
                    {
                        public static IGreeter Get(global::Grpc.Core.CallInvoker invoker)
                            => global::ProtoBuf.Grpc.Client.GrpcClientFactory.CreateGrpcService<IGreeter>(invoker);
                    }
                    """,
                Preamble + """
                    public static class Consumer
                    {
                        public static IGreeter Get(global::Grpc.Core.CallInvoker invoker)
                            => global::ProtoBuf.Grpc.Client.GrpcClientFactory.CreateGrpcService<IGreeter>(invoker, MyServices.Instance);
                    }
                    """,
                DiagnosticResult
                    .CompilerWarning(GrpcMigrationAnalyzer.CallDoesNotUseGeneratedFactory.Id)
                    .WithSpan(48, 12, 48, 95)
                    .WithArguments("MyServices", "IGreeter"));
        }
    }
}
