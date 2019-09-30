using ProtoBuf.Internal;
using ProtoBuf.WellKnownTypes;

namespace ProtoBuf.Internal
{
    partial class PrimaryTypeProvider : ISerializer<Empty>
    {
        WireType ISerializer<Empty>.DefaultWireType => WireType.String;
        Empty ISerializer<Empty>.Read(ref ProtoReader.State state, Empty value)
        {
            state.SkipAllFields();
            return value;
        }

        void ISerializer<Empty>.Write(ref ProtoWriter.State state, Empty value) { }
    }
}

namespace ProtoBuf.WellKnownTypes
{
    /// <summary>
    /// A generic empty message that you can re-use to avoid defining duplicated empty messages in your APIs
    /// </summary>
    [ProtoContract(Name = ".google.protobuf.Empty", Serializer = typeof(PrimaryTypeProvider))]
    public readonly struct Empty
    {
    }
}