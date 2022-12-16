using System;
using System.Diagnostics;
using System.Reflection;

namespace ProtoBuf.Internal.Serializers
{
    internal sealed class Int32Serializer : IRuntimeProtoSerializerNode, IDirectWriteNode
    {
        bool IRuntimeProtoSerializerNode.IsScalar => true;
        private Int32Serializer() { }
        internal static readonly Int32Serializer Instance = new Int32Serializer();

        private static readonly Type expectedType = typeof(int);

        public Type ExpectedType => expectedType;

        bool IRuntimeProtoSerializerNode.RequiresOldValue => false;

        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        public object Read(ref ProtoReader.State state, object value)
        {
            Debug.Assert(value is null); // since replaces
            return state.ReadInt32();
        }

        public void Write(ref ProtoWriter.State state, object value)
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

        bool IDirectWriteNode.CanEmitDirectWrite(WireType wireType) => wireType == WireType.Varint;

        void IDirectWriteNode.EmitDirectWrite(int fieldNumber, WireType wireType, Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            using var loc = ctx.GetLocalWithValue(typeof(int), valueFrom);
            ctx.LoadState();
            ctx.LoadValue(fieldNumber);
            ctx.LoadValue(loc);
            ctx.EmitCall(typeof(ProtoWriter.State).GetMethod(nameof(ProtoWriter.State.WriteInt32Varint), BindingFlags.Instance | BindingFlags.Public,
                null, new[] { typeof(int), typeof(int) }, null));
        }
    }
}