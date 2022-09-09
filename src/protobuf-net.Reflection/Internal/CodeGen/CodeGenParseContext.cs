#nullable enable
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
            found = new CodeGenPlaceholderType(fqn);
            _contractTypes.Add(fqn, found);
        }
        return found;
    }
    public void Register(string fqn, CodeGenType type)
    {
        if (_contractTypes.TryGetValue(fqn, out var found) && found is not CodeGenPlaceholderType)
        {
            throw new InvalidOperationException($"Registing '{fqn}', but existing type is not a placeholder, but {found.GetType().Name}");
        }
        _contractTypes[fqn] = type;
    }
}
