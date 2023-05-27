using System;
using System.IO;
using System.Linq;
using BuildToolsUnitTests.Generators.Abstractions;
using System.Threading.Tasks;
using BuildToolsUnitTests.Extensions;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using ProtoBuf;
using ProtoBuf.Generators.DiscriminatedUnion;
using ProtoBuf.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace BuildToolsUnitTests.Generators
{
    public class ProtoUnionGeneratorTests : GeneratorTestBase<ProtoUnionGenerator>
    {
        public ProtoUnionGeneratorTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [Fact]
        public async Task GenerateProtoUnion_GenericAttribute_BasicScenario()
        {
            var (result, diagnostics) = await GenerateAsync(cSharpProjectSourceTexts: new[]
            {
                """
                using ProtoBuf;
                namespace MySpace
                {
                    [ProtoUnion<int>("Abc", 1, "Bar")]
                    [ProtoUnion<string>("Abc", 2, "Blap")]
                    partial class Foo
                    {    
                    }
                }
                """
            });

            diagnostics.Should().BeEmpty();
            result.GeneratedTrees.Length.Should().Be(1);
            
            var typeInfo = await GetGeneratedTypeAsync(result);
            typeInfo.Should().NotBeNull();
            typeInfo!.CheckPropertyType("Bar", typeof(int?));
            typeInfo!.CheckPropertyType("Blap", typeof(string));
            typeInfo!.CheckFieldType(CSharpCodeGenerator.GetUnionField("Abc"), typeof(DiscriminatedUnion32Object));
        }
        
        [Fact]
        public async Task GenerateProtoUnion_GenericAttribute_DifferentUnions()
        {
            var (result, diagnostics) = await GenerateAsync(
                csharpCodeText: """
                    using ProtoBuf;
                    namespace MySpace
                    {
                        [ProtoUnion<int>("Qwe", 1, "Bar_int")]
                        [ProtoUnion<string>("Qwe", 2, "Bar_string")]

                        [ProtoUnion<int>("Rty", 3, "Blap_int")]
                        [ProtoUnion<string>("Rty", 4, "Blap_string")]
                        partial class Foo
                        {    
                        }
                    }
                """,
                sourceFileName: "Foo");

            diagnostics.Should().BeEmpty();
            result.GeneratedTrees.Length.Should().Be(2);
            
            var qweTypeInfo = await GetGeneratedTypeAsync(result, filePathFilter: filePath => Path.GetFileName(filePath) == "Foo_Qwe.cs");
            qweTypeInfo.Should().NotBeNull();
            qweTypeInfo!.CheckPropertyType("Bar_int", typeof(int?));
            qweTypeInfo!.CheckPropertyType("Bar_string", typeof(string));
            qweTypeInfo!.CheckFieldType(CSharpCodeGenerator.GetUnionField("Qwe"), typeof(DiscriminatedUnion32Object));
            
            var rtyTypeInfo = await GetGeneratedTypeAsync(result, filePathFilter: filePath => Path.GetFileName(filePath) == "Foo_Rty.cs");
            rtyTypeInfo.Should().NotBeNull();
            rtyTypeInfo!.CheckPropertyType("Blap_int", typeof(int?));
            rtyTypeInfo!.CheckPropertyType("Blap_string", typeof(string));
            rtyTypeInfo!.CheckFieldType(CSharpCodeGenerator.GetUnionField("Rty"), typeof(DiscriminatedUnion32Object));
        }
        
        [Fact]
        public async Task GenerateProtoUnion_GenericAttribute_32AllPropertyTypes()
        {
            var (result, diagnostics) = await GenerateAsync(cSharpProjectSourceTexts: new[]
            {
                """
                using ProtoBuf;
                using System;

                namespace MySpace
                {
                    [ProtoUnion<bool>("Abc", 1, "Bar_bool")]
                    [ProtoUnion<int>("Abc", 2, "Bar_int")]
                    [ProtoUnion<uint>("Abc", 3, "Bar_uint")]
                    [ProtoUnion<float>("Abc", 4, "Bar_float")]
                    partial class Foo
                    {
                    }
                }
                """
            });

            diagnostics.Should().BeEmpty();
            result.GeneratedTrees.Length.Should().Be(1);
            
            var typeInfo = await GetGeneratedTypeAsync(result);
            typeInfo.Should().NotBeNull();
            typeInfo!.CheckPropertyType("Bar_bool", typeof(bool?));
            typeInfo!.CheckPropertyType("Bar_int", typeof(int?));
            typeInfo!.CheckPropertyType("Bar_uint", typeof(uint?));
            typeInfo!.CheckPropertyType("Bar_float", typeof(float?));
            typeInfo!.CheckFieldType(CSharpCodeGenerator.GetUnionField("Abc"), typeof(DiscriminatedUnion32));
        }
        
        [Fact]
        public async Task GenerateProtoUnion_GenericAttribute_64AllPropertyTypes()
        {
            var (result, diagnostics) = await GenerateAsync(cSharpProjectSourceTexts: new[]
            {
                """
                using ProtoBuf;
                using System;

                namespace MySpace
                {
                    [ProtoUnion<bool>("Abc", 1, "Bar_bool")]
                    [ProtoUnion<int>("Abc", 2, "Bar_int")]
                    [ProtoUnion<uint>("Abc", 3, "Bar_uint")]
                    [ProtoUnion<float>("Abc", 4, "Bar_float")]
                    [ProtoUnion<long>("Abc", 5, "Bar_long")]
                    [ProtoUnion<ulong>("Abc", 6, "Bar_ulong")]
                    [ProtoUnion<TimeSpan>("Abc", 8, "Bar_timeSpan")]
                    [ProtoUnion<DateTime>("Abc", 9, "Bar_dateTime")]
                    partial class Foo
                    {
                    }
                }
                """
            });

            diagnostics.Should().BeEmpty();
            result.GeneratedTrees.Length.Should().Be(1);
            
            var typeInfo = await GetGeneratedTypeAsync(result);
            typeInfo.Should().NotBeNull();
            typeInfo!.CheckPropertyType("Bar_bool", typeof(bool?));
            typeInfo!.CheckPropertyType("Bar_int", typeof(int?));
            typeInfo!.CheckPropertyType("Bar_uint", typeof(uint?));
            typeInfo!.CheckPropertyType("Bar_float", typeof(float?));
            typeInfo!.CheckPropertyType("Bar_long", typeof(long?));
            typeInfo!.CheckPropertyType("Bar_ulong", typeof(ulong?));
            typeInfo!.CheckPropertyType("Bar_timeSpan", typeof(TimeSpan?));
            typeInfo!.CheckPropertyType("Bar_dateTime", typeof(DateTime?));
            typeInfo!.CheckFieldType(CSharpCodeGenerator.GetUnionField("Abc"), typeof(DiscriminatedUnion64));
        }
        
        [Fact]
        public async Task GenerateProtoUnion_GenericAttribute_128AllPropertyTypes()
        {
            var (result, diagnostics) = await GenerateAsync(cSharpProjectSourceTexts: new[]
            {
                """
                using ProtoBuf;
                using System;

                namespace MySpace
                {
                    [ProtoUnion<bool>("Abc", 1, "Bar_bool")]
                    [ProtoUnion<int>("Abc", 2, "Bar_int")]
                    [ProtoUnion<uint>("Abc", 3, "Bar_uint")]
                    [ProtoUnion<float>("Abc", 4, "Bar_float")]
                    [ProtoUnion<long>("Abc", 5, "Bar_long")]
                    [ProtoUnion<ulong>("Abc", 6, "Bar_ulong")]
                    [ProtoUnion<TimeSpan>("Abc", 8, "Bar_timeSpan")]
                    [ProtoUnion<DateTime>("Abc", 9, "Bar_dateTime")]
                    [ProtoUnion<Guid>("Abc", 10, "Bar_guid")]
                    partial class Foo
                    {
                    }
                }
                """
            });

            diagnostics.Should().BeEmpty();
            result.GeneratedTrees.Length.Should().Be(1);
            
            var typeInfo = await GetGeneratedTypeAsync(result);
            typeInfo.Should().NotBeNull();
            typeInfo!.CheckPropertyType("Bar_bool", typeof(bool?));
            typeInfo!.CheckPropertyType("Bar_int", typeof(int?));
            typeInfo!.CheckPropertyType("Bar_uint", typeof(uint?));
            typeInfo!.CheckPropertyType("Bar_float", typeof(float?));
            typeInfo!.CheckPropertyType("Bar_long", typeof(long?));
            typeInfo!.CheckPropertyType("Bar_ulong", typeof(ulong?));
            typeInfo!.CheckPropertyType("Bar_timeSpan", typeof(TimeSpan?));
            typeInfo!.CheckPropertyType("Bar_dateTime", typeof(DateTime?));
            typeInfo!.CheckPropertyType("Bar_guid", typeof(Guid?));
            typeInfo!.CheckFieldType(CSharpCodeGenerator.GetUnionField("Abc"), typeof(DiscriminatedUnion128));
        }
        
        [Fact]
        public async Task GenerateProtoUnion_GenericAttribute_ObjectAllPropertyTypes()
        {
            var (result, diagnostics) = await GenerateAsync(cSharpProjectSourceTexts: new[]
            {
                """
                using ProtoBuf;
                using System;

                namespace MySpace
                {
                    [ProtoUnion<string>("Abc", 7, "Bar_string")]
                    [ProtoUnion<MyData>("Abc", 10, "Bar_class")]
                    partial class Foo
                    {
                    }

                    public class MyData
                    {
                    }
                }
                """
            });

            diagnostics.Should().BeEmpty();
            result.GeneratedTrees.Length.Should().Be(1);

            var typeInfo = await GetGeneratedTypeAsync(
                result,
                additionalSourceCodeToInclude: """
                    namespace MySpace
                    {
                        public class MyData 
                        {
                            public string Id { get; set; }
                        }
                    }
                """);

            typeInfo.Should().NotBeNull();
            typeInfo!.CheckPropertyType("Bar_string", typeof(string));
            typeInfo!.CheckPropertyType("Bar_class", expectedTypeName: "MySpace.MyData");
            typeInfo!.CheckFieldType(CSharpCodeGenerator.GetUnionField("Abc"), typeof(DiscriminatedUnionObject));
        }
        
        [Fact]
        public async Task GenerateProtoUnion_GenericAttribute_32ObjectAllPropertyTypes()
        {
            var (result, diagnostics) = await GenerateAsync(cSharpProjectSourceTexts: new[]
            {
                """
                using ProtoBuf;
                using System;

                namespace MySpace
                {
                    [ProtoUnion<bool>("Abc", 1, "Bar_bool")]
                    [ProtoUnion<int>("Abc", 2, "Bar_int")]
                    [ProtoUnion<uint>("Abc", 3, "Bar_uint")]
                    [ProtoUnion<float>("Abc", 4, "Bar_float")]
                    [ProtoUnion<string>("Abc", 7, "Bar_string")]
                    [ProtoUnion<MyData>("Abc", 10, "Bar_class")]
                    partial class Foo
                    {
                    }

                    public class MyData
                    {
                    }
                }
                """
            });

            diagnostics.Should().BeEmpty();
            result.GeneratedTrees.Length.Should().Be(1);

            var typeInfo = await GetGeneratedTypeAsync(
                result,
                additionalSourceCodeToInclude: """
                    namespace MySpace
                    {
                        public class MyData 
                        {
                            public string Id { get; set; }
                        }
                    }
                """);

            typeInfo.Should().NotBeNull();
            typeInfo!.CheckPropertyType("Bar_bool", typeof(bool?));
            typeInfo!.CheckPropertyType("Bar_int", typeof(int?));
            typeInfo!.CheckPropertyType("Bar_uint", typeof(uint?));
            typeInfo!.CheckPropertyType("Bar_float", typeof(float?));
            typeInfo!.CheckPropertyType("Bar_string", typeof(string));
            typeInfo!.CheckPropertyType("Bar_class", expectedTypeName: "MySpace.MyData");
            typeInfo!.CheckFieldType(CSharpCodeGenerator.GetUnionField("Abc"), typeof(DiscriminatedUnion32Object));
        }
        
        [Fact]
        public async Task GenerateProtoUnion_GenericAttribute_64ObjectAllPropertyTypes()
        {
            var (result, diagnostics) = await GenerateAsync(cSharpProjectSourceTexts: new[]
            {
                """
                using ProtoBuf;
                using System;

                namespace MySpace
                {
                    [ProtoUnion<bool>("Abc", 1, "Bar_bool")]
                    [ProtoUnion<int>("Abc", 2, "Bar_int")]
                    [ProtoUnion<uint>("Abc", 3, "Bar_uint")]
                    [ProtoUnion<float>("Abc", 4, "Bar_float")]
                    [ProtoUnion<long>("Abc", 5, "Bar_long")]
                    [ProtoUnion<ulong>("Abc", 6, "Bar_ulong")]
                    [ProtoUnion<string>("Abc", 7, "Bar_string")]
                    [ProtoUnion<TimeSpan>("Abc", 8, "Bar_timeSpan")]
                    [ProtoUnion<DateTime>("Abc", 9, "Bar_dateTime")]
                    [ProtoUnion<MyData>("Abc", 10, "Bar_class")]
                    partial class Foo
                    {
                    }

                    public class MyData
                    {
                    }
                }
                """
            });

            diagnostics.Should().BeEmpty();
            result.GeneratedTrees.Length.Should().Be(1);

            var typeInfo = await GetGeneratedTypeAsync(
                result,
                additionalSourceCodeToInclude: """
                    namespace MySpace
                    {
                        public class MyData 
                        {
                            public string Id { get; set; }
                        }
                    }
                """);

            typeInfo.Should().NotBeNull();
            typeInfo!.CheckPropertyType("Bar_bool", typeof(bool?));
            typeInfo!.CheckPropertyType("Bar_int", typeof(int?));
            typeInfo!.CheckPropertyType("Bar_uint", typeof(uint?));
            typeInfo!.CheckPropertyType("Bar_float", typeof(float?));
            typeInfo!.CheckPropertyType("Bar_long", typeof(long?));
            typeInfo!.CheckPropertyType("Bar_ulong", typeof(ulong?));
            typeInfo!.CheckPropertyType("Bar_string", typeof(string));
            typeInfo!.CheckPropertyType("Bar_timeSpan", typeof(TimeSpan?));
            typeInfo!.CheckPropertyType("Bar_dateTime", typeof(DateTime?));
            typeInfo!.CheckPropertyType("Bar_class", expectedTypeName: "MySpace.MyData");
            typeInfo!.CheckFieldType(CSharpCodeGenerator.GetUnionField("Abc"), typeof(DiscriminatedUnion64Object));
        }
        
        [Fact]
        public async Task GenerateProtoUnion_GenericAttribute_128ObjectAllPropertyTypes()
        {
            var (result, diagnostics) = await GenerateAsync(cSharpProjectSourceTexts: new[]
            {
                """
                using ProtoBuf;
                using System;

                namespace MySpace
                {
                    [ProtoUnion<bool>("Abc", 1, "Bar_bool")]
                    [ProtoUnion<int>("Abc", 2, "Bar_int")]
                    [ProtoUnion<uint>("Abc", 3, "Bar_uint")]
                    [ProtoUnion<float>("Abc", 4, "Bar_float")]
                    [ProtoUnion<long>("Abc", 5, "Bar_long")]
                    [ProtoUnion<ulong>("Abc", 6, "Bar_ulong")]
                    [ProtoUnion<string>("Abc", 7, "Bar_string")]
                    [ProtoUnion<TimeSpan>("Abc", 8, "Bar_timeSpan")]
                    [ProtoUnion<DateTime>("Abc", 9, "Bar_dateTime")]
                    [ProtoUnion<Guid>("Abc", 10, "Bar_guid")]
                    [ProtoUnion<MyData>("Abc", 11, "Bar_class")]
                    partial class Foo
                    {
                    }

                    public class MyData
                    {
                    }
                }
                """
            });

            diagnostics.Should().BeEmpty();
            result.GeneratedTrees.Length.Should().Be(1);

            var typeInfo = await GetGeneratedTypeAsync(
                result,
                additionalSourceCodeToInclude: """
                    namespace MySpace
                    {
                        public class MyData 
                        {
                            public string Id { get; set; }
                        }
                    }
                """);

            typeInfo.Should().NotBeNull();
            typeInfo!.CheckPropertyType("Bar_bool", typeof(bool?));
            typeInfo!.CheckPropertyType("Bar_int", typeof(int?));
            typeInfo!.CheckPropertyType("Bar_uint", typeof(uint?));
            typeInfo!.CheckPropertyType("Bar_float", typeof(float?));
            typeInfo!.CheckPropertyType("Bar_long", typeof(long?));
            typeInfo!.CheckPropertyType("Bar_ulong", typeof(ulong?));
            typeInfo!.CheckPropertyType("Bar_string", typeof(string));
            typeInfo!.CheckPropertyType("Bar_timeSpan", typeof(TimeSpan?));
            typeInfo!.CheckPropertyType("Bar_dateTime", typeof(DateTime?));
            typeInfo!.CheckPropertyType("Bar_guid", typeof(Guid?));
            typeInfo!.CheckPropertyType("Bar_class", expectedTypeName: "MySpace.MyData");
            typeInfo!.CheckFieldType(CSharpCodeGenerator.GetUnionField("Abc"), typeof(DiscriminatedUnion128Object));
        }

        private async Task<System.Reflection.TypeInfo?> GetGeneratedTypeAsync(
            GeneratorDriverRunResult generatorDriverRunResult,
            string additionalSourceCodeToInclude = null,
            string typeName = "MySpace.Foo",
            Func<string, bool>? filePathFilter = null)
        {
            var sourceCodeText = filePathFilter is not null
                ? await generatorDriverRunResult.GeneratedTrees.First(x => filePathFilter!(x.FilePath)).GetTextAsync() 
                : await generatorDriverRunResult.GeneratedTrees.First().GetTextAsync();
            TestOutputHelper?.WriteLine($"Generated sourceCode: \n----\n {sourceCodeText}\n");

            var sourceCode = sourceCodeText.ToString();
            if (!string.IsNullOrEmpty(additionalSourceCodeToInclude))
            {
                sourceCode += "\n\n" + additionalSourceCodeToInclude;
            }
            
            var assembly = TryBuildAssemblyFromSourceCode(sourceCode);
            return assembly.DefinedTypes.FirstOrDefault(type => type.FullName == typeName);
        }
    }
}
