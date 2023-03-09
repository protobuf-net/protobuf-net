using System;
using System.Diagnostics;

namespace ProtoBuf.Internal.Serializers
{
    internal sealed class IntPtrSerializer : IRuntimeProtoSerializerNode
    {
        bool IRuntimeProtoSerializerNode.IsScalar => true;
        private IntPtrSerializer() { }
        internal static readonly IntPtrSerializer Instance = new IntPtrSerializer();
        public Type ExpectedType => typeof(IntPtr);
        bool IRuntimeProtoSerializerNode.RequiresOldValue => false;
        bool IRuntimeProtoSerializerNode.ReturnsValue => true;
        public void Write(ref ProtoWriter.State state, object value)
            => state.WriteIntPtr((IntPtr)value);

        public object Read(ref ProtoReader.State state, object value)
        {
            Debug.Assert(value is null); // since replaces
            return state.ReadIntPtr();
        }

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
            => ctx.EmitStateBasedWrite(nameof(ProtoWriter.State.WriteIntPtr), valueFrom);

        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
            => ctx.EmitStateBasedRead(nameof(ProtoReader.State.ReadIntPtr), ExpectedType);
    }
}