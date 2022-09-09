#nullable enable

using System;

namespace ProtoBuf.CodeGen;

internal abstract partial class CodeGenType
{
    public override string ToString() => FullyQualifiedPrefix + Name;
    public string Name { get; protected set; }
    public string FullyQualifiedPrefix { get; }
    public bool ShouldSerializeFullyQualifiedPrefix() => !string.IsNullOrWhiteSpace(FullyQualifiedPrefix) && !IsWellKnownType(out _);
    public bool ShouldSerializeName() => !IsWellKnownType(out _);
    public CodeGenType? ParentType { get; }
    public CodeGenType(string name, string fullyQualifiedPrefix)
    {
        Name = name?.Trim() ?? "";
        FullyQualifiedPrefix = fullyQualifiedPrefix?.Trim() ?? "";
    }

    public virtual bool IsWellKnownType(out CodeGenWellKnownType type)
    {
        type = CodeGenWellKnownType.None;
        return false;
    }

    internal virtual string Serialize() => ToString();
}
