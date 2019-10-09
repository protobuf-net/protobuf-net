using System;

namespace ProtoBuf.Internal.Serializers
{
#if FEAT_DYNAMIC_REF
    internal sealed class NetObjectSerializer : IRuntimeProtoSerializerNode
    {
        private readonly BclHelpers.NetObjectOptions options;

        public NetObjectSerializer(Type type, BclHelpers.NetObjectOptions options)
        {
            bool dynamicType = (options & BclHelpers.NetObjectOptions.DynamicType) != 0;
            ExpectedType = dynamicType ? typeof(object) : type;
            this.options = options;
        }

        public Type ExpectedType { get; }

        public bool ReturnsValue => true;

        public bool RequiresOldValue => true;

        public object Read(ref ProtoReader.State state, object value)
        {
            return BclHelpers.ReadNetObject(ref state, value, ExpectedType == typeof(object) ? null : ExpectedType, options);
        }

        public void Write(ref ProtoWriter.State state, object value)
        {
            BclHelpers.WriteNetObject(ref state, value, options);
        }

        public void EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            // BclHelpers.ReadNetObject(ref state, value, ExpectedType == typeof(object) ? null : ExpectedType, options);
            using var loc = ctx.GetLocalWithValue(ExpectedType, valueFrom);
            ctx.LoadState();
            ctx.LoadValue(loc);
            if (ExpectedType == typeof(object))
                ctx.LoadNullRef();
            else
                ctx.LoadValue(ExpectedType);
            ctx.LoadValue((int)options);
            ctx.EmitCall(typeof(BclHelpers).GetMethod(nameof(BclHelpers.ReadNetObject),
                new[] { Compiler.CompilerContext.StateBasedReadMethods.ByRefStateType, typeof(object),
                    typeof(Type), typeof(BclHelpers.NetObjectOptions)}));
        }

        public void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            // BclHelpers.WriteNetObject(ref state, value, options);
            using var loc = ctx.GetLocalWithValue(ExpectedType, valueFrom);
            ctx.LoadState();
            ctx.LoadValue(loc);
            ctx.CastToObject(ExpectedType);
            ctx.LoadValue((int)options);
            ctx.EmitCall(typeof(BclHelpers).GetMethod(nameof(BclHelpers.WriteNetObject),
                new[] { Compiler.WriterUtil.ByRefStateType, typeof(object),
                    typeof(BclHelpers.NetObjectOptions)}));

        }
    }
#endif
}