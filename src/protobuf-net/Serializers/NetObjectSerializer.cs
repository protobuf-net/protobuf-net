#if !NO_RUNTIME
using System;
using System.Reflection;
using ProtoBuf.Meta;

namespace ProtoBuf.Serializers
{
    internal sealed class NetObjectSerializer : IProtoSerializer
    {
        private readonly int key;

        private readonly BclHelpers.NetObjectOptions options;

        public NetObjectSerializer(TypeModel model, Type type, int key, BclHelpers.NetObjectOptions options)
        {
            bool dynamicType = (options & BclHelpers.NetObjectOptions.DynamicType) != 0;
            this.key = dynamicType ? -1 : key;
            ExpectedType = dynamicType ? model.MapType(typeof(object)) : type;
            this.options = options;
        }

        public Type ExpectedType { get; }

        public bool ReturnsValue => true;

        public bool RequiresOldValue => true;

        public object Read(ref ProtoReader.State state, object value, ProtoReader source)
        {
            return BclHelpers.ReadNetObject(ref state, value, source, key, ExpectedType == typeof(object) ? null : ExpectedType, options);
        }

        public void Write(object value, ProtoWriter dest)
        {
            BclHelpers.WriteNetObject(value, dest, key, options);
        }

#if FEAT_COMPILER
        public void EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.LoadValue(entity);
            ctx.CastToObject(ExpectedType);
            ctx.LoadReaderWriter();
            ctx.LoadValue(ctx.MapMetaKeyToCompiledKey(key));
            if (ExpectedType == ctx.MapType(typeof(object))) ctx.LoadNullRef();
            else ctx.LoadValue(ExpectedType);
            ctx.LoadValue((int)options);
            ctx.EmitCall(ctx.MapType(typeof(BclHelpers)).GetMethod("ReadNetObject"));
            ctx.CastFromObject(ExpectedType);
        }
        public void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadValue(valueFrom);
            ctx.CastToObject(ExpectedType);
            ctx.LoadReaderWriter();
            ctx.LoadValue(ctx.MapMetaKeyToCompiledKey(key));
            ctx.LoadValue((int)options);
            ctx.EmitCall(ctx.MapType(typeof(BclHelpers)).GetMethod("WriteNetObject"));
        }
#endif
    }
}
#endif