#if !NO_RUNTIME
using System;

namespace ProtoBuf.Serializers
{
    internal sealed class SByteSerializer : IProtoSerializer
    {
        private static readonly Type expectedType = typeof(sbyte);

        public Type ExpectedType => expectedType;

        bool IProtoSerializer.RequiresOldValue => false;

        bool IProtoSerializer.ReturnsValue => true;

        public object Read(ProtoReader source, ref ProtoReader.State state, object value)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return source.ReadSByte(ref state);
        }

        public void Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
        {
            ProtoWriter.WriteSByte((sbyte)value, dest, ref state);
        }

#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteSByte", valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.EmitBasicRead("ReadSByte", ExpectedType);
        }
#endif

    }
}
#endif