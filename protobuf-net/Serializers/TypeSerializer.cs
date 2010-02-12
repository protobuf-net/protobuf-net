using System;
using System.Diagnostics;


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
            Debug.Assert(forType != null);
            Debug.Assert(fieldNumbers != null);
            Debug.Assert(serializers != null);
            Debug.Assert(fieldNumbers.Length == serializers.Length);
            this.forType = forType;
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
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            Type expected = ExpectedType;
            using (Compiler.Local loc = ctx.GetLocalWithValue(expected, valueFrom))
            {
                for (int i = 0; i < serializers.Length; i++)
                {
                    serializers[i].EmitWrite(ctx, loc);
                }
            }
        }
#endif
    }

}
