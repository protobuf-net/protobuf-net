using System;
using System.Diagnostics;

namespace ProtoBuf.Internal.Serializers
{
    internal sealed class UIntPtrSerializer : IRuntimeProtoSerializerNode
    {
        bool IRuntimeProtoSerializerNode.IsScalar => true;
        private UIntPtrSerializer() { }
        internal static readonly UIntPtrSerializer Instance = new UIntPtrSerializer();
        public Type ExpectedType => typeof(UIntPtr);
        bool IRuntimeProtoSerializerNode.RequiresOldValue => false;
        bool IRuntimeProtoSerializerNode.ReturnsValue => true;
        public void Write(ref ProtoWriter.State state, object value)
            => state.WriteUIntPtr((UIntPtr)value);

        public object Read(ref ProtoReader.State state, object value)
        {
            Debug.Assert(value is null); // since replaces
            return state.ReadUIntPtr();
        }

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
            => ctx.EmitStateBasedWrite(nameof(ProtoWriter.State.WriteUIntPtr), valueFrom);

        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
            => ctx.EmitStateBasedRead(nameof(ProtoReader.State.ReadUIntPtr), ExpectedType);
    }
}