#if FEAT_COMPILER
namespace ProtoBuf.Compiler
{
    internal delegate void ProtoSerializer(ProtoWriter dest, ref ProtoWriter.State state, object value);
    internal delegate object ProtoDeserializer(ProtoReader source, ref ProtoReader.State state, object value);
}
#endif