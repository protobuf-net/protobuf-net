using ProtoBuf.BuildTools.Analyzers;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BuildToolsUnitTests.Connect
{
    /// <summary>
    /// <c>PBN5007</c>: a contract-first service whose authorization attributes would be silently dropped.
    /// </summary>
    /// <remarks>
    /// The negative cases matter more than the positive one here. A diagnostic about authorization that
    /// fires when it should not is one people turn off, and a turned-off rule protects nothing - so every
    /// way of answering the question is covered, not just the one the docs suggest.
    /// </remarks>
    public class ConnectContractFirstAnalyzerTests : AnalyzerTestBase<ConnectContractFirstAnalyzer>
    {
        public ConnectContractFirstAnalyzerTests(ITestOutputHelper log) : base(log) { }

        /// <summary>
        /// Stubs, matched by full name exactly as the analyzer does - ASP.NET Core is not referenced here.
        /// </summary>
        private const string Preamble = """
            using System;
            using System.Collections.Generic;
            using Microsoft.AspNetCore.Routing;
            using ProtoBuf.Connect.AspNetCore;

            namespace Microsoft.AspNetCore.Authorization
            {
                [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
                public class AuthorizeAttribute : Attribute { public string Policy { get; set; } }

                [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
                public sealed class AllowAnonymousAttribute : Attribute { }
            }

            namespace Microsoft.AspNetCore.Routing
            {
                public interface IEndpointRouteBuilder { }
                public interface IEndpointConventionBuilder { }
            }

            namespace Grpc.Core
            {
                public abstract class ServiceBinderBase { }
                public interface IMethod { }
            }

            namespace ProtoBuf.Connect.AspNetCore
            {
                public interface IConnectServiceBinder<TImplementation> where TImplementation : class { }

                public static class ContractFirstConnectExtensions
                {
                    // the contract-first overload: the generated BindService, as a delegate
                    public static global::Microsoft.AspNetCore.Routing.IEndpointConventionBuilder MapConnectService<TImplementation>(
                        this global::Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints,
                        Action<global::Grpc.Core.ServiceBinderBase, TImplementation> bindService,
                        string routingPrefix = null,
                        Func<global::Grpc.Core.IMethod, IReadOnlyList<object>> metadata = null)
                        where TImplementation : class => null;

                    // the code-first overload, which infers nothing and so has nothing to be wrong about
                    public static global::Microsoft.AspNetCore.Routing.IEndpointConventionBuilder MapConnectService<TImplementation>(
                        this global::Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints,
                        global::ProtoBuf.Connect.AspNetCore.IConnectServiceBinder<TImplementation> binder,
                        string routingPrefix = null)
                        where TImplementation : class => null;
                }
            }

            public static class Conventions
            {
                public static global::Microsoft.AspNetCore.Routing.IEndpointConventionBuilder RequireAuthorization(
                    this global::Microsoft.AspNetCore.Routing.IEndpointConventionBuilder builder, string policy = null)
                    => builder;
            }

            public abstract class GreeterBase
            {
                public virtual string SayHello(string name) => name;
                public virtual string Farewell(string name) => name;
            }

            public static class Greeter
            {
                public static void BindService(global::Grpc.Core.ServiceBinderBase binder, GreeterBase impl) { }
            }

            """;

        private const string Unprotected = """
            public sealed class Plain : GreeterBase { }
            """;

        private const string ProtectedType = """
            [Microsoft.AspNetCore.Authorization.Authorize]
            public sealed class Guarded : GreeterBase { }
            """;

        private const string ProtectedMethod = """
            public sealed class GuardedMethod : GreeterBase
            {
                [Microsoft.AspNetCore.Authorization.Authorize(Policy = "admin")]
                public override string SayHello(string name) => name;
            }
            """;

        private static string Map(string implementation, string arguments = "")
            => $$"""
            public static class Startup
            {
                public static void Configure(global::Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
                    => app.MapConnectService<{{implementation}}>(Greeter.BindService{{arguments}});
            }
            """;

        /// <remarks>
        /// The compile-error assertion is load-bearing, not belt-and-braces. The harness returns
        /// <em>all</em> diagnostics, compiler errors included, so a negative test whose source does not
        /// compile passes vacuously - "no PBN5007 here" is trivially true of a file that was never
        /// analysed. Most of the cases below are negative, so without this the suite could go green while
        /// testing nothing. It has already caught one such case: the stub extension method was not in
        /// scope, and six tests reported that and six did not.
        /// </remarks>
        private async Task<System.Collections.Generic.ICollection<Microsoft.CodeAnalysis.Diagnostic>> RunAsync(string body)
        {
            var diagnostics = await AnalyzeAsync(Preamble + body);

            var errors = diagnostics
                .Where(x => x.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .Select(x => x.ToString())
                .ToList();
            Assert.True(errors.Count == 0, "the fixture did not compile: " + string.Join("; ", errors));

            return diagnostics;
        }

        [Fact]
        public async Task AuthorizeOnTheTypeIsReported()
        {
            var diagnostics = await RunAsync(ProtectedType + Map("Guarded"));
            var single = Assert.Single(diagnostics.Where(x => x.Id == "PBN5007"));
            Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning, single.Severity);
            Assert.Contains("Guarded", single.GetMessage());
        }

        [Fact]
        public async Task AuthorizeOnAMethodIsReported()
        {
            var diagnostics = await RunAsync(ProtectedMethod + Map("GuardedMethod"));
            var single = Assert.Single(diagnostics.Where(x => x.Id == "PBN5007"));
            Assert.Contains("one of its methods", single.GetMessage());
        }

        /// <summary>
        /// Inherited from the generated base, which is where a shared contract would put it.
        /// </summary>
        [Fact]
        public async Task AuthorizeOnTheBaseIsReported()
        {
            var diagnostics = await RunAsync("""
                [Microsoft.AspNetCore.Authorization.Authorize]
                public abstract class GuardedBase : GreeterBase { }
                public sealed class FromBase : GuardedBase { }
                """ + Map("FromBase"));

            Assert.Single(diagnostics.Where(x => x.Id == "PBN5007"));
        }

        /// <summary>
        /// A policy is normally carried by a derived attribute rather than the bare one.
        /// </summary>
        [Fact]
        public async Task ADerivedAuthorizeAttributeIsReported()
        {
            var diagnostics = await RunAsync("""
                public sealed class AdminOnlyAttribute : Microsoft.AspNetCore.Authorization.AuthorizeAttribute { }

                [AdminOnly]
                public sealed class Derived : GreeterBase { }
                """ + Map("Derived"));

            Assert.Single(diagnostics.Where(x => x.Id == "PBN5007"));
        }

        /// <summary>
        /// <c>[AllowAnonymous]</c> counts: dropped, it makes an endpoint less reachable rather than more,
        /// which is a different bug and equally invisible.
        /// </summary>
        [Fact]
        public async Task AllowAnonymousIsReported()
        {
            var diagnostics = await RunAsync("""
                [Microsoft.AspNetCore.Authorization.AllowAnonymous]
                public sealed class Open : GreeterBase { }
                """ + Map("Open"));

            Assert.Single(diagnostics.Where(x => x.Id == "PBN5007"));
        }

        [Fact]
        public async Task NothingIsReportedWithoutAuthorizationAttributes()
        {
            var diagnostics = await RunAsync(Unprotected + Map("Plain"));
            Assert.DoesNotContain(diagnostics, x => x.Id == "PBN5007");
        }

        [Fact]
        public async Task SupplyingMetadataAnswersIt()
        {
            var diagnostics = await RunAsync(ProtectedType + Map("Guarded", ", metadata: _ => null"));
            Assert.DoesNotContain(diagnostics, x => x.Id == "PBN5007");
        }

        /// <summary>Positionally, too - the argument is matched by parameter, not by position.</summary>
        [Fact]
        public async Task SupplyingMetadataPositionallyAnswersIt()
        {
            var diagnostics = await RunAsync(ProtectedType + Map("Guarded", ", null, _ => null"));
            Assert.DoesNotContain(diagnostics, x => x.Id == "PBN5007");
        }

        /// <summary>A routing prefix is not an answer; only the metadata argument is.</summary>
        [Fact]
        public async Task APrefixAloneIsStillReported()
        {
            var diagnostics = await RunAsync(ProtectedType + Map("Guarded", ", \"rpc\""));
            Assert.Single(diagnostics.Where(x => x.Id == "PBN5007"));
        }

        /// <summary>The ASP.NET Core idiom, on the builder we hand back.</summary>
        [Fact]
        public async Task ChainingAConventionAnswersIt()
        {
            var diagnostics = await RunAsync(ProtectedType + """
                public static class Startup
                {
                    public static void Configure(global::Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
                        => app.MapConnectService<Guarded>(Greeter.BindService).RequireAuthorization();
                }
                """);

            Assert.DoesNotContain(diagnostics, x => x.Id == "PBN5007");
        }

        /// <summary>
        /// The code-first overload takes a binder rather than a delegate, and its metadata comes from the
        /// generator - so it is not this rule's business.
        /// </summary>
        [Fact]
        public async Task TheCodeFirstOverloadIsNotReported()
        {
            var diagnostics = await RunAsync(ProtectedType + """
                public static class Startup
                {
                    public static void Configure(
                        global::Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app,
                        global::ProtoBuf.Connect.AspNetCore.IConnectServiceBinder<Guarded> binder)
                        => app.MapConnectService<Guarded>(binder);
                }
                """);

            Assert.DoesNotContain(diagnostics, x => x.Id == "PBN5007");
        }

        /// <summary>The switch that declines all of this.</summary>
        [Fact]
        public async Task NothingIsReportedWhenBuildToolsAreDisabled()
        {
            GlobalOptions["build_property.ProtoBufDisableBuildTools"] = "true";
            var diagnostics = await RunAsync(ProtectedType + Map("Guarded"));
            Assert.DoesNotContain(diagnostics, x => x.Id == "PBN5007");
        }
    }
}
