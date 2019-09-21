namespace ProtoBuf.Serializers
{
    internal interface IDirectWriteNode
    {
        bool CanEmitDirectWrite(WireType wireType);
        void EmitDirectWrite(int fieldNumber, WireType wireType, Compiler.CompilerContext ctx, Compiler.Local valueFrom);
    }
}
