using System;
using System.Diagnostics;

namespace ProtoBuf.Internal.Serializers
{
#if NET6_0_OR_GREATER
    internal sealed class DateOnlySerializer : IRuntimeProtoSerializerNode
    {
        bool IRuntimeProtoSerializerNode.IsScalar => true;
        private DateOnlySerializer() { }
        internal static readonly DateOnlySerializer Instance = new DateOnlySerializer();

        private static readonly Type expectedType = typeof(DateOnly);

        public Type ExpectedType => expectedType;

        bool IRuntimeProtoSerializerNode.RequiresOldValue => false;

        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        public object Read(ref ProtoReader.State state, object value)
        {
            Debug.Assert(value is null); // since replaces
            return DateOnly.FromDayNumber(state.ReadInt32());
        }

        public void Write(ref ProtoWriter.State state, object value)
        {
            state.WriteInt32(((DateOnly)value).DayNumber);
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
#endif
}