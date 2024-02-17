#nullable enable
using Google.Protobuf.Reflection;
using ProtoBuf.Internal.CodeGen;
using System;
using System.Collections.Generic;

namespace ProtoBuf.Reflection.Internal.CodeGen;

internal sealed class CodeGenDescriptorParseContext : CodeGenParseContext
{
    private readonly IDiagnosticSink? _diagnostics;
    public CodeGenDescriptorParseContext(IDiagnosticSink? diagnostics = null)
        => _diagnostics = diagnostics;
    internal override void ReportDiagnostic(CodeGenDiagnostic diagnostic, ILocated located, params object[] messageArgs)
        => _diagnostics?.ReportDiagnostic(diagnostic, located, messageArgs);

    private readonly Dictionary<string, CodeGenType> _contractTypes = new Dictionary<string, CodeGenType>();

    public CodeGenType GetContractType(string fqn)
    {
        if (string.IsNullOrWhiteSpace(fqn)) return CodeGenType.Unknown;
        if (!_contractTypes.TryGetValue(fqn, out var found))
        {
            found = TryFindWellKnown(fqn) ?? new CodeGenDescriptorPlaceholderType(fqn);
            _contractTypes.Add(fqn, found);
        }
        return found;
    }

    private CodeGenType? TryFindWellKnown(string fqn) => fqn switch
    {
        ".bcl.NetObjectProxy" => CodeGenSimpleType.NetObjectProxy,
        _ => null,
    };

    public CodeGenType GetMapEntryType(string fqn, CodeGenType key, CodeGenType value, object? origin)
    {
        if (!_contractTypes.TryGetValue(fqn, out var type) || type is CodeGenPlaceholderType)
        {
            type = new CodeGenMapEntryType(fqn, key, value, origin);
            _contractTypes[fqn] = type;
        }
        return type;
    }

    public void Register(string fqn, CodeGenType type)
    {
        if (_contractTypes.TryGetValue(fqn, out var found) && found is not CodeGenPlaceholderType)
        {
            throw new InvalidOperationException($"Registing '{fqn}', but existing type is not a placeholder, but {found.GetType().Name}");
        }
        _contractTypes[fqn] = type;
    }

    protected override IEnumerable<CodeGenType> GetKnownTypes() => _contractTypes.Values;
    protected override bool TryResolve(CodeGenPlaceholderType placeholder, out CodeGenType value)
        => _contractTypes.TryGetValue(placeholder.Name, out value);

    internal bool AddMapEntry(DescriptorProto type)
    {
        if (type.Options?.MapEntry == true && type.Fields.Count == 2)
        {
            if (!_contractTypes.TryGetValue(type.FullyQualifiedName, out var found)
                || found is CodeGenPlaceholderType)
            {
                GetMapEntryType(type.FullyQualifiedName,
                    CodeGenField.Parse(type.Fields[0], this).Type,
                    CodeGenField.Parse(type.Fields[1], this).Type, type);
            }
            return true;
        }
        return false;
    }
}
internal abstract class CodeGenParseContext
{
    public NameNormalizer NameNormalizer { get; set; } = NameNormalizer.Default;

    public bool UseLegacyArrayStyle { get; set; }

    protected abstract IEnumerable<CodeGenType> GetKnownTypes();

    internal void FixupPlaceholders()
    {
        foreach (var type in GetKnownTypes())
        {
            switch (type)
            {
                case CodeGenMessage msg:
                    msg.FixupPlaceholders(this);
                    break;
                case CodeGenMapEntryType map:
                    map.FixupPlaceholders(this);
                    break;
            }
        }
    }

    internal abstract void ReportDiagnostic(CodeGenDiagnostic diagnostic, ILocated located, params object[] messageArgs);

    protected abstract bool TryResolve(CodeGenPlaceholderType placeholder, out CodeGenType value);
    internal bool FixupPlaceholder(CodeGenType type, out CodeGenType value)
    {
        if (type is CodeGenPlaceholderType placeholder)
        {
            if (TryResolve(placeholder, out value) && value is not null && value is not CodeGenPlaceholderType)
            {
                return true;
            }
            throw new InvalidOperationException($"No non-placeholder was registered for '{placeholder.Name}'");
        }
        value = type;
        return false;
    }


}
