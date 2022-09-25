#nullable enable

using System;

namespace ProtoBuf.Reflection.Internal.CodeGen;

internal class CodeGenSimpleType : CodeGenType
{
    protected CodeGenSimpleType(string name, string fullyQualifiedPrefix) : base(name, fullyQualifiedPrefix) { }
    public static CodeGenSimpleType String { get; } = new WellKnown(CodeGenWellKnownType.String, "String", "System.");
    public static CodeGenSimpleType Float { get; } = new WellKnown(CodeGenWellKnownType.Float, "Single", "System.");
    public static CodeGenSimpleType Double { get; } = new WellKnown(CodeGenWellKnownType.Double, "Double", "System.");
    public static CodeGenSimpleType Int32 { get; } = new WellKnown(CodeGenWellKnownType.Int32, "Int32", "System.");
    public static CodeGenSimpleType Int64 { get; } = new WellKnown(CodeGenWellKnownType.Int64, "Int64", "System.");
    public static CodeGenSimpleType SInt32 { get; } = new WellKnown(CodeGenWellKnownType.SInt32, "Int32", "System.");
    public static CodeGenSimpleType SInt64 { get; } = new WellKnown(CodeGenWellKnownType.SInt64, "Int64", "System.");
    public static CodeGenSimpleType UInt32 { get; } = new WellKnown(CodeGenWellKnownType.UInt32, "UInt32", "System.");
    public static CodeGenSimpleType UInt64 { get; } = new WellKnown(CodeGenWellKnownType.UInt64, "UInt64", "System.");
    public static CodeGenSimpleType Boolean { get; } = new WellKnown(CodeGenWellKnownType.Boolean, "Boolean", "System.");
    public static CodeGenSimpleType Byte { get; } = new WellKnown(CodeGenWellKnownType.Byte, "Byte", "System.");
    public static CodeGenSimpleType Bytes { get; } = new WellKnown(CodeGenWellKnownType.Bytes, "Byte[]", "System.");
    public static CodeGenSimpleType Fixed32 { get; } = new WellKnown(CodeGenWellKnownType.Fixed32, "UInt32", "System.");
    public static CodeGenSimpleType Fixed64 { get; } = new WellKnown(CodeGenWellKnownType.Fixed64, "UInt64", "System.");
    public static CodeGenSimpleType SFixed32 { get; } = new WellKnown(CodeGenWellKnownType.SFixed32, "Int32", "System.");
    public static CodeGenSimpleType SFixed64 { get; } = new WellKnown(CodeGenWellKnownType.SFixed64, "Int64", "System.");
    public static CodeGenSimpleType NetObjectProxy { get; } = new WellKnown(CodeGenWellKnownType.NetObjectProxy, "NetObjectProxy", "ProtoBuf.Bcl."); // dummy; does not exist

    public virtual CodeGenWellKnownType WellKnownType => CodeGenWellKnownType.None;
    public virtual bool ShouldSerializeWellKnownType() => false;

    internal class WellKnown : CodeGenSimpleType
    {
        internal WellKnown(CodeGenWellKnownType wellKnownType, string name, string fullyQualifiedPrefix) : base(name, fullyQualifiedPrefix)
            => WellKnownType = wellKnownType;

        public override CodeGenWellKnownType WellKnownType { get; }
        public override bool IsWellKnownType(out CodeGenWellKnownType type)
        {
            type = WellKnownType;
            return type != CodeGenWellKnownType.None;
        }
        public override bool ShouldSerializeWellKnownType() => WellKnownType != CodeGenWellKnownType.None;

        internal override string Serialize() => WellKnownType switch
        {
            // special JSON output
            CodeGenWellKnownType.String => "string",
            CodeGenWellKnownType.Bytes => "bytes",
            CodeGenWellKnownType.Float => "float",
            CodeGenWellKnownType.Double => "double",
            CodeGenWellKnownType.Boolean => "bool",
            CodeGenWellKnownType.Int32 => "int32",
            CodeGenWellKnownType.Int64 => "int64",
            CodeGenWellKnownType.SInt32 => "sint32",
            CodeGenWellKnownType.SInt64 => "sint64",
            CodeGenWellKnownType.UInt32 => "uint32",
            CodeGenWellKnownType.UInt64 => "uint64",
            CodeGenWellKnownType.Fixed32 => "fixed32",
            CodeGenWellKnownType.Fixed64 => "fixed64",
            CodeGenWellKnownType.SFixed32 => "sfixed32",
            CodeGenWellKnownType.SFixed64 => "sfixed64",
            _ => ":" + WellKnownType.ToString(),
        };
    }

}

