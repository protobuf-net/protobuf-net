#if !NO_RUNTIME
using System;

namespace ProtoBuf.Serializers
{
    sealed class NetObjectSerializer : IProtoSerializer
    {
        private readonly int key;
        private readonly Type type;
        private readonly bool asReference;
        private readonly bool dynamicType;
        public NetObjectSerializer(Type type, int key, bool asReference, bool dynamicType)
        {
            this.key = dynamicType ? -1 : key;
            this.type = dynamicType ? typeof(object) : type;
            this.asReference = asReference;
            this.dynamicType = dynamicType;
        }

        public Type ExpectedType
        {
            get { return type; }
        }
        public bool ReturnsValue
        {
            get { return true; }
        }
        public bool RequiresOldValue
        {
            get { return true; }
        }
        
        public object Read(object value, ProtoReader source)
        {
            return BclHelpers.ReadNetObject(value, source, key, type == typeof(object) ? null : type);
        }
        public void Write(object value, ProtoWriter dest)
        {
            BclHelpers.WriteNetObject(value, dest, key, dynamicType, asReference);
        }

#if FEAT_COMPILER
        public void EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadValue(valueFrom);
            ctx.CastToObject(type);
            ctx.LoadReaderWriter();
            ctx.LoadValue(key);
            if (type == typeof(object)) ctx.LoadNullRef();
            else ctx.LoadValue(type);
            ctx.EmitCall(typeof(BclHelpers).GetMethod("ReadNetObject"));
            ctx.CastFromObject(type);
        }
        public void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadValue(valueFrom);
            ctx.CastToObject(type);
            ctx.LoadReaderWriter();
            ctx.LoadValue(key);
            ctx.LoadValue(dynamicType);
            ctx.LoadValue(asReference);
            ctx.EmitCall(typeof(BclHelpers).GetMethod("WriteNetObject"));
        }
#endif
    }
}
#endif