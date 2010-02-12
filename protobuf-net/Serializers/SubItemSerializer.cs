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

        void IProtoSerializer.Write(object value, ProtoWriter dest)
        {
            dest.WriteObject(value, key);
        }
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.InjectStore(type, valueFrom);
            ctx.LoadValue(key);
            ctx.EmitWrite("WriteObject");
        }
#endif
    }
}
