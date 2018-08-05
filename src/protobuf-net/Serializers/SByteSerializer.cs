#if !NO_RUNTIME
using System;

namespace ProtoBuf.Serializers
{
    internal sealed class SByteSerializer : IProtoSerializer
    {
        static readonly Type expectedType = typeof(sbyte);
        
        public Type ExpectedType => expectedType;

        bool IProtoSerializer.RequiresOldValue => false;

        bool IProtoSerializer.ReturnsValue => true;

        public object Read(ref ProtoReader.State state, object value, ProtoReader source)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return source.ReadSByte(ref state);
        }

        public void Write(object value, ProtoWriter dest)
        {
            ProtoWriter.WriteSByte((sbyte)value, dest);
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