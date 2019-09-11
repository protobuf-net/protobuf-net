#if !NO_RUNTIME
using System;
using ProtoBuf.Meta;

#if FEAT_COMPILER
using System.Reflection.Emit;
#endif

namespace ProtoBuf.Serializers
{
    internal sealed class SubItemSerializer : IProtoTypeSerializer
    {
        bool IProtoTypeSerializer.HasCallbacks(TypeModel.CallbackType callbackType)
        {
            return ((IProtoTypeSerializer)proxy.Serializer).HasCallbacks(callbackType);
        }

        bool IProtoTypeSerializer.CanCreateInstance()
        {
            return ((IProtoTypeSerializer)proxy.Serializer).CanCreateInstance();
        }

#if FEAT_COMPILER
        void IProtoTypeSerializer.EmitCallback(Compiler.CompilerContext ctx, Compiler.Local valueFrom, TypeModel.CallbackType callbackType)
        {
            ((IProtoTypeSerializer)proxy.Serializer).EmitCallback(ctx, valueFrom, callbackType);
        }

        void IProtoTypeSerializer.EmitCreateInstance(Compiler.CompilerContext ctx)
        {
            ((IProtoTypeSerializer)proxy.Serializer).EmitCreateInstance(ctx);
        }
#endif

        void IProtoTypeSerializer.Callback(object value, TypeModel.CallbackType callbackType, SerializationContext context)
        {
            ((IProtoTypeSerializer)proxy.Serializer).Callback(value, callbackType, context);
        }

        object IProtoTypeSerializer.CreateInstance(ProtoReader source)
        {
            return ((IProtoTypeSerializer)proxy.Serializer).CreateInstance(source);
        }

        private readonly int key;
        private readonly Type type;
        private readonly ISerializerProxy proxy;
        private readonly bool recursionCheck;
        public SubItemSerializer(Type type, int key, ISerializerProxy proxy, bool recursionCheck)
        {
            this.type = type ?? throw new ArgumentNullException(nameof(type));
            this.proxy = proxy ?? throw new ArgumentNullException(nameof(proxy));
            this.key = key;
            this.recursionCheck = recursionCheck;
        }

        Type IProtoSerializer.ExpectedType => type;

        bool IProtoSerializer.RequiresOldValue => true;

        bool IProtoSerializer.ReturnsValue => true;

        void IProtoSerializer.Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
        {
            if (recursionCheck)
            {
                ProtoWriter.WriteObject(value, key, dest, ref state);
            }
            else
            {
                ProtoWriter.WriteRecursionSafeObject(value, key, dest, ref state);
            }
        }

        object IProtoSerializer.Read(ProtoReader source, ref ProtoReader.State state, object value)
        {
            return ProtoReader.ReadObject(value, key, source, ref state);
        }

#if FEAT_COMPILER
        private bool EmitDedicatedMethod(Compiler.CompilerContext ctx, Compiler.Local valueFrom, bool read)
        {
            MethodBuilder method = ctx.GetDedicatedMethod(key, read);
            if (method == null) return false;

            using (Compiler.Local val = ctx.GetLocalWithValue(type, valueFrom))
            using (Compiler.Local token = new ProtoBuf.Compiler.Local(ctx, typeof(SubItemToken)))
            {
                Type rwType = read ? typeof(ProtoReader) : typeof(ProtoWriter);

                if (read)
                {
                    ctx.LoadReader(true);
                }
                else
                {
                    // write requires the object for StartSubItem; read doesn't
                    // (if recursion-check is disabled [subtypes] then null is fine too)
                    if (Helpers.IsValueType(type) || !recursionCheck) { ctx.LoadNullRef(); }
                    else { ctx.LoadValue(val); }
                    ctx.LoadWriter(true);
                }
                ctx.EmitCall(Helpers.GetStaticMethod(rwType, "StartSubItem",
                    read ? Compiler.ReaderUtil.ReaderStateTypeArray
                    : new Type[] { typeof(object), rwType, Compiler.WriterUtil.ByRefStateType }));
                ctx.StoreValue(token);

                if (read)
                {
                    ctx.LoadReader(true);
                    ctx.LoadValue(val);
                }
                else
                {
                    ctx.LoadWriter(true);
                    ctx.LoadValue(val);
                }
                ctx.EmitCall(method);
                // handle inheritance (we will be calling the *base* version of things,
                // but we expect Read to return the "type" type)
                if (read && type != method.ReturnType) ctx.Cast(type);
                ctx.LoadValue(token);
                if (read)
                {
                    ctx.LoadReader(true);
                    ctx.EmitCall(Helpers.GetStaticMethod(rwType, "EndSubItem",
                        new Type[] { typeof(SubItemToken), rwType, Compiler.ReaderUtil.ByRefStateType }));
                }
                else
                {
                    ctx.LoadWriter(true);
                    ctx.EmitCall(Compiler.WriterUtil.GetStaticMethod("EndSubItem"));
                }
            }
            return true;
        }
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            if (!EmitDedicatedMethod(ctx, valueFrom, false))
            {
                ctx.LoadValue(valueFrom);
                if (Helpers.IsValueType(type)) ctx.CastToObject(type);
                ctx.LoadValue(ctx.MapMetaKeyToCompiledKey(key)); // re-map for formality, but would expect identical, else dedicated method
                ctx.LoadWriter(true);
                ctx.EmitCall(Helpers.GetStaticMethod(typeof(ProtoWriter), recursionCheck ? "WriteObject" : "WriteRecursionSafeObject", new Type[] { typeof(object), typeof(int), typeof(ProtoWriter), Compiler.WriterUtil.ByRefStateType }));
            }
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            if (!EmitDedicatedMethod(ctx, valueFrom, true))
            {
                ctx.LoadValue(valueFrom);
                if (Helpers.IsValueType(type)) ctx.CastToObject(type);
                ctx.LoadValue(ctx.MapMetaKeyToCompiledKey(key)); // re-map for formality, but would expect identical, else dedicated method
                ctx.LoadReader(true);
                ctx.EmitCall(Helpers.GetStaticMethod(typeof(ProtoReader), "ReadObject",
                    new[] { typeof(object), typeof(int), typeof(ProtoReader), Compiler.ReaderUtil.ByRefStateType }));
                ctx.CastFromObject(type);
            }
        }
#endif
    }
}
#endif