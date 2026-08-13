using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ProtoBuf.BuildTools.Generators;
using System.IO;
using System.Linq;
using Xunit;

namespace BuildToolsUnitTests.Aot
{
    /// <summary>
    /// [ProtoSerializer] across real assembly boundaries: the domain type, the serializer package
    /// that declares the pairing, and a consumer that says nothing - the NodaTime-style hand-off,
    /// for serializers instead of surrogates.
    /// </summary>
    public class ProtoSerializerReferenceTests : AotGeneratorTestBase
    {
        public ProtoSerializerReferenceTests(ITestOutputHelper log) : base(log) { }

        // plain domain types, no protobuf-net awareness anywhere
        private const string DomainSource = """
            namespace Norse
            {
                public readonly struct Token<T>
                {
                    public Token(long tag) => Tag = tag;
                    public long Tag { get; }
                }
            }
            """;

        // knows both sides; ships the serializer and offers the pairing to every consumer
        private const string HelperSource = """
            using ProtoBuf;
            using ProtoBuf.Serializers;

            #pragma warning disable PBN9001 // the compile-time model attributes are [Experimental]
            [assembly: ProtoSerializer(typeof(Norse.Token<>), typeof(Norse.Proto.TokenSerializer<>),
                IsScalar = true)]
            #pragma warning restore PBN9001

            namespace Norse.Proto
            {
                public sealed class TokenSerializer<T> : ISerializer<Norse.Token<T>>
                {
                    SerializerFeatures ISerializer<Norse.Token<T>>.Features
                        => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeVarint;

                    Norse.Token<T> ISerializer<Norse.Token<T>>.Read(ref ProtoReader.State state, Norse.Token<T> value)
                        => new Norse.Token<T>(state.ReadInt64());

                    void ISerializer<Norse.Token<T>>.Write(ref ProtoWriter.State state, Norse.Token<T> value)
                        => state.WriteInt64(value.Tag);
                }
            }
            """;

        // WCF-attributed contracts; a third assembly so the serializer only ever arrives as metadata
        private const string ContractsSource = """
            using System.Runtime.Serialization;

            namespace Norse.Contracts
            {
                [DataContract]
                public class LoginRequest
                {
                    [DataMember(Order = 1)] public Norse.Token<int> UserId { get; set; }
                    [DataMember(Order = 2)] public string Password { get; set; }
                }
            }
            """;

        private const string ConsumerSource = """
            using ProtoBuf;
            using ProtoBuf.Meta;

            namespace Consumer
            {
                [ProtoModel]
                [ProtoSerializable(typeof(Norse.Contracts.LoginRequest))]
                public partial class ClientModel : TypeModel
                {
                }
            }
            """;

        [Fact]
        public void SerializerOfferedByAReferencedHelperIsHonoured()
        {
            var domain = Compile("Norse", DomainSource);
            var helper = Compile("Norse.Proto", HelperSource, domain);
            var contracts = Compile("Norse.Contracts", ContractsSource, domain);

            var result = Execute<ProtoModelGenerator>(ConsumerSource, null,
                extraReferences: new[] { domain, helper, contracts });

            Assert.Equal(0, result.ErrorCount);
            Assert.Contains("ISerializerProxy<global::Norse.Token<int>>", result.GeneratedCode);
            Assert.Contains(
                "SerializerCache.Get<global::Norse.Proto.TokenSerializer<int>, global::Norse.Token<int>>()",
                result.GeneratedCode);
            // IsScalar came from the declaration - the metadata-only route - so the member is
            // framed by the serializer, not as a sub-message
            Assert.Contains(".Read(ref state,", result.GeneratedCode);
            Assert.Contains("WriteAny<global::Norse.Token<int>>", result.GeneratedCode);
        }

        // same as HelperSource but with no IsScalar stated, so Features cannot fold across the
        // compiled reference and framing must defer to WriteAny/ReadAny at runtime. Declared
        // independently rather than derived from HelperSource via string surgery: HelperSource is a
        // raw string literal and does NOT normalize line endings, so on Windows CI (this repo's only
        // CI platform) it checks out with CRLF and a "\n"-based Replace silently fails to match.
        private const string HelperSourceNoScalar = """
            using ProtoBuf;
            using ProtoBuf.Serializers;

            #pragma warning disable PBN9001 // the compile-time model attributes are [Experimental]
            [assembly: ProtoSerializer(typeof(Norse.Token<>), typeof(Norse.Proto.TokenSerializer<>))]
            #pragma warning restore PBN9001

            namespace Norse.Proto
            {
                public sealed class TokenSerializer<T> : ISerializer<Norse.Token<T>>
                {
                    SerializerFeatures ISerializer<Norse.Token<T>>.Features
                        => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeVarint;

                    Norse.Token<T> ISerializer<Norse.Token<T>>.Read(ref ProtoReader.State state, Norse.Token<T> value)
                        => new Norse.Token<T>(state.ReadInt64());

                    void ISerializer<Norse.Token<T>>.Write(ref ProtoWriter.State state, Norse.Token<T> value)
                        => state.WriteInt64(value.Tag);
                }
            }
            """;

        [Fact]
        public void WithoutIsScalarTheFramingDefersToRuntime()
        {
            var domain = Compile("Norse", DomainSource);
            var helper = Compile("Norse.Proto", HelperSourceNoScalar, domain);
            var contracts = Compile("Norse.Contracts", ContractsSource, domain);

            var result = Execute<ProtoModelGenerator>(ConsumerSource, null,
                extraReferences: new[] { domain, helper, contracts });

            Assert.Equal(0, result.ErrorCount);
            // Features cannot fold across a compiled reference, so ReadAny/WriteAny decide at run time
            Assert.Contains("state.ReadAny<global::Norse.Token<int>>", result.GeneratedCode);
        }

        [Fact]
        public void AModelCanOverrideAnOfferFromAReference()
        {
            var consumerOverride = """
                using ProtoBuf;
                using ProtoBuf.Meta;
                using ProtoBuf.Serializers;

                namespace Consumer
                {
                    public sealed class MyTokenSerializer : ISerializer<Norse.Token<int>>
                    {
                        SerializerFeatures ISerializer<Norse.Token<int>>.Features
                            => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeFixed64;

                        Norse.Token<int> ISerializer<Norse.Token<int>>.Read(ref ProtoReader.State state, Norse.Token<int> value)
                            => new Norse.Token<int>(state.ReadInt64());

                        void ISerializer<Norse.Token<int>>.Write(ref ProtoWriter.State state, Norse.Token<int> value)
                            => state.WriteInt64(value.Tag);
                    }

                    [ProtoModel]
                    [ProtoSerializable(typeof(Norse.Contracts.LoginRequest))]
                    [ProtoSerializer(typeof(Norse.Token<int>), typeof(MyTokenSerializer), IsScalar = true)]
                    public partial class ClientModel : TypeModel
                    {
                    }
                }
                """;
            var domain = Compile("Norse", DomainSource);
            var helper = Compile("Norse.Proto", HelperSource, domain);
            var contracts = Compile("Norse.Contracts", ContractsSource, domain);

            var result = Execute<ProtoModelGenerator>(consumerOverride, null,
                extraReferences: new[] { domain, helper, contracts });

            Assert.Equal(0, result.ErrorCount);
            Assert.Contains("MyTokenSerializer", result.GeneratedCode);
            Assert.DoesNotContain("TokenSerializer<int>", result.GeneratedCode);
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
