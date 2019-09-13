using System;

namespace ProtoBuf.Serializers
{
    internal sealed class SystemTypeSerializer : IProtoSerializer
    {
        private static readonly Type expectedType = typeof(Type);

        public Type ExpectedType => expectedType;

        void IProtoSerializer.Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
        {
            ProtoWriter.WriteType((Type)value, dest, ref state);
        }

        object IProtoSerializer.Read(ProtoReader source, ref ProtoReader.State state, object value)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return source.ReadType(ref state);
        }

        bool IProtoSerializer.RequiresOldValue => false;

        bool IProtoSerializer.ReturnsValue => true;

        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteType", valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.EmitBasicRead("ReadType", ExpectedType);
        }
    }
}