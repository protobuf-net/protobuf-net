#if !NO_RUNTIME
using System;

namespace ProtoBuf.Serializers
{
    sealed class Int16Serializer : IProtoSerializer
    {
        static readonly Type expectedType = typeof(short);

        public Type ExpectedType => expectedType;

        bool IProtoSerializer.RequiresOldValue => false;

        bool IProtoSerializer.ReturnsValue => true;

        public object Read(ref ProtoReader.State state, object value, ProtoReader source)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return source.ReadInt16(ref state);
        }

        public void Write(object value, ProtoWriter dest)
        {
            ProtoWriter.WriteInt16((short)value, dest);
        }

#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteInt16", valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.EmitBasicRead("ReadInt16", ExpectedType);
        }
#endif

    }
}
#endif