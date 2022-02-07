using System;

namespace ProtoBuf.Internal.Serializers
{
    internal abstract class ProtoDecoratorBase : IRuntimeProtoSerializerNode
    {
        public virtual bool IsScalar => Tail.IsScalar;
        public abstract Type ExpectedType { get; }
        protected readonly IRuntimeProtoSerializerNode Tail;
        protected ProtoDecoratorBase(IRuntimeProtoSerializerNode tail)
        {
            this.Tail = tail;
        }
        public abstract bool ReturnsValue { get; }
        public abstract bool RequiresOldValue { get; }
        public abstract void Write(ref ProtoWriter.State state, object value);
        public abstract object Read(ref ProtoReader.State state, object value);

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom) { EmitWrite(ctx, valueFrom); }
        protected abstract void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom);
        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity) { EmitRead(ctx, entity); }
        protected abstract void EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom);
    }
}