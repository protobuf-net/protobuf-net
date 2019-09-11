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

        public NetObjectSerializer(Type type, int key, BclHelpers.NetObjectOptions options)
        {
            bool dynamicType = (options & BclHelpers.NetObjectOptions.DynamicType) != 0;
            this.key = dynamicType ? -1 : key;
            ExpectedType = dynamicType ? typeof(object) : type;
            this.options = options;
        }

        public Type ExpectedType { get; }

        public bool ReturnsValue => true;

        public bool RequiresOldValue => true;

        public object Read(ProtoReader source, ref ProtoReader.State state, object value)
        {
            return BclHelpers.ReadNetObject(source, ref state, value, key, ExpectedType == typeof(object) ? null : ExpectedType, options);
        }

        public void Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
        {
            BclHelpers.WriteNetObject(value, dest, ref state, key, options);
        }

#if FEAT_COMPILER
        public void EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            using (var val = ctx.GetLocalWithValue(ExpectedType, entity))
            {
                ctx.LoadReader(true);
                ctx.LoadValue(val);
                ctx.CastToObject(ExpectedType);
                ctx.LoadValue(ctx.MapMetaKeyToCompiledKey(key));
                if (ExpectedType == typeof(object)) ctx.LoadNullRef();
                else ctx.LoadValue(ExpectedType);
                ctx.LoadValue((int)options);

                ctx.EmitCall(typeof(BclHelpers).GetMethod("ReadNetObject",
                    new[] { typeof(ProtoReader), Compiler.ReaderUtil.ByRefStateType, typeof(object),
                    typeof(int), typeof(Type), typeof(BclHelpers.NetObjectOptions)}));
                ctx.CastFromObject(ExpectedType);
            }
        }
        public void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadValue(valueFrom);
            ctx.CastToObject(ExpectedType);
            ctx.LoadWriter(true);
            ctx.LoadValue(ctx.MapMetaKeyToCompiledKey(key));
            ctx.LoadValue((int)options);
            ctx.EmitCall(Compiler.WriterUtil.GetStaticMethod<BclHelpers>("WriteNetObject"));
        }
#endif
    }
}
#endif