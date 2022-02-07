using System;
using System.Diagnostics;

namespace ProtoBuf.Internal.Serializers
{
    internal sealed class ByteSerializer : IRuntimeProtoSerializerNode
    {
        bool IRuntimeProtoSerializerNode.IsScalar => true;
        private ByteSerializer() { }
        internal static readonly ByteSerializer Instance = new ByteSerializer();
        public Type ExpectedType { get { return expectedType; } }

        private static readonly Type expectedType = typeof(byte);

        bool IRuntimeProtoSerializerNode.RequiresOldValue => false;

        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        public void Write(ref ProtoWriter.State state, object value)
        {
            state.WriteByte((byte)value);
        }

        public object Read(ref ProtoReader.State state, object value)
        {
            Debug.Assert(value is null); // since replaces
            return state.ReadByte();
        }

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitStateBasedWrite(nameof(ProtoWriter.State.WriteByte), valueFrom);
        }

        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.EmitStateBasedRead(nameof(ProtoReader.State.ReadByte), ExpectedType);
        }
    }
}