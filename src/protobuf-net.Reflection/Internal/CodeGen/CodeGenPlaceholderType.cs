#nullable enable


namespace ProtoBuf.Reflection.Internal.CodeGen;

internal abstract class CodeGenPlaceholderType : CodeGenType
{
    protected CodeGenPlaceholderType(string name, string fullyQualifiedPrefix) : base(name, fullyQualifiedPrefix) { }

    internal override string Serialize() => "!!" + Name + "!!";
}
internal sealed class CodeGenDescriptorPlaceholderType : CodeGenPlaceholderType
{
    public CodeGenDescriptorPlaceholderType(string fqn) : base(fqn, "") { }
}
