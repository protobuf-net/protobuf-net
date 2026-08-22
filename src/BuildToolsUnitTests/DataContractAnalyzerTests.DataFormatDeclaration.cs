using ProtoBuf.BuildTools.Analyzers;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BuildToolsUnitTests
{
    /// <summary>
    /// Gap B30 item 3: a <c>[ProtoDataFormat]</c> naming a type the resolver can never be asked
    /// about is silently ignored, which is the worst outcome for a declaration whose whole job is
    /// to change the wire format.
    /// </summary>
    public partial class ProtobufFieldAnalyzerTests
    {
        private async Task<int> CountAsync(string declaration)
        {
            var diagnostics = await AnalyzeAsync($@"
                using ProtoBuf;
                using System;
                using System.Collections.Generic;

                {declaration}
                [ProtoContract]
                public class Foo {{
                    [ProtoMember(1)] public Guid Id {{ get; set; }}
                }}
            ");
            return diagnostics.Count(x => x.Descriptor == DataContractAnalyzer.DataFormatDeclarationCannotMatch);
        }

        // both sides unwrap the MEMBER - a collection selects its element, a Nullable<T> unwraps to
        // T - but the declared type is compared as written, so these match nothing at all
        [Theory]
        [InlineData("[ProtoDataFormat(typeof(Guid?), DataFormat.FixedSize)]")]
        [InlineData("[ProtoDataFormat(typeof(List<Guid>), DataFormat.FixedSize)]")]
        [InlineData("[ProtoDataFormat(typeof(Guid[]), DataFormat.FixedSize)]")]
        public async Task ReportsADeclarationThatCanNeverMatch(string declaration)
            => Assert.Equal(1, await CountAsync(declaration));

        // ...and the forms that DO match must stay silent, including string - which is a scalar
        // here, and a reasonable thing to declare a default for, despite being IEnumerable<char>
        [Theory]
        [InlineData("[ProtoDataFormat(typeof(Guid), DataFormat.FixedSize)]")]
        [InlineData("[ProtoDataFormat(typeof(int), DataFormat.ZigZag)]")]
        [InlineData("[ProtoDataFormat(typeof(string), DataFormat.Default)]")]
        [InlineData("")]
        public async Task StaysSilentOnADeclarationThatCanMatch(string declaration)
            => Assert.Equal(0, await CountAsync(declaration));

        /// <summary>The declaration is equally wrong at assembly scope, and equally silent.</summary>
        [Fact]
        public async Task ReportsAtAssemblyScope()
        {
            var diagnostics = await AnalyzeAsync(@"
                using ProtoBuf;
                using System;
                [assembly: ProtoDataFormat(typeof(Guid?), DataFormat.FixedSize)]

                [ProtoContract]
                public class Foo { [ProtoMember(1)] public Guid Id { get; set; } }
            ");
            Assert.Equal(1, diagnostics.Count(
                x => x.Descriptor == DataContractAnalyzer.DataFormatDeclarationCannotMatch));
        }
    }
}
