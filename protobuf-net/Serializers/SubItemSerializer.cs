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
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.InjectStore(type, valueFrom);
            ctx.LoadValue(key);
            ctx.EmitWrite("WriteObject"); //TODO: box?
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.InjectStore(type, valueFrom);
            ctx.LoadValue(key);
            ctx.EmitWrite("ReadObject"); //TODO: box and unbox?
        }
#endif
    }
}
