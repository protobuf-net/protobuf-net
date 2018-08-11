#if !NO_RUNTIME
using System;

namespace ProtoBuf.Serializers
{
    internal sealed class UInt32Serializer : IProtoSerializer
    {
        static readonly Type expectedType = typeof(uint);

        public Type ExpectedType => expectedType;

        bool IProtoSerializer.RequiresOldValue => false;

        bool IProtoSerializer.ReturnsValue => true;

        public object Read(ProtoReader source, ref ProtoReader.State state, object value)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return source.ReadUInt32(ref state);
        }

        public void Write(object value, ProtoWriter dest)
        {
            ProtoWriter.WriteUInt32((uint)value, dest);
        }
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteUInt32", valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.EmitBasicRead("ReadUInt32", typeof(uint));
        }
#endif
    }
}
#endif