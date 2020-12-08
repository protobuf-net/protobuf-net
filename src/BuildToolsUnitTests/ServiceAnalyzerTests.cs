using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Analyzers;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BuildToolsUnitTests
{
    public class ServiceAnalyzerTests : AnalyzerTestBase<ProtoBufServiceAnalyzer>
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
        public async Task DetectInvalidParameter()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf.Grpc.Configuration;
[Service]
public interface IMyService
{
    public void Bar();
}");
            Assert.Empty(diagnostics);
        }
    }
}
