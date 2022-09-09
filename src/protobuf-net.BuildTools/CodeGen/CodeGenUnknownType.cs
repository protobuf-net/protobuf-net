#nullable enable

namespace ProtoBuf.CodeGen;

partial class CodeGenType
{
    public static CodeGenType Unknown => CodeGenUnknownType.Instance;
}
internal class CodeGenUnknownType : CodeGenType
{
    internal static CodeGenUnknownType Instance { get; } = new();
    private CodeGenUnknownType() : base("(unknown)","") { }
}
