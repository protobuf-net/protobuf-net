#nullable enable

using ProtoBuf.Internal.CodeGen;

namespace ProtoBuf.Reflection.Internal.CodeGen;

internal abstract partial class CodeGenType
{
    public override string ToString() => FullyQualifiedPrefix + Name;
    public string Name { get; protected set; }
    public string FullyQualifiedPrefix { get; }
    public bool ShouldSerializeFullyQualifiedPrefix() => !string.IsNullOrWhiteSpace(FullyQualifiedPrefix) && !IsWellKnownType(out _);
    public bool ShouldSerializeName() => !IsWellKnownType(out _);
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

internal abstract class CodeGenLocatedType : CodeGenType, ILocated
{
    protected CodeGenLocatedType(string name, string fullyQualifiedPrefix, object? origin)
        : base(name, fullyQualifiedPrefix) => _origin = origin;
    private readonly object? _origin;
    object? ILocated.Origin => _origin;
}