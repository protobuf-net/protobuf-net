using System;
using System.Diagnostics;

namespace ProtoBuf.Internal.Serializers
{
    internal sealed class Int64Serializer : IRuntimeProtoSerializerNode
    {
        bool IRuntimeProtoSerializerNode.IsScalar => true;
        private Int64Serializer() { }
        internal static readonly Int64Serializer Instance = new Int64Serializer();

        private static readonly Type expectedType = typeof(long);

        public Type ExpectedType => expectedType;

        bool IRuntimeProtoSerializerNode.RequiresOldValue => false;

        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        public object Read(ref ProtoReader.State state, object value)
        {
            Debug.Assert(value is null); // since replaces
            return state.ReadInt64();
        }

        public void Write(ref ProtoWriter.State state, object value)
        {
            state.WriteInt64((long)value);
        }

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitStateBasedWrite(nameof(ProtoWriter.State.WriteInt64), valueFrom);
        }
        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.EmitStateBasedRead(nameof(ProtoReader.State.ReadInt64), ExpectedType);
        }
    }
}