namespace ProtoBuf.WellKnownTypes
{
    /// <summary>
    /// A generic empty message that you can re-use to avoid defining duplicated empty messages in your APIs
    /// </summary>
    [ProtoContract(Name = ".google.protobuf.Empty")]
    internal readonly struct Empty { }

    partial class WellKnownSerializer : IProtoSerializer<Empty>
    {
        Empty IProtoSerializer<Empty>.Read(ref ProtoReader.State state, Empty value)
        {
            while(state.ReadFieldHeader() > 0)
            {
                state.SkipField();
            }
            return value;
        }

        void IProtoSerializer<Empty>.Write(ref ProtoWriter.State state, Empty value) { }
    }
}