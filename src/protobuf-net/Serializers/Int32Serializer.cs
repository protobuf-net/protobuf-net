#if !NO_RUNTIME
using System;

namespace ProtoBuf.Serializers
{
    internal sealed class Int32Serializer : IProtoSerializer
    {
        static readonly Type expectedType = typeof(int);

        public Type ExpectedType => expectedType;

        bool IProtoSerializer.RequiresOldValue => false;

        bool IProtoSerializer.ReturnsValue => true;

        public object Read(ref ProtoReader.State state, object value, ProtoReader source)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return source.ReadInt32(ref state);
        }

        public void Write(object value, ProtoWriter dest)
        {
            ProtoWriter.WriteInt32((int)value, dest);
        }

#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteInt32", valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.EmitBasicRead("ReadInt32", ExpectedType);
        }
#endif

    }
}
#endif