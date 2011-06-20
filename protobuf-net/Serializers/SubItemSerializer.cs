#if !NO_RUNTIME
using System;
using ProtoBuf.Meta;


namespace ProtoBuf.Serializers
{
    sealed class SubItemSerializer : IProtoTypeSerializer
    {
        bool IProtoTypeSerializer.HasCallbacks(TypeModel.CallbackType callbackType)
        {
            return ((IProtoTypeSerializer)proxy.Serializer).HasCallbacks(callbackType);
        }
#if FEAT_COMPILER
        void IProtoTypeSerializer.EmitCallback(Compiler.CompilerContext ctx, Compiler.Local valueFrom, TypeModel.CallbackType callbackType)
        {
            ((IProtoTypeSerializer)proxy.Serializer).EmitCallback(ctx, valueFrom, callbackType);
        }
#endif
        void IProtoTypeSerializer.Callback(object value, TypeModel.CallbackType callbackType, SerializationContext context)
        {
            ((IProtoTypeSerializer)proxy.Serializer).Callback(value, callbackType, context);
        }

        private readonly int key;
        private readonly Type type;
        private readonly ISerializerProxy proxy;
        private readonly bool recursionCheck;
        public SubItemSerializer(Type type, int key, ISerializerProxy proxy, bool recursionCheck)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (proxy == null) throw new ArgumentNullException("proxy");
            this.type = type;
            this.proxy= proxy;
            this.key = key;
            this.recursionCheck = recursionCheck;
        }
        Type IProtoSerializer.ExpectedType
        {
            get { return type; }
        }
        bool IProtoSerializer.RequiresOldValue { get { return true; } }
        bool IProtoSerializer.ReturnsValue { get { return true; } }

        void IProtoSerializer.Write(object value, ProtoWriter dest)
        {
            if (recursionCheck)
            {
                ProtoWriter.WriteObject(value, key, dest);
            }
            else
            {
                ProtoWriter.WriteRecursionSafeObject(value, key, dest);
            }
        }
        object IProtoSerializer.Read(object value, ProtoReader source)
        {
            return ProtoReader.ReadObject(value, key, source);
        }
#if FEAT_COMPILER
        bool EmitDedicatedMethod(Compiler.CompilerContext ctx, Compiler.Local valueFrom, bool read)
        {
            System.Reflection.Emit.MethodBuilder method = ctx.GetDedicatedMethod(key, read);
            if (method == null) return false;
            using (Compiler.Local token = new ProtoBuf.Compiler.Local(ctx, typeof(SubItemToken)))
            {
                Type rwType = read ? typeof(ProtoReader) : typeof(ProtoWriter);
                ctx.LoadValue(valueFrom);
                if (!read) // write requires the object for StartSubItem; read doesn't
                {  // (if recursion-check is disabled [subtypes] then null is fine too)
                    if (type.IsValueType || !recursionCheck) { ctx.LoadNullRef(); }
                    else { ctx.CopyValue(); }
                }
                ctx.LoadReaderWriter();
                ctx.EmitCall(rwType.GetMethod("StartSubItem"));
                ctx.StoreValue(token);

                // note: value already on the stack
                ctx.LoadReaderWriter();                
                ctx.EmitCall(method);
                // handle inheritance (we will be calling the *base* version of things,
                // but we expect Read to return the "type" type)
                if (read && type != method.ReturnType) ctx.Cast(this.type);
                ctx.LoadValue(token);
                
                ctx.LoadReaderWriter();
                ctx.EmitCall(rwType.GetMethod("EndSubItem"));
            }            
            return true;
        }
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            if (!EmitDedicatedMethod(ctx, valueFrom, false))
            {
                ctx.LoadValue(valueFrom);
                if (type.IsValueType) ctx.CastToObject(type);
                ctx.LoadValue(key);
                ctx.LoadReaderWriter();
                ctx.EmitCall(typeof(ProtoWriter).GetMethod(recursionCheck ?  "WriteObject" : "WriteRecursionSafeObject"));
            }
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            if (!EmitDedicatedMethod(ctx, valueFrom, true))
            {
                ctx.LoadValue(valueFrom);
                if (type.IsValueType) ctx.CastToObject(type);
                ctx.LoadValue(key);
                ctx.LoadReaderWriter();
                ctx.EmitCall(typeof(ProtoReader).GetMethod("ReadObject"));
                ctx.CastFromObject(type);
            }
        }
#endif
    }
}
#endif