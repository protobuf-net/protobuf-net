#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ProtoBuf.BuildTools.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BuildToolsUnitTests.Grpc
{
    public class GrpcProxyGeneratorTests : GrpcGeneratorTestBase
    {
        public GrpcProxyGeneratorTests(ITestOutputHelper log) : base(log) { }

        /// <summary>
        /// The whole emitted surface for every operation shape, pinned as a golden file - and compiled,
        /// so a signature that doesn't line up with protobuf-net.Grpc fails here rather than in a
        /// consumer's build.
        /// </summary>
        [Fact]
        public void EmitsProxiesForEveryOperationShape()
            => ExecuteFixture("Contracts").AssertClean();

        [Fact]
        public void NestedContractIsReportedAndSkipped()
        {
            var result = ExecuteFixture("Diagnostics/Nested").AssertCompiles().AssertNoOutput();
            var diagnostic = result.AssertSingleDiagnostic("PBN3001");
            Assert.Contains("INestedService", diagnostic.GetMessage());
        }

        [Fact]
        public void GenericContractIsReportedAndSkipped()
        {
            var result = ExecuteFixture("Diagnostics/Generic").AssertCompiles().AssertNoOutput();
            result.AssertSingleDiagnostic("PBN3003");
        }

        /// <summary>
        /// One unsupported operation takes the whole contract out: the runtime path covers a wider set
        /// of shapes, so a proxy with missing members would be a regression, not a partial win.
        /// </summary>
        [Fact]
        public void UnsupportedOperationTakesOutTheWholeContract()
        {
            var result = ExecuteFixture("Diagnostics/UnsupportedShape").AssertCompiles().AssertNoOutput();
            var diagnostic = result.AssertSingleDiagnostic("PBN3002");
            Assert.Contains("TwoPayloadsAsync", diagnostic.GetMessage());
        }

        /// <summary>
        /// The runtime binds an inherited interface only when it is marked [SubService]; for any other
        /// base it emits a throwing stub and binds nothing. Generating a binding there would put a
        /// method on the wire that the runtime path never serves.
        /// </summary>
        [Fact]
        public void PlainBaseInterfaceTakesOutTheWholeContract()
        {
            var result = ExecuteFixture("Diagnostics/PlainBaseInterface").AssertCompiles().AssertNoOutput();
            var diagnostic = result.AssertSingleDiagnostic("PBN3005");
            Assert.Contains("IPlainBase", diagnostic.GetMessage());
        }

        /// <summary>
        /// The runtime path recognises shapes this generator doesn't emit (observables, streams, raw
        /// Grpc.Core call types). Those must be recognised as *not ours* rather than mistaken for an
        /// ordinary payload, which would compile and then fail looking for a marshaller at startup.
        /// </summary>
        [Fact]
        public void RuntimeOnlyShapesAreLeftToTheRuntime()
        {
            var result = ExecuteFixture("Diagnostics/RuntimeOnlyShapes").AssertCompiles().AssertNoOutput();
            Assert.All(result.GeneratorDiagnostics, static diagnostic => Assert.Equal("PBN3002", diagnostic.Id));
            Assert.Equal(4, result.GeneratorDiagnostics.Length);
        }

        /// <summary>
        /// The generator ships in protobuf-net.BuildTools, which versions independently of
        /// protobuf-net.Grpc; when the runtime half isn't there, standing down silently is the only
        /// safe behaviour - emitting calls into a missing API would bury the real problem under
        /// generated-code errors.
        /// </summary>
        [Fact]
        public void WithoutTheRuntimeApiNothingIsEmittedOrReported()
        {
            const string Source = @"
using System.Threading.Tasks;
namespace ProtoBuf.Grpc.Configuration
{
    // an older protobuf-net.Grpc: [Service] exists, the generated-proxy API does not
    public sealed class ServiceAttribute : System.Attribute { }
}
namespace MyApp
{
    public class Request { }
    public class Response { }

    [ProtoBuf.Grpc.Configuration.Service]
    public interface IMyService
    {
        Task<Response> UnaryAsync(Request request);
    }
}";
            Execute(Source, includeContractSurface: false).AssertClean().AssertNoOutput();
        }

        /// <summary>
        /// A plain WCF contract in a project that has never heard of protobuf-net.Grpc must not draw
        /// diagnostics, let alone generated code.
        /// </summary>
        [Fact]
        public void PlainWcfContractIsLeftAlone()
        {
            const string Source = @"
using System.Threading.Tasks;
namespace System.ServiceModel
{
    public sealed class ServiceContractAttribute : System.Attribute { }
}
namespace MyApp
{
    public class Request { }
    public class Response { }

    [System.ServiceModel.ServiceContract]
    public interface IMyService
    {
        Task<Response> UnaryAsync(Request request);
    }
}";
            Execute(Source, includeContractSurface: false).AssertClean().AssertNoOutput();
        }

        /// <summary>
        /// [ServiceContract] is honoured when protobuf-net.Grpc *is* present, and an interface carrying
        /// both markers is generated exactly once (both attributes feed the generator).
        /// </summary>
        [Theory]
        [InlineData("[ServiceContract]")]
        [InlineData("[Service]")]
        [InlineData("[Service, ServiceContract]")]
        public void ServiceContractAndServiceBothTrigger(string attributes)
        {
            var result = Execute(WcfAware(attributes)).AssertClean();
            Assert.Single(HintNames(result.GeneratedCode));
            Assert.Contains("MyApp_IMyService_ClientProxy", result.GeneratedCode);
        }

        /// <summary>
        /// An explicit [Proxy(typeof(...))] is the user saying "use mine".
        /// </summary>
        [Fact]
        public void ExplicitProxyAttributeIsRespected()
        {
            const string Source = @"
using Grpc.Core;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using System.Threading.Tasks;
namespace MyApp
{
    public class Request { }
    public class Response { }

    [Service, Proxy(typeof(MyProxy))]
    public interface IMyService
    {
        Task<Response> UnaryAsync(Request request, CallContext context = default);
    }

    public class MyProxy : IMyService
    {
        public MyProxy(CallInvoker callInvoker) { }
        public Task<Response> UnaryAsync(Request request, CallContext context = default) => null!;
    }
}";
            Execute(Source).AssertClean().AssertNoOutput();
        }

        /// <summary>
        /// The emitted code is nullable-annotated, so C# 7.3 consumers keep the runtime path; that is
        /// worth saying out loud, because it is silent otherwise.
        /// </summary>
        [Fact]
        public void BelowCSharp8TheContractIsReportedAndSkipped()
        {
            // every tree in a compilation must share one language version, and the real contract surface
            // is C# 8+; all the generator probes for is that these three types exist, so a 7.3-parsable
            // stand-in is enough to get past the runtime-support check and reach the version check
            const string Source = @"
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using System;
using System.Threading.Tasks;
namespace Grpc.Core
{
    public abstract class ClientBase { }
}
namespace ProtoBuf.Grpc
{
    public struct CallContext { }
}
namespace ProtoBuf.Grpc.Configuration
{
    public sealed class ServiceAttribute : Attribute { }
    public interface IServerMethodBinder<TService> where TService : class { }
}
namespace ProtoBuf.Grpc.Internal
{
    public static class GeneratedProxyRegistry { }
}
namespace MyApp
{
    public class Request { }
    public class Response { }

    [Service]
    public interface IMyService
    {
        Task<Response> UnaryAsync(Request request, CallContext context);
    }
}";
            var result = Execute(Source, includeContractSurface: false, languageVersion: LanguageVersion.CSharp7_3);
            result.AssertNoOutput();
            result.AssertSingleDiagnostic("PBN3000");
        }

        /// <summary>
        /// The emit decision itself, exhaustively: the environment cases (an old protobuf-net.Grpc, a
        /// target framework without [ModuleInitializer]) can't be staged through a reference set on the
        /// test host, but they are exactly the cases that only appear once this ships separately from
        /// the runtime, so the decision is asserted directly.
        /// </summary>
        [Theory]
        // runtime, moduleInit, langVer,                     emits, reported
        [InlineData(true, true, LanguageVersion.CSharp8, true, null)]
        [InlineData(true, true, LanguageVersion.CSharp10, true, null)]
        [InlineData(false, true, LanguageVersion.CSharp10, false, null)]      // no runtime API: silent
        [InlineData(false, false, LanguageVersion.CSharp7_3, false, null)]    // ... even when everything else is wrong too
        [InlineData(true, true, LanguageVersion.CSharp7_3, false, "PBN3000")]
        [InlineData(true, false, LanguageVersion.CSharp10, false, "PBN3004")]
        public void EmitDecisionCoversTheEnvironment(bool hasRuntimeSupport, bool hasModuleInitializer, LanguageVersion languageVersion, bool expectEmit, string? expectReported)
        {
            var emits = GrpcProxyGenerator.CanEmit(hasRuntimeSupport, hasModuleInitializer, languageVersion, out var blocker);

            Assert.Equal(expectEmit, emits);
            Assert.Equal(expectReported, blocker?.Id);
        }

        /// <summary>
        /// Contracts are parsed per-interface so that editing one doesn't re-emit the rest of the
        /// project; that only holds while the model compares by value, and a caching regression is
        /// invisible (everything still works, just slower), so it needs asserting explicitly.
        /// </summary>
        [Fact]
        public void ContractModelIsCachedWhenNothingRelevantChanges()
        {
            var reasons = RunWithEdit(static source => source + "\n// a comment, changing nothing that matters\n");

            Assert.NotEmpty(reasons);
            Assert.All(reasons, reason => Assert.True(
                reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged,
                $"contract step reported {reason}; the model is not comparing equal across irrelevant edits"));
        }

        [Fact]
        public void ContractModelIsNotCachedWhenTheContractActuallyChanges()
        {
            // the counterpart to the test above: an equality implementation that reported "equal" for
            // everything would pass that one and nobody would notice
            var reasons = RunWithEdit(static source => source.Replace("UnaryAsync(Request request", "UnaryAsync(Response request"));
            Assert.Contains(IncrementalStepRunReason.Modified, reasons);
        }

        /// <summary>
        /// Run the generator, then again over an edited version of the same file, and report why the
        /// contract step produced what it produced the second time.
        /// </summary>
        /// <remarks>
        /// The edit replaces the contract's *own* syntax tree rather than adding an unrelated one:
        /// adding a tree leaves the step trivially cached, which would prove nothing.
        /// </remarks>
        private List<IncrementalStepRunReason> RunWithEdit(Func<string, string> edit)
        {
            var source = WcfAware("[Service]");
            var parseOptions = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse);
            var compilation = Compile(source, parseOptions);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                new[] { new GrpcProxyGenerator().AsSourceGenerator() },
                parseOptions: parseOptions,
                optionsProvider: null,
                driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None,
                    trackIncrementalGeneratorSteps: true));

            driver = driver.RunGenerators(compilation);

            var edited = compilation.ReplaceSyntaxTree(
                compilation.SyntaxTrees.First(),
                CSharpSyntaxTree.ParseText(edit(source), parseOptions, path: "input.cs"));
            driver = driver.RunGenerators(edited);

            var results = driver.GetRunResult().Results.Single();
            Assert.True(results.TrackedSteps.ContainsKey(GrpcProxyGenerator.ContractTrackingName),
                $"no tracked step named '{GrpcProxyGenerator.ContractTrackingName}'");

            return (from step in results.TrackedSteps[GrpcProxyGenerator.ContractTrackingName]
                    from output in step.Outputs
                    select output.Reason).ToList();
        }

        private static string[] HintNames(string generatedCode)
            => generatedCode.Split('\n').Where(static line => line.StartsWith("// ---- ")).ToArray();

        /// <summary>
        /// A single contract, attributed as the caller asks, with the WCF marker available.
        /// </summary>
        private static string WcfAware(string attributes) => @"
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using System.ServiceModel;
using System.Threading.Tasks;
namespace System.ServiceModel
{
    [AttributeUsage(AttributeTargets.Interface)]
    public sealed class ServiceContractAttribute : Attribute { }
}
namespace MyApp
{
    public class Request { }
    public class Response { }

    " + attributes + @"
    public interface IMyService
    {
        Task<Response> UnaryAsync(Request request, CallContext context = default);
    }
}";
    }
}
