using System;
using ProtoBuf.Compiler;

namespace ProtoBuf.Serializers
{
    sealed class TypeSerializer : IProtoSerializer
    {
        private readonly Type forType;
        public Type ExpectedType { get { return forType; } }
        private readonly IProtoSerializer[] serializers;
        private readonly int[] fieldNumbers;
        public TypeSerializer(Type forType, int[] fieldNumbers, IProtoSerializer[] serializers)
        {
            if (forType == null) throw new ArgumentNullException("forType");
            this.forType = forType;
            if (fieldNumbers == null) throw new ArgumentNullException("fieldNumbers");
            if (serializers == null) throw new ArgumentNullException("serializers");
            if (fieldNumbers.Length != serializers.Length) throw new InvalidOperationException();
            this.serializers = serializers;
            this.fieldNumbers = fieldNumbers;
        }
        public void Write(object value, ProtoWriter dest)
        {
            // write all suitable fields
            for (int i = 0; i < serializers.Length; i++)
            {
                serializers[i].Write(value, dest);
            }
        }
        void IProtoSerializer.Write(CompilerContext ctx)
        {
            Type expected = ExpectedType;
            // for a reference, we can just copy the reference and access members in turn;
            // for a struct, we need to write it to a variable so we can use "ldloca"
            // and access members via the address
            using (Local entity = expected.IsValueType ? ctx.GetLocal(expected) : null)
            {
                if (entity != null) { ctx.StoreValue(entity); }
                for (int i = 0; i < serializers.Length; i++)
                {
                    if (expected.IsValueType)
                    {
                        ctx.LoadAddress(entity);
                    }
                    else
                    {
                        ctx.CopyValue();
                    }
                    serializers[i].Write(ctx);
                }
                if (entity == null) { ctx.DiscardValue(); }
            }
        }
    }

}
