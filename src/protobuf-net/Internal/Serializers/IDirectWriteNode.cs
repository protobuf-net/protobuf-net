namespace ProtoBuf.Internal.Serializers
{
    internal interface IDirectWriteNode
    {
        bool CanEmitDirectWrite(WireType wireType);
        void EmitDirectWrite(int fieldNumber, WireType wireType, Compiler.CompilerContext ctx, Compiler.Local valueFrom);
    }
    internal interface IDirectRuntimeWriteNode
    {
        bool CanDirectWrite(WireType wireType);
        void DirectWrite(int fieldNumber, WireType wireType, ref ProtoWriter.State state, object value);
    }
}
