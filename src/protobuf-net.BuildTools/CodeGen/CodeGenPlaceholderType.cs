#nullable enable

namespace ProtoBuf.CodeGen;

internal class CodeGenPlaceholderType : CodeGenType
{
    public CodeGenPlaceholderType(string fqn) : base(fqn, "") { }

    internal override string Serialize() => "!!" + Name + "!!";
}
