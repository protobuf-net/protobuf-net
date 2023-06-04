using System;
using System.Diagnostics;

namespace ProtoBuf.Internal.Serializers
{
#if NET6_0_OR_GREATER
    internal sealed class TimeOnlySerializer : IRuntimeProtoSerializerNode
    {
        bool IRuntimeProtoSerializerNode.IsScalar => false;
        private TimeOnlySerializer() { }
        internal static readonly TimeOnlySerializer Instance = new TimeOnlySerializer();

        private static readonly Type expectedType = typeof(TimeOnly);

        public Type ExpectedType => expectedType;

        bool IRuntimeProtoSerializerNode.RequiresOldValue => false;

        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        public object Read(ref ProtoReader.State state, object value)
        {
            Debug.Assert(value is null); // since replaces
            return BclHelpers.ReadTimeOnly(ref state);
        }

        public void Write(ref ProtoWriter.State state, object value)
        {
            BclHelpers.WriteTimeOnly(ref state, (TimeOnly)value);
        }

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitStateBasedWrite(nameof(BclHelpers.WriteTimeOnly), valueFrom, typeof(BclHelpers));
        }
        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.EmitStateBasedRead(typeof(BclHelpers), nameof(BclHelpers.ReadTimeOnly), ExpectedType);
        }
    }
#endif
}