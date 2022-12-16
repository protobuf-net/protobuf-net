using System;
using System.Diagnostics;

namespace ProtoBuf.Internal.Serializers
{
    sealed class SingleSerializer : IRuntimeProtoSerializerNode
    {
        bool IRuntimeProtoSerializerNode.IsScalar => true;
        private SingleSerializer() { }
        internal static readonly SingleSerializer Instance = new SingleSerializer();

        static readonly Type expectedType = typeof(float);

        public Type ExpectedType { get { return expectedType; } }

        bool IRuntimeProtoSerializerNode.RequiresOldValue => false;

        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        public object Read(ref ProtoReader.State state, object value)
        {
            Debug.Assert(value is null); // since replaces
            return state.ReadSingle();
        }

        public void Write(ref ProtoWriter.State state, object value)
        {
            state.WriteSingle((float)value);
        }

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitStateBasedWrite(nameof(ProtoWriter.State.WriteSingle), valueFrom);
        }
        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.EmitStateBasedRead(nameof(ProtoReader.State.ReadSingle), ExpectedType);
        }
    }
}