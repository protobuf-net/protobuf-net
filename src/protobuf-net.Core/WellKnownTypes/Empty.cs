namespace ProtoBuf.WellKnownTypes
{
    /// <summary>
    /// A generic empty message that you can re-use to avoid defining duplicated empty messages in your APIs
    /// </summary>
    [ProtoContract(Name = ".google.protobuf.Empty")]
    internal readonly struct Empty { }

    partial class WellKnownSerializer : IProtoSerializer<Empty>
    {
        Empty IProtoSerializer<Empty>.Read(ProtoReader reader, ref ProtoReader.State state, Empty value)
        {
            while(reader.ReadFieldHeader(ref state) > 0)
            {
                reader.SkipField(ref state);
            }
            return value;
        }

        void IProtoSerializer<Empty>.Write(ProtoWriter writer, ref ProtoWriter.State state, Empty value) { }
    }
}