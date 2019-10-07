using ProtoBuf.Serializers;

namespace ProtoBuf.Compiler
{
    internal delegate void ProtoSerializer<T>(ref ProtoWriter.State state, T value);
    internal delegate T ProtoDeserializer<T>(ref ProtoReader.State state, T value);
    internal delegate T ProtoSubTypeDeserializer<T>(ref ProtoReader.State state, SubTypeState<T> value) where T : class;
}