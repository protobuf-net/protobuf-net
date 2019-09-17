using System;

namespace ProtoBuf.Serializers
{
    internal sealed class SystemTypeSerializer : IRuntimeProtoSerializerNode
    {
        private static readonly Type expectedType = typeof(Type);

        public Type ExpectedType => expectedType;

        void IRuntimeProtoSerializerNode.Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
        {
            ProtoWriter.WriteType((Type)value, dest, ref state);
        }

        object IRuntimeProtoSerializerNode.Read(ProtoReader source, ref ProtoReader.State state, object value)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return source.ReadType(ref state);
        }

        bool IRuntimeProtoSerializerNode.RequiresOldValue => false;

        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteType", valueFrom, this);
        }
        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.EmitBasicRead("ReadType", ExpectedType);
        }
    }
}