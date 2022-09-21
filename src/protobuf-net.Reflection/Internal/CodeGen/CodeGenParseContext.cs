#nullable enable
using ProtoBuf.Internal.CodeGen;
using ProtoBuf.Reflection;
using System;
using System.Collections.Generic;

namespace ProtoBuf.Reflection.Internal.CodeGen;

internal class CodeGenParseContext
{
    public NameNormalizer NameNormalizer { get; set; } = NameNormalizer.Default;

    private readonly Dictionary<string, CodeGenType> _contractTypes = new Dictionary<string, CodeGenType>();

    public CodeGenType GetContractType(string fqn)
    {
        if (string.IsNullOrWhiteSpace(fqn)) return CodeGenType.Unknown;
        if (!_contractTypes.TryGetValue(fqn, out var found))
        {
            found = TryFindWellKnown(fqn) ?? new CodeGenPlaceholderType(fqn);
            _contractTypes.Add(fqn, found);
        }
        return found;
    }

    private CodeGenType? TryFindWellKnown(string fqn) => fqn switch
        {
            ".bcl.NetObjectProxy" => CodeGenSimpleType.NetObjectProxy,
            _ => null,
        };

    public CodeGenType GetMapEntryType(string fqn, CodeGenType key, CodeGenType value)
    {
        if (!_contractTypes.TryGetValue(fqn, out var type) || type is CodeGenPlaceholderType)
        {
            type = new CodeGenMapEntryType(fqn, key, value);
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

    internal void FixupPlaceholders()
    {
        foreach (var type in _contractTypes.Values)
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

    internal bool FixupPlaceholder(CodeGenType type, out CodeGenType value)
    {
        if (type is CodeGenPlaceholderType placeholder)
        {
            if (_contractTypes.TryGetValue(placeholder.Name, out value) && value is not CodeGenPlaceholderType)
            {
                return true;
            }
            throw new InvalidOperationException($"No non-placeholder was registered for '{placeholder.Name}'");
        }
        value = type;
        return false;
    }
}
