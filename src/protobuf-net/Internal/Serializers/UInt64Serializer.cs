using System;
using System.Diagnostics;

namespace ProtoBuf.Internal.Serializers
{
    internal sealed class UInt64Serializer : IRuntimeProtoSerializerNode
    {
        bool IRuntimeProtoSerializerNode.IsScalar => true;
        private UInt64Serializer() { }
        internal static readonly UInt64Serializer Instance = new UInt64Serializer();

        static readonly Type expectedType = typeof(ulong);

        public Type ExpectedType => expectedType;

        bool IRuntimeProtoSerializerNode.RequiresOldValue => false;

        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        public object Read(ref ProtoReader.State state, object value)
        {
            Debug.Assert(value is null); // since replaces
            return state.ReadUInt64();
        }

        public void Write(ref ProtoWriter.State state, object value)
        {
            state.WriteUInt64((ulong)value);
        }

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitStateBasedWrite(nameof(ProtoWriter.State.WriteUInt64), valueFrom);
        }
        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.EmitStateBasedRead(nameof(ProtoReader.State.ReadUInt64), ExpectedType);
        }
    }
}