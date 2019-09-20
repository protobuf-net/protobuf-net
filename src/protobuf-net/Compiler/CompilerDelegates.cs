namespace ProtoBuf.Compiler
{
    internal delegate void ProtoSerializer<T>(ProtoWriter dest, ref ProtoWriter.State state, T value);
    internal delegate T ProtoDeserializer<T>(ref ProtoReader.State state, T value);
}