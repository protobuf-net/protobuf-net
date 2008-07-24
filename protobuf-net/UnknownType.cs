
namespace ProtoBuf
{
    /// <summary>
    /// The (non-extensible) UnknownType is used when deserializing
    /// unexpected groups.
    /// </summary>
    [ProtoContract]
    internal sealed class UnknownType
    {
        internal static readonly IGroupSerializer<UnknownType> Serializer
            = new EntitySerializer<UnknownType>();
    }
}
