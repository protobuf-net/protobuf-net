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
            Assert.Equal("The member 'Property' is not a method; only methods are supported for services.", err.GetMessage(CultureInfo.InvariantCulture));
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
            Assert.Equal(ServiceContractAnalyzer.InvalidReturnValue, err.Descriptor);
            Assert.Equal(DiagnosticSeverity.Error, err.Severity);
            Assert.Equal("The return value must currently be Void, a reference-type data contract, or an task / async sequence of the same.", err.GetMessage(CultureInfo.InvariantCulture));
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

[Service]
public interface IMyService
{{
    Foo BasicMethod({kind});
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
            Assert.Equal(ServiceContractAnalyzer.InvalidDataParameter, err.Descriptor);
            Assert.Equal(DiagnosticSeverity.Error, err.Severity);
            Assert.Equal("The return value must currently be Void, a reference-type data contract, or an task / async sequence of the same.", err.GetMessage(CultureInfo.InvariantCulture));
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
    }
}
