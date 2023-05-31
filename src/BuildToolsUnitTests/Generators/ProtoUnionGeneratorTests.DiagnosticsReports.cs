using System.Linq;
using System.Threading.Tasks;
using BuildToolsUnitTests.Extensions;
using FluentAssertions;
using ProtoBuf.BuildTools.Analyzers;
using Xunit;

namespace BuildToolsUnitTests.Generators
{
    public partial class ProtoUnionGeneratorTests
    {
        [Fact]
        public async Task GenerateProtoUnion_FileScopedNamespaceSyntax_DoesNotReportDiagnostic()
        {
            var (result, diagnostics) = await GenerateAsync(cSharpProjectSourceTexts: new[]
            {
                """
                using ProtoBuf;
                namespace MySpace;
                
                [ProtoUnion<int>("Abc", 1, "Bar")]
                [ProtoUnion<string>("Abc", 2, "Blap")]
                partial class Foo
                {
                }
                """
            });

            diagnostics.Length.Should().Be(0);
            result.GeneratedTrees.Length.Should().Be(1);

            var typeInfo = await GetGeneratedTypeAsync(result);
            typeInfo!.Namespace.Should().Be("MySpace");
        }

        [Fact]
        public async Task GenerateProtoUnion_StandardNamespaceSyntax_DoesNotReportDiagnostic()
        {
            var (result, diagnostics) = await GenerateAsync(cSharpProjectSourceTexts: new[]
            {
                """
                using ProtoBuf;
                namespace Space.MySpace
                {
                    [ProtoUnion<int>("Abc", 1, "Bar")]
                    [ProtoUnion<string>("Abc", 2, "Blap")]
                    partial class Foo
                    {
                    }
                }
                """
            });

            diagnostics.Length.Should().Be(0);
            result.GeneratedTrees.Length.Should().Be(1);

            var typeInfo = await GetGeneratedTypeAsync(result, typeName: "Space.MySpace.Foo");
            typeInfo!.Namespace.Should().Be("Space.MySpace");
        }

        [Fact]
        public async Task GenerateProtoUnion_NotPartialClass_ReportsCorrespondingDiagnostic()
        {
            var (_, diagnostics) = await GenerateAsync(cSharpProjectSourceTexts: new[]
            {
                """
                using ProtoBuf;
                namespace MySpace
                {
                    [ProtoUnion<int>("Abc", 1, "Bar")]
                    [ProtoUnion<string>("Abc", 2, "Blap")]
                    class Foo
                    {    
                    }
                }
                """
            });

            diagnostics.Length.Should().Be(1);
            diagnostics.First().Descriptor.Should().Be(DataContractAnalyzer.DiscriminatedUnionShouldBePartial);
        }
        
        [Fact]
        public async Task GenerateProtoUnion_EmptyUnionName_ReportsCorrespondingDiagnostic()
        {
            var (_, diagnostics) = await GenerateAsync(cSharpProjectSourceTexts: new[]
            {
                """
                using ProtoBuf;
                namespace MySpace
                {
                    [ProtoUnion<int>(" ", 1, "Bar")]
                    partial class Foo
                    {    
                    }
                }
                """
            });

            diagnostics.Length.Should().Be(1);
            diagnostics.First().Descriptor.Should().Be(DataContractAnalyzer.DiscriminatedUnionNameShouldNotBeEmpty);
        }
        
        [Fact]
        public async Task GenerateProtoUnion_EmptyMemberName_ReportsCorrespondingDiagnostic()
        {
            var (_, diagnostics) = await GenerateAsync(cSharpProjectSourceTexts: new[]
            {
                """
                using ProtoBuf;
                namespace MySpace
                {
                    [ProtoUnion<int>("Abc", 1, " ")]
                    partial class Foo
                    {    
                    }
                }
                """
            });

            diagnostics.Length.Should().Be(1);
            diagnostics.First().Descriptor.Should().Be(DataContractAnalyzer.DiscriminatedUnionMemberNameShouldNotBeEmpty);
        }
        
        [Fact]
        public async Task GenerateProtoUnion_NonUniqueFieldNumber_ReportsCorrespondingDiagnostic()
        {
            var (_, diagnostics) = await GenerateAsync(cSharpProjectSourceTexts: new[]
            {
                """
                using ProtoBuf;
                namespace MySpace
                {
                    [ProtoUnion<int>("Abc", 1, "Qwe")]
                    [ProtoUnion<int>("Abc", 1, "Rty")]
                    partial class Foo
                    {    
                    }
                }
                """
            });

            diagnostics.Length.Should().Be(1);
            diagnostics.First().Descriptor.Should().Be(DataContractAnalyzer.DiscriminatedUnionFieldNumbersShouldBeUnique);
        }

        [Fact]
        public async Task GenerateProtoUnion_NoNamespaceDefined_ReportsCorrespondingDiagnostic()
        {
            var (_, diagnostics) = await GenerateAsync(cSharpProjectSourceTexts: new[]
            {
                """
                using ProtoBuf;
                
                [ProtoUnion<int>("Abc", 1, "Qwe")]
                [ProtoUnion<int>("Abc", 1, "Rty")]
                partial class Foo
                {    
                }
                """
            });

            diagnostics.Length.Should().Be(1);
            diagnostics.First().Descriptor.Should().Be(DataContractAnalyzer.DiscriminatedUnionNamespaceNotFound);
        }

        [Fact]
        public async Task GenerateProtoUnion_MemberNameHaveDuplicates_ReportsCorrespondingDiagnostic()
        {
            var (_, diagnostics) = await GenerateAsync(cSharpProjectSourceTexts: new[]
            {
                """
                using ProtoBuf;
                
                namespace MySpace
                {
                    [ProtoUnion<int>("Abc", 1, "Qwe")]
                    [ProtoUnion<int>("Abc", 2, "Rty")]
                
                    [ProtoUnion<int>("Dfe", 3, "Rty")]
                    [ProtoUnion<int>("Dfe", 4, "Bar")]
                    partial class Foo
                    {    
                    }
                }
                """
            });

            diagnostics.Length.Should().Be(1);
            diagnostics.First().Descriptor.Should().Be(DataContractAnalyzer.DiscriminatedUnionMemberNamesShouldBeUnique);
        }
    }
}
