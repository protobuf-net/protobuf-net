namespace ProtoBuf.Compiler
{
    internal delegate void ProtoSerializer<TActual>(ProtoWriter dest, ref ProtoWriter.State state, TActual value);
    internal delegate TActual ProtoDeserializer<TBase, TActual>(ProtoReader source, ref ProtoReader.State state, TBase value)
        where TActual : TBase;
}