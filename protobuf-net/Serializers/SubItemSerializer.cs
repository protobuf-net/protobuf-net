#if !NO_RUNTIME
using System;


namespace ProtoBuf.Serializers
{
    sealed class SubItemSerializer : IProtoSerializer
    {
        private readonly Type type;
        private readonly int key;
        public SubItemSerializer(Type type, int key)
        {
            this.type = type;
            this.key = key;
        }
        Type IProtoSerializer.ExpectedType
        {
            get { return type; }
        }
        bool IProtoSerializer.RequiresOldValue { get { return true; } }
        bool IProtoSerializer.ReturnsValue { get { return true; } }

        void IProtoSerializer.Write(object value, ProtoWriter dest)
        {
            dest.WriteObject(value, key);
        }
        object IProtoSerializer.Read(object value, ProtoReader source)
        {
            return source.ReadObject(value, key);
        }
#if FEAT_COMPILER
        void EmitReaderWriterObjectAndKey(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            using (Compiler.Local loc = ctx.GetLocalWithValue(type, valueFrom))
            {
                ctx.LoadReaderWriter(); // must do this *after* storing (GetLocalWithValue) the value from the stack (if any)
                ctx.LoadValue(loc);
                if (type.IsValueType) { ctx.CastToObject(type); }
            }
            ctx.LoadValue(key);
        }
        bool EmitDedicatedMethod(Compiler.CompilerContext ctx, Compiler.Local valueFrom, bool read)
        {
            System.Reflection.Emit.MethodBuilder method = ctx.GetDedicatedMethod(key, read);
            if (method == null) return false;
            using (Compiler.Local loc = ctx.GetLocalWithValue(type, valueFrom))
            using (Compiler.Local token = new ProtoBuf.Compiler.Local(ctx, typeof(int)))
            {
                ctx.LoadReaderWriter();
                Type rwType = read ? typeof(ProtoReader) : typeof(ProtoWriter);
                if (!read)
                {
                    if (type.IsValueType) { ctx.LoadNullRef(); }
                    else { ctx.LoadValue(loc); } 
                }
                ctx.EmitCall(rwType.GetMethod("StartSubItem"));
                ctx.StoreValue(token);

                ctx.LoadValue(loc);
                ctx.LoadReaderWriter();
                ctx.EmitCall(method);
                if (read) { ctx.StoreValue(loc); }

                ctx.LoadReaderWriter();
                ctx.LoadValue(token);
                ctx.EmitCall(rwType.GetMethod("EndSubItem"));

                if (read) { ctx.LoadValue(loc); }
            }
            return true;
        }
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            if (!EmitDedicatedMethod(ctx, valueFrom, false))
            {
                EmitReaderWriterObjectAndKey(ctx, valueFrom);
                ctx.EmitWrite("WriteObject");
            }
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            if (!EmitDedicatedMethod(ctx, valueFrom, true))
            {
                EmitReaderWriterObjectAndKey(ctx, valueFrom);
                ctx.EmitCall(typeof(ProtoReader).GetMethod("ReadObject"));
                ctx.CastFromObject(type);
            }
        }
#endif
    }
}
#endif