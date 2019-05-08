#if !NO_RUNTIME
using System;

namespace ProtoBuf.Serializers
{
    internal sealed class DoubleSerializer : IProtoSerializer
    {
        private static readonly Type expectedType = typeof(double);

        public Type ExpectedType => expectedType;

        bool IProtoSerializer.RequiresOldValue => false;

        bool IProtoSerializer.ReturnsValue => true;

        public object Read(ProtoReader source, ref ProtoReader.State state, object value)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return source.ReadDouble(ref state);
        }

        public void Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
        {
            ProtoWriter.WriteDouble((double)value, dest, ref state);
        }

#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteDouble", valueFrom);
        }

        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.EmitBasicRead("ReadDouble", ExpectedType);
        }
#endif
    }
}
#endif