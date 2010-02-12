using System;
using ProtoBuf.Compiler;

namespace ProtoBuf.Serializers
{
    abstract class ProtoDecoratorBase : IProtoSerializer
    {
        public abstract Type ExpectedType { get; }
        protected readonly IProtoSerializer Tail;
        protected ProtoDecoratorBase(IProtoSerializer tail) { this.Tail = tail; }

        public abstract void Write(object value, ProtoWriter dest);
        void IProtoSerializer.Write(CompilerContext ctx) { Write(ctx); }
        protected abstract void Write(CompilerContext ctx);
    }
}
