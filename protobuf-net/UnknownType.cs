
using ProtoBuf.Property;
namespace ProtoBuf
{
    /// <summary>
    /// The (non-extensible) UnknownType is used when deserializing
    /// unexpected groups.
    /// </summary>
    [ProtoContract]
    internal sealed class UnknownType
    {
        public static readonly UnknownType Default = new UnknownType();
    }
}
