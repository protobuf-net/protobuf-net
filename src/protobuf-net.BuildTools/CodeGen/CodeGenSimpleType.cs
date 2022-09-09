#nullable enable
namespace ProtoBuf.CodeGen;

internal class CodeGenSimpleType : CodeGenType
{
    public static CodeGenSimpleType String = new WellKnown(CodeGenWellKnownType.String, "String", "System.");
    protected CodeGenSimpleType(string name, string fullyQualifiedPrefix) : base(name, fullyQualifiedPrefix) { }
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
            _ => WellKnownType.ToString(),
        };
    }

}

