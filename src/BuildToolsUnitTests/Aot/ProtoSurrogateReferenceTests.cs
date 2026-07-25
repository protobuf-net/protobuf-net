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
    /// The three-assembly case that matters for something like NodaTime: the types live in one
    /// package, the protobuf-net helper that knows how to serialize them is a second, and the
    /// consumer is a third and references only the helper.
    /// </summary>
    /// <remarks>
    /// This cannot be a golden fixture, because it needs real separate compilations — and the
    /// interesting part is precisely that the helper's <c>ProtoSurrogateAttribute</c> is a
    /// <em>different type</em> from the consumer's: both are generator-owned and internal, so each
    /// assembly gets its own copy. Matching by full name rather than by symbol is what makes the
    /// hand-off work, and that is what these tests pin.
    /// </remarks>
    public class ProtoSurrogateReferenceTests : AotGeneratorTestBase
    {
        public ProtoSurrogateReferenceTests(ITestOutputHelper log) : base(log) { }

        /// <summary>Stands in for NodaTime: plain types, no protobuf-net awareness at all.</summary>
        private const string LibrarySource = """
            namespace Chrono;

            public readonly struct Span
            {
                public Span(long ticks) => Ticks = ticks;
                public long Ticks { get; }
            }
            """;

        /// <summary>
        /// Stands in for protobuf-net.NodaTime: knows about both sides, and offers the pairing to
        /// anything that references it. Note it declares its own copy of the trigger attribute,
        /// exactly as the generator's post-init output would give it.
        /// </summary>
        private const string HelperSource = """
            using ProtoBuf;

            [assembly: ProtoSurrogate(typeof(Chrono.Span), typeof(Chrono.Proto.SpanSurrogate),
                Converter = typeof(Chrono.Proto.SpanConverter),
                ToSurrogate = nameof(Chrono.Proto.SpanConverter.ToSurrogate),
                ToType = nameof(Chrono.Proto.SpanConverter.ToSpan))]

            namespace ProtoBuf
            {
                [global::System.AttributeUsage(
                    global::System.AttributeTargets.Class | global::System.AttributeTargets.Assembly,
                    AllowMultiple = true, Inherited = false)]
                internal sealed class ProtoSurrogateAttribute : global::System.Attribute
                {
                    public ProtoSurrogateAttribute(global::System.Type type, global::System.Type surrogate)
                    {
                        Type = type;
                        Surrogate = surrogate;
                    }
                    public global::System.Type Type { get; }
                    public global::System.Type Surrogate { get; }
                    public global::System.Type? Converter { get; set; }
                    public string? ToSurrogate { get; set; }
                    public string? ToType { get; set; }
                }
            }

            namespace Chrono.Proto
            {
                [ProtoContract]
                public class SpanSurrogate
                {
                    [ProtoMember(1)] public long Ticks { get; set; }
                }

                public static class SpanConverter
                {
                    public static SpanSurrogate ToSurrogate(Chrono.Span value) => new() { Ticks = value.Ticks };
                    public static Chrono.Span ToSpan(SpanSurrogate value)
                        => value is null ? default : new Chrono.Span(value.Ticks);
                }
            }
            """;

        /// <summary>The consumer: references the helper, and says nothing about surrogates at all.</summary>
        private const string ConsumerSource = """
            using ProtoBuf;
            using ProtoBuf.Meta;

            namespace Consumer;

            [ProtoContract]
            public class Job
            {
                [ProtoMember(1)] public Chrono.Span Elapsed { get; set; }
            }

            [ProtoModel]
            [ProtoSerializable(typeof(Job))]
            public partial class JobModel : TypeModel
            {
            }
            """;

        [Fact]
        public void SurrogateOfferedByAReferencedHelperIsHonoured()
        {
            var library = Compile("Chrono", LibrarySource);
            var helper = Compile("Chrono.Proto", HelperSource, library);

            var result = Execute<ProtoModelGenerator>(ConsumerSource,
                extraReferences: new[] { library, helper });

            Assert.Equal(0, result.ErrorCount);

            // the whole point: a type from a third assembly, serialized via a surrogate the consumer
            // never mentioned, using the helper's own conversion methods
            Assert.Contains("ISerializer<global::Chrono.Span>", result.GeneratedCode);
            Assert.Contains("global::Chrono.Proto.SpanConverter.ToSurrogate(value)", result.GeneratedCode);
            Assert.Contains("global::Chrono.Proto.SpanConverter.ToSpan(surrogate)", result.GeneratedCode);
        }

        [Fact]
        public void AModelCanOverrideAnOfferFromAReference()
        {
            var library = Compile("Chrono", LibrarySource);
            var helper = Compile("Chrono.Proto", HelperSource, library);

            const string overriding = """
                using ProtoBuf;
                using ProtoBuf.Meta;

                namespace Consumer;

                [ProtoContract]
                public class MySpanSurrogate
                {
                    [ProtoMember(1)] public long Value { get; set; }

                    public static implicit operator MySpanSurrogate(Chrono.Span value)
                        => new() { Value = value.Ticks };
                    public static implicit operator Chrono.Span(MySpanSurrogate value)
                        => value is null ? default : new Chrono.Span(value.Value);
                }

                [ProtoContract]
                public class Job
                {
                    [ProtoMember(1)] public Chrono.Span Elapsed { get; set; }
                }

                [ProtoModel]
                [ProtoSerializable(typeof(Job))]
                [ProtoSurrogate(typeof(Chrono.Span), typeof(MySpanSurrogate))]
                public partial class JobModel : TypeModel
                {
                }
                """;

            var result = Execute<ProtoModelGenerator>(overriding,
                extraReferences: new[] { library, helper });

            Assert.Equal(0, result.ErrorCount);
            Assert.Contains("(global::Consumer.MySpanSurrogate)value", result.GeneratedCode);
            Assert.DoesNotContain("SpanConverter", result.GeneratedCode);
        }

        private static MetadataReference Compile(string assemblyName, string source,
            params MetadataReference[] references)
        {
            var compilation = CSharpCompilation.Create(assemblyName,
                new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest)) },
                MetadataReferenceHelpers.WellKnownReferences
                    .Concat(MetadataReferenceHelpers.ProtoBufReferences)
                    .Concat(references),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    nullableContextOptions: NullableContextOptions.Enable));

            using var peStream = new MemoryStream();
            var emitted = compilation.Emit(peStream);
            Assert.True(emitted.Success, string.Join("\n", emitted.Diagnostics
                .Where(static x => x.Severity == DiagnosticSeverity.Error)
                .Select(static x => x.ToString())));

            return MetadataReference.CreateFromImage(peStream.ToArray());
        }
    }
}
