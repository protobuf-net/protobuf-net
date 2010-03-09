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
            ProtoWriter.WriteObject(value, key, dest);
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

            // this is pretty optimised (to reduce the number of locals that are typed for each
            // type in the model), and is not particularly reflector-friendly
            // see: http://marcgravell.blogspot.com/2010/03/last-will-be-first-and-first-will-be.html
            using (Compiler.Local token = new ProtoBuf.Compiler.Local(ctx, typeof(SubItemToken)))
            {
                Type rwType = read ? typeof(ProtoReader) : typeof(ProtoWriter);
                ctx.LoadValue(valueFrom);
                if (!read) // write requires the object for StartSubItem; read doesn't
                {
                    if (type.IsValueType) { ctx.LoadNullRef(); }
                    else { ctx.CopyValue(); }
                }
                ctx.LoadReaderWriter();
                ctx.EmitCall(rwType.GetMethod("StartSubItem"));
                ctx.StoreValue(token);

                // note: value already on the stack
                ctx.LoadReaderWriter();
                ctx.EmitCall(method);

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
                ctx.EmitCall(typeof(ProtoWriter).GetMethod("WriteObject"));
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