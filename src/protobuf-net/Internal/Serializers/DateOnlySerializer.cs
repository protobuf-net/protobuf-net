#if NET6_0_OR_GREATER
using System;
using System.Diagnostics;

namespace ProtoBuf.Internal.Serializers
{
    internal sealed class DateOnlySerializer : IRuntimeProtoSerializerNode
    {
        bool IRuntimeProtoSerializerNode.IsScalar => true;
        private DateOnlySerializer() { }
        internal static readonly DateOnlySerializer Instance = new DateOnlySerializer();

        static readonly Type expectedType = typeof(DateOnly);

        public Type ExpectedType => expectedType;

        bool IRuntimeProtoSerializerNode.RequiresOldValue => false;

        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        public object Read(ref ProtoReader.State state, object value)
        {
            Debug.Assert(value is null); // since replaces
            return BclHelpers.ReadDateOnly(ref state);
        }

        public void Write(ref ProtoWriter.State state, object value)
        {
            BclHelpers.WriteDateOnly(ref state, (DateOnly)value);
        }

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitStateBasedWrite(nameof(BclHelpers.WriteDateOnly), valueFrom, typeof(BclHelpers));
        }
        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.EmitStateBasedRead(typeof(BclHelpers), nameof(BclHelpers.ReadDateOnly), ExpectedType);
        }
    }
}
#endif