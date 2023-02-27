#nullable enable

namespace ProtoBuf.Reflection.Internal.CodeGen;

internal class CodeGenPlaceholderType : CodeGenType
{
    public CodeGenPlaceholderType(string fqn) : base(fqn, "") { }

    internal override string Serialize() => "!!" + Name + "!!";
}
