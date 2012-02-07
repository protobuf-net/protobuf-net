using System;

#if !NO_RUNTIME

namespace ProtoBuf.Serializers
{
    class SystemTypeSerializer : IProtoSerializer
    {
        public Type ExpectedType { get { return typeof(Type); } }

        void IProtoSerializer.Write(object value, ProtoWriter dest)
        {
            ProtoWriter.WriteType((Type)value, dest);
        }

        object IProtoSerializer.Read(object value, ProtoReader source)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return source.ReadType();
        }

        bool IProtoSerializer.RequiresOldValue { get { return false; } }
        bool IProtoSerializer.ReturnsValue { get { return true; } }

#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteType", valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicRead("ReadType", ExpectedType);
        }
#endif
    }
}

#endif
