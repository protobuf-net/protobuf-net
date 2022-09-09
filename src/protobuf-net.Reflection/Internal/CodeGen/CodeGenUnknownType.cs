#nullable enable

namespace ProtoBuf.Reflection.Internal.CodeGen;

partial class CodeGenType
{
    public static CodeGenType Unknown => CodeGenUnknownType.Instance;
}
internal class CodeGenUnknownType : CodeGenType
{
    internal static CodeGenUnknownType Instance { get; } = new();
    private CodeGenUnknownType() : base("(unknown)","") { }
}
