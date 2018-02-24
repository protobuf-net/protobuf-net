#if !NO_RUNTIME
using System;

#if FEAT_IKVM
using Type = IKVM.Reflection.Type;
using IKVM.Reflection;
#endif

namespace ProtoBuf.Serializers
{
    sealed class DateTimeOffsetSerializer : IProtoSerializer
    {
#if FEAT_IKVM
        readonly Type expectedType;
#else
        static readonly Type expectedType = typeof(DateTimeOffset);
#endif
        public Type ExpectedType => expectedType;

        bool IProtoSerializer.RequiresOldValue => false;

        bool IProtoSerializer.ReturnsValue => true;

        public DateTimeOffsetSerializer(DataFormat dataFormat, ProtoBuf.Meta.TypeModel model)
        {
        }
#if !FEAT_IKVM
        public object Read(object value, ProtoReader source)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return BclHelpers.ReadDateTimeOffset(source);
        }

        public void Write(object value, ProtoWriter dest)
        {
            BclHelpers.WriteDateTimeOffset((DateTimeOffset)value, dest);
        }
#endif
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitWrite(ctx.MapType(typeof(BclHelpers)), nameof(BclHelpers.WriteDateTimeOffset), valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicRead(ctx.MapType(typeof(BclHelpers)), nameof(BclHelpers.ReadDateTimeOffset), ExpectedType);
        }
#endif

    }
}
#endif