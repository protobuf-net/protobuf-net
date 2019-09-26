namespace ProtoBuf.WellKnownTypes
{
    /// <summary>
    /// A generic empty message that you can re-use to avoid defining duplicated empty messages in your APIs
    /// </summary>
    [ProtoContract(Name = ".google.protobuf.Empty")]
    public readonly struct Empty
    {
    }

    partial class WellKnownSerializer : ISerializer<Empty>
    {
        Empty ISerializer<Empty>.Read(ref ProtoReader.State state, Empty value)
        {
            state.SkipAllFields();
            return value;
        }

        void ISerializer<Empty>.Write(ref ProtoWriter.State state, Empty value) { }
    }
}