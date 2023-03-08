using System.Collections.Generic;
using System.Collections.Immutable;

namespace ProtoBuf.Internal
{
    internal class DiagnosticPropertiesBuilder
    {
        private readonly ImmutableDictionary<string, string>.Builder _builder 
            = ImmutableDictionary.CreateBuilder<string, string>();

        private DiagnosticPropertiesBuilder()
        {
        }

        public static DiagnosticPropertiesBuilder Create() => new();

        public DiagnosticPropertiesBuilder Add(string key, string value)
        {
            _builder.Add(new KeyValuePair<string, string>(key, value));
            return this;
        }

        public ImmutableDictionary<string, string> Build() => _builder.ToImmutableDictionary();
    }
}