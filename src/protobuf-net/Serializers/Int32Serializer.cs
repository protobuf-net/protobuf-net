using System;
using System.Diagnostics;

namespace ProtoBuf.Serializers
{
    internal sealed class Int32Serializer : IRuntimeProtoSerializerNode
    {
        private static readonly Type expectedType = typeof(int);

        public Type ExpectedType => expectedType;

        bool IRuntimeProtoSerializerNode.RequiresOldValue => false;

        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        public object Read(ref ProtoReader.State state, object value)
        {
            Debug.Assert(value == null); // since replaces
            return state.ReadInt32();
        }

        public void Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
        {
            state.WriteInt32((int)value);
        }

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitStateBasedWrite(nameof(ProtoWriter.State.WriteInt32), valueFrom);
        }
        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.EmitStateBasedRead(nameof(ProtoReader.State.ReadInt32), ExpectedType);
        }
    }
}