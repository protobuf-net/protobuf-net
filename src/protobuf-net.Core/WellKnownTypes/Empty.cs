namespace ProtoBuf.WellKnownTypes
{
    /// <summary>
    /// A generic empty message that you can re-use to avoid defining duplicated empty messages in your APIs
    /// </summary>
    [ProtoContract(Name = ".google.protobuf.Empty")]
    internal readonly struct Empty { }

    partial class WellKnownSerializer : IProtoDeserializer<Empty>, IProtoSerializer<Empty>
    {
        Empty IProtoDeserializer<Empty>.Deserialize(ProtoReader reader, ref ProtoReader.State state, Empty value)
        {
            while(reader.ReadFieldHeader(ref state) > 0)
            {
                reader.SkipField(ref state);
            }
            return value;
        }

        void IProtoSerializer<Empty>.Serialize(ProtoWriter writer, ref ProtoWriter.State state, Empty value) { }
    }
}