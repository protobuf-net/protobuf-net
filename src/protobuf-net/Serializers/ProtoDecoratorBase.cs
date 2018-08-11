#if !NO_RUNTIME
using System;

namespace ProtoBuf.Serializers
{
    internal abstract class ProtoDecoratorBase : IProtoSerializer
    {
        public abstract Type ExpectedType { get; }
        protected readonly IProtoSerializer Tail;
        protected ProtoDecoratorBase(IProtoSerializer tail) { this.Tail = tail; }
        public abstract bool ReturnsValue { get; }
        public abstract bool RequiresOldValue { get; }
        public abstract void Write(ProtoWriter dest, ref ProtoWriter.State state, object value);
        public abstract object Read(ProtoReader source, ref ProtoReader.State state, object value);

#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom) { EmitWrite(ctx, valueFrom); }
        protected abstract void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom);
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity) { EmitRead(ctx, entity); }
        protected abstract void EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom);
#endif
    }
}
#endif