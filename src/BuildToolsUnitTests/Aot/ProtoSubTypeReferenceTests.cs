using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ProtoBuf.BuildTools.Generators;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace BuildToolsUnitTests.Aot
{
    /// <summary>
    /// <c>[ProtoSubType]</c> across real assembly boundaries: a base type in one package, the
    /// sub-type and the linkage in a second, and a consumer that says nothing about either.
    /// </summary>
    /// <remarks>
    /// This cannot be a golden fixture, because the whole point is that the declaration crosses an
    /// assembly boundary — and every fixture is linked into one assembly. It is the shape
    /// protobuf-net#1308 asks for: <c>AddSubType</c> works at runtime because the model is built at
    /// runtime, and the compile-time model needs somewhere to say the same thing.
    /// </remarks>
    public class ProtoSubTypeReferenceTests : AotGeneratorTestBase
    {
        public ProtoSubTypeReferenceTests(ITestOutputHelper log) : base(log) { }

        /// <summary>The base library: a contract, with no knowledge of anything below it.</summary>
        private const string LibrarySource = """
            using ProtoBuf;

            namespace Shapes;

            [ProtoContract]
            public class Shape
            {
                [ProtoMember(1)] public string Label { get; set; }
            }
            """;

        /// <summary>
        /// A package that extends the hierarchy and offers the linkage to anything referencing it,
        /// using the real attribute from protobuf-net.Core.
        /// </summary>
        private const string CircleSource = """
            using ProtoBuf;

            #pragma warning disable PBN9001 // the compile-time model attributes are [Experimental]
            [assembly: ProtoSubType(typeof(Shapes.Shape), typeof(Shapes.Round.Circle), 100)]
            #pragma warning restore PBN9001

            namespace Shapes.Round;

            [ProtoContract]
            public class Circle : Shapes.Shape
            {
                [ProtoMember(1)] public int Radius { get; set; }
            }
            """;

        /// <summary>A second, independent package doing the same thing to the same base.</summary>
        private const string SquareSource = """
            using ProtoBuf;

            #pragma warning disable PBN9001
            [assembly: ProtoSubType(typeof(Shapes.Shape), typeof(Shapes.Angular.Square), 101)]
            #pragma warning restore PBN9001

            namespace Shapes.Angular;

            [ProtoContract]
            public class Square : Shapes.Shape
            {
                [ProtoMember(1)] public int Side { get; set; }
            }
            """;

        private const string ConsumerHolder = """
            using ProtoBuf;
            using ProtoBuf.Meta;

            namespace Consumer;

            [ProtoContract]
            public class Holder
            {
                [ProtoMember(1)] public Shapes.Shape Shape { get; set; }
            }

            [ProtoModel]
            [ProtoSerializable(typeof(Holder))]
            public partial class HolderModel : TypeModel
            {
            }
            """;

        [Fact]
        public void SubTypeOfferedByAReferencedLibraryIsHonoured()
        {
            var library = Compile("Shapes", LibrarySource);
            var round = Compile("Shapes.Round", CircleSource, library);

            var result = Execute<ProtoModelGenerator>(ConsumerHolder,
                extraReferences: new[] { library, round });

            Assert.Equal(0, result.ErrorCount);

            // the base is a hierarchy root now, though nothing in either the base library or the
            // consumer says so
            Assert.Contains("ISubTypeSerializer<global::Shapes.Shape>", result.GeneratedCode);
            Assert.Contains("value is global::Shapes.Round.Circle sub100", result.GeneratedCode);
            Assert.Contains("state.WriteSubType(100, sub100, this)", result.GeneratedCode);
        }

        [Fact]
        public void DeclarationsFromTwoReferencesAccumulate()
        {
            // where [ProtoSurrogate] is most-specific-wins, this is a union: two packages each
            // extending one base is exactly the case the feature is for, and neither knows about
            // the other
            var library = Compile("Shapes", LibrarySource);
            var round = Compile("Shapes.Round", CircleSource, library);
            var angular = Compile("Shapes.Angular", SquareSource, library);

            var result = Execute<ProtoModelGenerator>(ConsumerHolder,
                extraReferences: new[] { library, round, angular });

            Assert.Equal(0, result.ErrorCount);
            Assert.Contains("value is global::Shapes.Round.Circle sub100", result.GeneratedCode);
            Assert.Contains("value is global::Shapes.Angular.Square sub101", result.GeneratedCode);
        }

        [Fact]
        public void SeedingOnlyTheSubTypeStillReachesTheBase()
        {
            // the other direction: an out-of-band sub-type is linked *back* to its base, so seeding
            // one end is enough and nothing has to be seeded twice
            var library = Compile("Shapes", LibrarySource);
            var round = Compile("Shapes.Round", CircleSource, library);

            const string source = """
                using ProtoBuf;
                using ProtoBuf.Meta;

                namespace Consumer;

                [ProtoModel]
                [ProtoSerializable(typeof(Shapes.Round.Circle))]
                public partial class CircleModel : TypeModel
                {
                }
                """;

            var result = Execute<ProtoModelGenerator>(source, extraReferences: new[] { library, round });

            Assert.Equal(0, result.ErrorCount);
            Assert.Contains("ISerializer<global::Shapes.Shape>", result.GeneratedCode);
            Assert.Contains("value is global::Shapes.Round.Circle sub100", result.GeneratedCode);
        }

        [Fact]
        public void AModelCanExtendAHierarchyItReferences()
        {
            // the ticket's own shape: the sub-type is a closed generic construction, which the base
            // library could not have named in a [ProtoInclude] however much it wanted to
            var library = Compile("Shapes", LibrarySource);

            const string source = """
                using ProtoBuf;
                using ProtoBuf.Meta;

                namespace Consumer;

                [ProtoContract]
                public class Tagged<T> : Shapes.Shape
                {
                    [ProtoMember(1)] public T Value { get; set; }
                }

                [ProtoModel]
                [ProtoSerializable(typeof(Shapes.Shape))]
                [ProtoSubType(typeof(Shapes.Shape), typeof(Tagged<int>), 100)]
                public partial class TaggedModel : TypeModel
                {
                }
                """;

            var result = Execute<ProtoModelGenerator>(source, extraReferences: new[] { library });

            Assert.Equal(0, result.ErrorCount);
            Assert.Contains("value is global::Consumer.Tagged<int> sub100", result.GeneratedCode);
        }

        [Fact]
        public void ConflictingDeclarationsFromTwoReferencesAreReported()
        {
            // the cost of accumulating: two packages can collide, and neither is at fault. Reported
            // against the base, which is where the collision exists, and the hierarchy is dropped -
            // emitting it would give ReadSubType two cases with the same label
            var library = Compile("Shapes", LibrarySource);
            var round = Compile("Shapes.Round", CircleSource, library);
            var clash = Compile("Shapes.Clash", """
                using ProtoBuf;

                #pragma warning disable PBN9001
                [assembly: ProtoSubType(typeof(Shapes.Shape), typeof(Shapes.Clashing.Blob), 100)]
                #pragma warning restore PBN9001

                namespace Shapes.Clashing;

                [ProtoContract]
                public class Blob : Shapes.Shape
                {
                    [ProtoMember(1)] public int Size { get; set; }
                }
                """, library);

            var result = Execute<ProtoModelGenerator>(ConsumerHolder,
                extraReferences: new[] { library, round, clash });

            var reported = result.Result.Diagnostics
                .Where(static x => x.Id == "PBN3002")
                .Select(static x => x.GetMessage())
                .ToList();

            Assert.Contains(reported, x => x.Contains("both declared as sub-types at field number 100"));
        }

        private static MetadataReference Compile(string assemblyName, string source,
            params MetadataReference[] references)
        {
            var compilation = CSharpCompilation.Create(assemblyName,
                new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest)) },
                MetadataReferenceHelpers.WellKnownReferences
                    .Concat(MetadataReferenceHelpers.ProtoBufReferences)
                    .Concat(references),
                CompilationOptions.WithNullableContextOptions(NullableContextOptions.Enable));

            using var peStream = new MemoryStream();
            var emitted = compilation.Emit(peStream);
            Assert.True(emitted.Success, string.Join("\n", emitted.Diagnostics
                .Where(static x => x.Severity == DiagnosticSeverity.Error)
                .Select(static x => x.ToString())));

            return MetadataReference.CreateFromImage(peStream.ToArray());
        }
    }
}
