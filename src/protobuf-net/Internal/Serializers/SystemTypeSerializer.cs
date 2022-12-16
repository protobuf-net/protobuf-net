using System;
using System.Diagnostics;

namespace ProtoBuf.Internal.Serializers
{
    internal sealed class SystemTypeSerializer : IRuntimeProtoSerializerNode
    {
        bool IRuntimeProtoSerializerNode.IsScalar => true;
        private SystemTypeSerializer() { }
        internal static readonly SystemTypeSerializer Instance = new SystemTypeSerializer();

        private static readonly Type expectedType = typeof(Type);

        public Type ExpectedType => expectedType;

        void IRuntimeProtoSerializerNode.Write(ref ProtoWriter.State state, object value)
        {
            state.WriteType((Type)value);
        }

        object IRuntimeProtoSerializerNode.Read(ref ProtoReader.State state, object value)
        {
            Debug.Assert(value is null); // since replaces
            return state.ReadType();
        }

        bool IRuntimeProtoSerializerNode.RequiresOldValue => false;

        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitStateBasedWrite(nameof(ProtoWriter.State.WriteType), valueFrom);
        }
        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.EmitStateBasedRead(nameof(ProtoReader.State.ReadType), ExpectedType);
        }
    }
}