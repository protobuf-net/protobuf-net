#if !NO_RUNTIME
using System;

namespace ProtoBuf.Serializers
{
    internal sealed class BooleanSerializer : IProtoSerializer
    {
        private static readonly Type expectedType = typeof(bool);

        public Type ExpectedType => expectedType;

        public void Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
        {
            ProtoWriter.WriteBoolean((bool)value, dest, ref state);
        }

        public object Read(ProtoReader source, ref ProtoReader.State state, object value)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return source.ReadBoolean(ref state);
        }

        bool IProtoSerializer.RequiresOldValue => false;

        bool IProtoSerializer.ReturnsValue => true;

#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteBoolean", valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.EmitBasicRead("ReadBoolean", ExpectedType);
        }
#endif
    }
}
#endif