#nullable enable
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGen;

internal class CodeGenMapEntryType : CodeGenType
{
    public CodeGenMapEntryType(string fqn, CodeGenType keyType, CodeGenType valueType)
        : base(fqn, "")
    {
        KeyType = keyType;
        ValueType = valueType;
    }

    public CodeGenType KeyType { get; private set; }
    public CodeGenType ValueType { get; private set; }

    internal override string Serialize() => KeyType.Serialize() + ", " + ValueType.Serialize();

    internal void FixupPlaceholders(CodeGenParseContext context)
    {
        if (context.FixupPlaceholder(KeyType, out var value))
        {
            KeyType = value;
        }
        if (context.FixupPlaceholder(ValueType, out value))
        {
            ValueType = value;
        }
    }

    // note: these will probably never be used; just retaining to avoid recursive serializer graph
    public bool ShouldSerializeKeyType() => false;
    public bool ShouldSerializeValueType() => false;

    public string? KeyTypeName => KeyType?.Serialize();
    public string? ValueTypeName => ValueType?.Serialize();
}
