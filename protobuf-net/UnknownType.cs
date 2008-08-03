
namespace ProtoBuf
{
    /// <summary>
    /// The (non-extensible) UnknownType is used when deserializing
    /// unexpected groups.
    /// </summary>
    [ProtoContract]
    internal sealed class UnknownType
    {
        internal static readonly ILengthSerializer<UnknownType> Serializer
            = new EntitySerializer<UnknownType>();
    }
}
