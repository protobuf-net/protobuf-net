using System;
using System.Diagnostics;

namespace ProtoBuf.Serializers
{
    internal sealed class ByteSerializer : IRuntimeProtoSerializerNode
    {
        public Type ExpectedType { get { return expectedType; } }

        private static readonly Type expectedType = typeof(byte);

        bool IRuntimeProtoSerializerNode.RequiresOldValue => false;

        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        public void Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
        {
            ProtoWriter.WriteByte((byte)value, dest, ref state);
        }

        public object Read(ref ProtoReader.State state, object value)
        {
            Debug.Assert(value == null); // since replaces
            return state.ReadByte();
        }

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteByte", valueFrom, this);
        }

        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.EmitStateBasedRead(nameof(ProtoReader.State.ReadByte), ExpectedType);
        }
    }
}