using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using ProtoBuf.BuildTools.Internal;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;

namespace BuildToolsUnitTests
{
    // these types borrowed from Roslyn's internal implementations of the abstract types
    internal sealed class InMemoryAdditionalText : AdditionalText
    {
        private readonly SourceText _content;
        private readonly (string key, string value)[]? _options;
        public InMemoryAdditionalText(string path, string content, (string key, string value)[]? options = default)
        {
            Path = path;
            _content = SourceText.From(content, Encoding.UTF8);
            _options = options;
        }

        public AnalyzerConfigOptions GetOptions()
        {
            if (_options is null || _options.Length == 0) return InMemoryConfigOptions.Empty;

            var builder = ImmutableDictionary.CreateBuilder<string, string>(AnalyzerConfigOptions.KeyComparer);
            foreach ((var key, var value) in _options)
            {
                builder.Add(Literals.AdditionalFileMetadataPrefix + key, value);
            }
            return new InMemoryConfigOptions(builder.ToImmutable());
        }

        public override string Path { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default) => _content;

        private class InMemoryConfigOptions : AnalyzerConfigOptions
        {
            public static AnalyzerConfigOptions Empty { get; } = new InMemoryConfigOptions(ImmutableDictionary<string, string>.Empty);

            private readonly ImmutableDictionary<string, string> _values;
            public InMemoryConfigOptions(ImmutableDictionary<string, string> values)
                => _values = values;
            // The base declares [NotNullWhen(true)] on `value` and this override cannot restate it:
            // protobuf-net.BuildTools compiles in protobuf-net.Core's INTERNAL polyfill of that
            // attribute (netstandard2.0 has none, see Core's Internal/NullableAttributes.cs), and
            // this project sees BuildTools' internals - so naming the attribute here is CS0433
            // against System.Runtime's own copy. Dropping it is CS8765 instead, which is what is
            // suppressed: callers go through AnalyzerConfigOptions, which still carries it.
#pragma warning disable CS8765 // nullability of parameter doesn't match overridden member
            public override bool TryGetValue(string key, out string? value)
                => _values.TryGetValue(key, out value);
#pragma warning restore CS8765
        }
    }

    internal sealed class TestAnalyzeConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly ImmutableDictionary<object, AnalyzerConfigOptions> _treeDict;

        public static TestAnalyzeConfigOptionsProvider Empty { get; }
            = new TestAnalyzeConfigOptionsProvider(
                ImmutableDictionary<object, AnalyzerConfigOptions>.Empty,
                TestAnalyzerConfigOptions.Empty);

        internal TestAnalyzeConfigOptionsProvider(
            ImmutableDictionary<object, AnalyzerConfigOptions> treeDict,
            AnalyzerConfigOptions globalOptions)
        {
            _treeDict = treeDict;
            GlobalOptions = globalOptions;
        }

        public override AnalyzerConfigOptions GlobalOptions { get; }

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
            => _treeDict.TryGetValue(tree, out var options) ? options : TestAnalyzerConfigOptions.Empty;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
            => _treeDict.TryGetValue(textFile, out var options) ? options : TestAnalyzerConfigOptions.Empty;

        internal TestAnalyzeConfigOptionsProvider WithAdditionalTreeOptions(ImmutableDictionary<object, AnalyzerConfigOptions> treeDict)
            => new TestAnalyzeConfigOptionsProvider(_treeDict.AddRange(treeDict), GlobalOptions);

        internal TestAnalyzeConfigOptionsProvider WithGlobalOptions(AnalyzerConfigOptions globalOptions)
            => new TestAnalyzeConfigOptionsProvider(_treeDict, globalOptions);
    }

    internal sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        public static TestAnalyzerConfigOptions Empty { get; } = new TestAnalyzerConfigOptions(ImmutableDictionary.Create<string, string>(KeyComparer));

        private readonly ImmutableDictionary<string, string> _backing;

        public TestAnalyzerConfigOptions(ImmutableDictionary<string, string> properties)
        {
            _backing = properties;
        }

        // see the note on InMemoryConfigOptions.TryGetValue above
#pragma warning disable CS8765 // nullability of parameter doesn't match overridden member
        public override bool TryGetValue(string key, out string? value) => _backing.TryGetValue(key, out value);
#pragma warning restore CS8765
    }
}
