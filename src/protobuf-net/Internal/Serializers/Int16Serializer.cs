using System;
using System.Diagnostics;

namespace ProtoBuf.Internal.Serializers
{
    internal sealed class Int16Serializer : IRuntimeProtoSerializerNode
    {
        bool IRuntimeProtoSerializerNode.IsScalar => true;
        private Int16Serializer() { }
        internal static readonly Int16Serializer Instance = new Int16Serializer();

        private static readonly Type expectedType = typeof(short);

        public Type ExpectedType => expectedType;

        bool IRuntimeProtoSerializerNode.RequiresOldValue => false;

        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        public object Read(ref ProtoReader.State state, object value)
        {
            Debug.Assert(value is null); // since replaces
            return state.ReadInt16();
        }

        public void Write(ref ProtoWriter.State state, object value)
        {
            state.WriteInt16((short)value);
        }

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitStateBasedWrite(nameof(ProtoWriter.State.WriteInt16), valueFrom);
        }
        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.EmitStateBasedRead(nameof(ProtoReader.State.ReadInt16), ExpectedType);
        }
    }
}