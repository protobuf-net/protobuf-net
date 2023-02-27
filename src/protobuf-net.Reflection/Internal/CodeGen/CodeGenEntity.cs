#nullable enable
namespace ProtoBuf.Internal.CodeGen;

internal abstract class CodeGenEntity : ILocated
{
    private readonly object? _origin;
    protected CodeGenEntity(object? origin) => _origin = origin;

    object? ILocated.Origin => _origin;
}
