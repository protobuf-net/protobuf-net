using System;
using ProtoBuf.Compiler;

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

        void IProtoSerializer.Write(CompilerContext ctx)
        {
            ctx.InjectStore(type);
            ctx.LoadValue(key);
            ctx.EmitWrite("WriteObject");
        }
    }
}
