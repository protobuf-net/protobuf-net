using System;

namespace ProtoBuf.Serializers
{
    abstract class ProtoDecoratorBase : IProtoSerializer
    {
        public abstract Type ExpectedType { get; }
        protected readonly IProtoSerializer Tail;
        protected ProtoDecoratorBase(IProtoSerializer tail) { this.Tail = tail; }

        public abstract void Write(object value, ProtoWriter dest);
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom) { EmitWrite(ctx, valueFrom); }
        protected abstract void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom);
#endif
    }
}
