#if FEAT_COMPILER
namespace ProtoBuf.Compiler
{
    internal delegate void ProtoSerializer(object value, ProtoWriter dest);
    internal delegate object ProtoDeserializer(object value, ref ProtoReader.State state, ProtoReader source);
}
#endif