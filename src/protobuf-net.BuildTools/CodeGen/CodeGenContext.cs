#nullable enable
using ProtoBuf.Reflection;

namespace ProtoBuf.CodeGen;

internal class CodeGenContext
{
    public NameNormalizer NameNormalizer { get; set; } = NameNormalizer.Default;
}
