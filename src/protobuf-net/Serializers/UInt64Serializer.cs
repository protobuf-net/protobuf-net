#if !NO_RUNTIME
using System;

namespace ProtoBuf.Serializers
{
    internal sealed class UInt64Serializer : IProtoSerializer
    {
        static readonly Type expectedType = typeof(ulong);

        public Type ExpectedType => expectedType;

        bool IProtoSerializer.RequiresOldValue => false;

        bool IProtoSerializer.ReturnsValue => true;

        public object Read(ProtoReader source, ref ProtoReader.State state, object value)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return source.ReadUInt64(ref state);
        }

        public void Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
        {
            ProtoWriter.WriteUInt64((ulong)value, dest, ref state);
        }

#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteUInt64", valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.EmitBasicRead("ReadUInt64", ExpectedType);
        }
#endif
    }
}
#endif