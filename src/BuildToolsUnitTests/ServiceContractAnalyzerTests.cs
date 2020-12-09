using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Analyzers;
using System.Globalization;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BuildToolsUnitTests
{
    public class ServiceAnalyzerTests : AnalyzerTestBase<ServiceContractAnalyzer>
    {
        public ServiceAnalyzerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [Fact]
        public async Task CleanForUnrelatedContract()
        {
            var diagnostics = await AnalyzeAsync(@"
public interface IMyService
{
    public void Bar();
}");
            Assert.Empty(diagnostics);
        }

        protected override Project SetupProject(Project project)
        {
            // add a modified snapshot from https://github.com/protobuf-net/protobuf-net.Grpc/blob/main/src/protobuf-net.Grpc/Configuration/ServiceAttribute.cs
            // since we can't add a project reference without getting into a complete mess
            return project.AddDocument("ServiceAttribute.cs", @"
using System;

namespace ProtoBuf.Grpc.Configuration
{
    /// <summary>
    /// Explicitly indicates that an interface represents a gRPC service
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public sealed class ServiceAttribute : Attribute
    {
        /// <summary>
        /// The name of the service
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Create a new instance of the attribute
        /// </summary>
        public ServiceAttribute(string name = null)
            => Name = name;
    }
}").Project;
        }

        [Fact]
        public async Task DetectInvalidMethodKind()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf.Grpc.Configuration;
using ProtoBuf;
namespace SomeNamespace.Whatever
{
    public static class ContainingType
    {
        [Service]
        public interface IMyService
        {
            Foo Property {get;}
        }
        [ProtoContract]
        public class Foo {}
    }
}");
            var err = Assert.Single(diagnostics);
            Assert.Equal(ServiceContractAnalyzer.InvalidMemberKind, err.Descriptor);
            Assert.Equal(DiagnosticSeverity.Error, err.Severity);
            Assert.Equal("This member is not a method; only methods are supported for gRPC services.", err.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task ValidMethodKindIsClean()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf.Grpc.Configuration;
using ProtoBuf;

[Service]
public interface IMyService
{
    Foo BasicMethod(Foo value);
}
[ProtoContract]
public class Foo {}");

            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task GenericMethodDetected()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf.Grpc.Configuration;
using ProtoBuf;

[Service]
public interface IMyService
{
    Foo BasicMethod<T>(Foo value);
}
[ProtoContract]
public class Foo {}");

            var err = Assert.Single(diagnostics);
            Assert.Equal(ServiceContractAnalyzer.GenericMethod, err.Descriptor);
            Assert.Equal(DiagnosticSeverity.Error, err.Severity);
            Assert.Equal("The gRPC method can not be generic.", err.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task GenericServiceDetected()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf.Grpc.Configuration;
using ProtoBuf;

[Service]
public interface IMyService<T>
{
    Foo BasicMethod(Foo value);
}
[ProtoContract]
public class Foo {}");

            var err = Assert.Single(diagnostics);
            Assert.Equal(ServiceContractAnalyzer.GenericService, err.Descriptor);
            Assert.Equal(DiagnosticSeverity.Error, err.Severity);
            Assert.Equal("The gRPC service can not be generic.", err.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task NestedGenericServiceDetected()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf.Grpc.Configuration;
using ProtoBuf;

public class Foo<T> where T : class
{
    [Service]
    public interface IMyService
    {
        Foo BasicMethod(Foo value);
    }
}
[ProtoContract]
public class Foo {}");

            var err = Assert.Single(diagnostics);
            Assert.Equal(ServiceContractAnalyzer.GenericService, err.Descriptor);
            Assert.Equal(DiagnosticSeverity.Error, err.Severity);
            Assert.Equal("The gRPC service can not be generic.", err.GetMessage(CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData("int")]
        [InlineData("Task<int>")]
        [InlineData("Task<Task<Foo>>")]
        [InlineData("ValueTask<ValueTask<Foo>>")]
        [InlineData("ValueTask<int>")]
        [InlineData("IAsyncEnumerable<int>")]
        public async Task DetectInvalidReturnTypes(string kind)
        {
            var diagnostics = await AnalyzeAsync($@"
#pragma warning disable CS8019
using ProtoBuf.Grpc.Configuration;
using ProtoBuf;
using System.Collections.Generic;
using System.Threading.Tasks;

[Service]
public interface IMyService
{{
    {kind} BasicMethod(Foo value);
}}
[ProtoContract]
public class Foo {{}}");

            var err = Assert.Single(diagnostics);
            Assert.Equal(ServiceContractAnalyzer.InvalidReturnType, err.Descriptor);
            Assert.Equal(DiagnosticSeverity.Error, err.Severity);
            Assert.Equal("The return value of a gRPC method must currently be Void, a reference-type data contract, or an task / async sequence of the same.", err.GetMessage(CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData("Foo foo")]
        [InlineData("IAsyncEnumerable<Foo> foos")]
        [InlineData("")]
        public async Task DetectValidParameterKinds(string? kind)
        {
            var diagnostics = await AnalyzeAsync($@"
#pragma warning disable CS8019
using ProtoBuf.Grpc.Configuration;
using ProtoBuf;
using System.Collections.Generic;
using System.Threading.Tasks;

[Service]
public interface IMyService
{{
    ValueTask<Foo> BasicMethod({kind});
}}
[ProtoContract]
public class Foo {{}}");

            Assert.Empty(diagnostics);
        }

        [Theory]
        [InlineData("ValueTask pending")]
        [InlineData("Task pending")]
        [InlineData("int value")]
        [InlineData("string value")]
        public async Task DetectInvalidParameterKinds(string? kind)
        {
            var diagnostics = await AnalyzeAsync($@"
#pragma warning disable CS8019
using ProtoBuf.Grpc.Configuration;
using ProtoBuf;
using System.Threading.Tasks;

[Service]
public interface IMyService
{{
    Foo BasicMethod({kind});
}}
[ProtoContract]
public class Foo {{}}");

            var err = Assert.Single(diagnostics);
            Assert.Equal(ServiceContractAnalyzer.InvalidPayloadType, err.Descriptor);
            Assert.Equal(DiagnosticSeverity.Error, err.Severity);
            Assert.Equal("The data parameter of a gRPC method must currently be Void, a reference-type data contract, or an async sequence of the same.", err.GetMessage(CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData("Foo")]
        [InlineData("IAsyncEnumerable<Foo>")]
        [InlineData("void")]
        [InlineData("Task")]
        [InlineData("ValueTask")]
        public async Task DetectValidReturnKinds(string? kind)
        {
            var diagnostics = await AnalyzeAsync($@"
#pragma warning disable CS8019
using ProtoBuf.Grpc.Configuration;
using ProtoBuf;
using System.Collections.Generic;
using System.Threading.Tasks;

[Service]
public interface IMyService
{{
    {kind} BasicMethod(Foo foo);
}}
[ProtoContract]
public class Foo {{}}");

            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task DetectMultipleContext()
        {
            var diagnostics = await AnalyzeAsync(@"
#pragma warning disable CS8019
using ProtoBuf.Grpc.Configuration;
using ProtoBuf;
using ProtoBuf.Grpc;
using System.Threading.Tasks;
using System.Threading;

[Service]
public interface IMyService
{
    Foo BasicMethod(CancellationToken x, CallContext y);
}
[ProtoContract]
public class Foo {}

// hacking this in
namespace ProtoBuf.Grpc {
    public struct CallContext {}
}");

            var err = Assert.Single(diagnostics);
            Assert.Equal(ServiceContractAnalyzer.InvalidPayloadType, err.Descriptor);
            Assert.Equal(DiagnosticSeverity.Error, err.Severity);
            Assert.Equal("The data parameter of a gRPC method must currently be Void, a reference-type data contract, or an async sequence of the same.", err.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task DetectMultipleDataParameter_NoContext()
        {
            var diagnostics = await AnalyzeAsync(@"
#pragma warning disable CS8019
using ProtoBuf.Grpc.Configuration;
using ProtoBuf;
using System.Threading.Tasks;

[Service]
public interface IMyService
{
    Foo BasicMethod(Foo x, Foo y);
}
[ProtoContract]
public class Foo {}");

            var err = Assert.Single(diagnostics);
            Assert.Equal(ServiceContractAnalyzer.InvalidParameters, err.Descriptor);
            Assert.Equal(DiagnosticSeverity.Error, err.Severity);
            Assert.Equal("Invalid signature; gRPC methods expect a single optional payload and a single optional context.", err.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task DetectMultipleDataParameter_Context()
        {
            var diagnostics = await AnalyzeAsync(@"
#pragma warning disable CS8019
using ProtoBuf.Grpc.Configuration;
using ProtoBuf;
using System.Threading.Tasks;
using System.Threading;

[Service]
public interface IMyService
{
    Foo BasicMethod(Foo x, Foo y, CancellationToken token);
}
[ProtoContract]
public class Foo {}");

            var err = Assert.Single(diagnostics);
            Assert.Equal(ServiceContractAnalyzer.InvalidParameters, err.Descriptor);
            Assert.Equal(DiagnosticSeverity.Error, err.Severity);
            Assert.Equal("Invalid signature; gRPC methods expect a single optional payload and a single optional context.", err.GetMessage(CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData("CallOptions")]
        [InlineData("ServerCallContext")]
        public async Task DetectInvalidContext(string kind)
        {
            var diagnostics = await AnalyzeAsync($@"
#pragma warning disable CS8019
using ProtoBuf.Grpc.Configuration;
using Grpc.Core;
using ProtoBuf;
using System.Threading.Tasks;

[Service]
public interface IMyService
{{
    Foo BasicMethod(Foo x, {kind} ctx);
}}
[ProtoContract]
public class Foo {{}}

// hacking these in
namespace Grpc.Core {{
    public struct CallOptions {{}}
    public class ServerCallContext {{}}
}}
");

            var err = Assert.Single(diagnostics);
            Assert.Equal(ServiceContractAnalyzer.InvalidContextType, err.Descriptor);
            Assert.Equal(DiagnosticSeverity.Error, err.Severity);
            Assert.Equal("The context parameter of a gRPC method must be CallContext or CancellationToken.", err.GetMessage(CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData("CancellationToken")]
        [InlineData("CallContext")]
        public async Task DetectValidContext(string kind)
        {
            var diagnostics = await AnalyzeAsync($@"
#pragma warning disable CS8019
using ProtoBuf.Grpc.Configuration;
using ProtoBuf;
using ProtoBuf.Grpc;
using System.Threading.Tasks;
using System.Threading;

[Service]
public interface IMyService
{{
    Foo BasicMethod(Foo x, {kind} ctx);
}}
[ProtoContract]
public class Foo {{}}

// hacking this in
namespace ProtoBuf.Grpc {{
    public struct CallContext {{}}
}}");

            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task DetectNonSerializable_Both()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf.Grpc.Configuration;

[Service]
public interface IMyService
{
    Foo SomeMethod(Foo value);
}
public class Foo {}
");
            var err = Assert.Single(diagnostics); // assert not double-reported
            Assert.Equal(ServiceContractAnalyzer.PossiblyNotSerializable, err.Descriptor);
            Assert.Equal(DiagnosticSeverity.Warning, err.Severity);
            Assert.Equal("gRPC methods require inputs/outputs that can be marshalled with gRPC; this type *may* be usable with gRPC, but it could not be verified.", err.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task DetectNonSerializable_Payload()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf.Grpc.Configuration;
using ProtoBuf;
[Service]
public interface IMyService
{
    Bar SomeMethod(Foo value);
}
public class Foo {}
[ProtoContract]
public class Bar {}
");
            var err = Assert.Single(diagnostics);
            Assert.Equal(ServiceContractAnalyzer.PossiblyNotSerializable, err.Descriptor);
            Assert.Equal(DiagnosticSeverity.Warning, err.Severity);
            Assert.Equal("gRPC methods require inputs/outputs that can be marshalled with gRPC; this type *may* be usable with gRPC, but it could not be verified.", err.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task DetectNonSerializable_Return()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf.Grpc.Configuration;
using ProtoBuf;
[Service]
public interface IMyService
{
    Bar SomeMethod(Foo value);
}
[ProtoContract]
public class Foo {}
public class Bar {}
");
            var err = Assert.Single(diagnostics);
            Assert.Equal(ServiceContractAnalyzer.PossiblyNotSerializable, err.Descriptor);
            Assert.Equal(DiagnosticSeverity.Warning, err.Severity);
            Assert.Equal("gRPC methods require inputs/outputs that can be marshalled with gRPC; this type *may* be usable with gRPC, but it could not be verified.", err.GetMessage(CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData("Foo Method();")]
        [InlineData("void Method();")]
        public async Task DetectSync(string signature)
        {
            var diagnostics = await AnalyzeAsync($@"
using ProtoBuf.Grpc.Configuration;
using ProtoBuf;
[Service]
public interface IMyService
{{
    {signature}
}}
[ProtoContract]
public class Foo {{}}
", ignorePreferAsyncAdvice: false);
            var err = Assert.Single(diagnostics);
            Assert.Equal(ServiceContractAnalyzer.PreferAsync, err.Descriptor);
            Assert.Equal(DiagnosticSeverity.Info, err.Severity);
            Assert.Equal("gRPC methods should be async when possible.", err.GetMessage(CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData("Foo Method(IAsyncEnumerable<Foo> foos);")]
        [InlineData("void Method(IAsyncEnumerable<Foo> foos);")]
        public async Task DetectStreamingSync(string signature)
        {
            var diagnostics = await AnalyzeAsync($@"
using ProtoBuf.Grpc.Configuration;
using ProtoBuf;
using System.Collections.Generic;
[Service]
public interface IMyService
{{
    {signature}
}}
[ProtoContract]
public class Foo {{}}
", ignorePreferAsyncAdvice: false);
            var err = Assert.Single(diagnostics);
            Assert.Equal(ServiceContractAnalyzer.StreamingSyncMethod, err.Descriptor);
            Assert.Equal(DiagnosticSeverity.Error, err.Severity);
            Assert.Equal("gRPC methods that take streaming parameters cannot be synchronous.", err.GetMessage(CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData("Task<Foo> Method();")]
        [InlineData("ValueTask<Foo> Method();")]
        [InlineData("IAsyncEnumerable<Foo> Method();")]
        [InlineData("Task<Foo> Method(IAsyncEnumerable<Foo> foos);")]
        [InlineData("ValueTask<Foo> Method(IAsyncEnumerable<Foo> foos);")]
        [InlineData("IAsyncEnumerable<Foo> Method(IAsyncEnumerable<Foo> foos);")]
        [InlineData("Task<Foo> Method(Foo foo);")]
        [InlineData("ValueTask<Foo> Method(Foo foo);")]
        [InlineData("IAsyncEnumerable<Foo> Method(Foo foo);")]
        public async Task DetectAsync(string signature)
        {
            var diagnostics = await AnalyzeAsync($@"
#pragma warning disable CS8019
using ProtoBuf.Grpc.Configuration;
using ProtoBuf;
using System.Threading.Tasks;
using System.Collections.Generic;
[Service]
public interface IMyService
{{
    {signature}
}}
[ProtoContract]
public class Foo {{}}
", ignorePreferAsyncAdvice: false);
            Assert.Empty(diagnostics);
        }
    }
}
