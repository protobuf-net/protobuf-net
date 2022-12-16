using System;
using System.Diagnostics;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

namespace ProtoBuf.Internal.Serializers
{
    internal sealed class TagDecorator : ProtoDecoratorBase, IProtoTypeSerializer
    {
        SerializerFeatures IProtoTypeSerializer.Features => wireType.AsFeatures();
        bool IProtoTypeSerializer.IsSubType => Tail is IProtoTypeSerializer pts && pts.IsSubType;
        public bool HasCallbacks(TypeModel.CallbackType callbackType) => Tail is IProtoTypeSerializer pts && pts.HasCallbacks(callbackType);

        public bool CanCreateInstance() => Tail is IProtoTypeSerializer pts && pts.CanCreateInstance();

        public object CreateInstance(ISerializationContext source) => ((IProtoTypeSerializer)Tail).CreateInstance(source);

        public void Callback(object value, TypeModel.CallbackType callbackType, ISerializationContext context)
            => (Tail as IProtoTypeSerializer)?.Callback(value, callbackType, context);
            
        public void EmitCallback(Compiler.CompilerContext ctx, Compiler.Local valueFrom, TypeModel.CallbackType callbackType)
        {
            // we only expect this to be invoked if HasCallbacks returned true, so implicitly Tail
            // **must** be of the correct type
            ((IProtoTypeSerializer)Tail).EmitCallback(ctx, valueFrom, callbackType);
        }

        public void EmitCreateInstance(Compiler.CompilerContext ctx, bool callNoteObject)
        {
            ((IProtoTypeSerializer)Tail).EmitCreateInstance(ctx, callNoteObject);
        }

        bool IProtoTypeSerializer.ShouldEmitCreateInstance => Tail is IProtoTypeSerializer pts && pts.ShouldEmitCreateInstance;

        public override Type ExpectedType => Tail.ExpectedType;
        Type IProtoTypeSerializer.BaseType => ExpectedType;

        public TagDecorator(int fieldNumber, WireType wireType, bool strict, IRuntimeProtoSerializerNode tail)
            : base(tail)
        {
            this.fieldNumber = fieldNumber;
            this.wireType = wireType;
            this.strict = strict;
        }

        public override bool RequiresOldValue => Tail.RequiresOldValue;

        public override bool ReturnsValue => Tail.ReturnsValue;

        private readonly bool strict;
        private readonly int fieldNumber;
        private readonly WireType wireType;

        private bool NeedsHint => ((int)wireType & ~7) != 0;

        public override object Read(ref ProtoReader.State state, object value)
        {
            Debug.Assert(fieldNumber == state.FieldNumber);
            if (strict) { state.Assert(wireType); }
            else if (NeedsHint) { state.Hint(wireType); }
            return Tail.Read(ref state, value);
        }

        public override void Write(ref ProtoWriter.State state, object value)
        {
            if (Tail is IDirectRuntimeWriteNode dw && dw.CanDirectWrite(wireType))
            {
                dw.DirectWrite(fieldNumber, wireType, ref state, value);
            }
            else
            {
                state.WriteFieldHeader(fieldNumber, wireType);
                Tail.Write(ref state, value);
            }
        }

        bool IProtoTypeSerializer.HasInheritance => false;

        void IProtoTypeSerializer.EmitReadRoot(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
            => EmitRead(ctx, valueFrom);

        void IProtoTypeSerializer.EmitWriteRoot(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
            => EmitWrite(ctx, valueFrom);

        protected override void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            if (Tail is IDirectWriteNode dw && dw.CanEmitDirectWrite(wireType))
            {
                dw.EmitDirectWrite(fieldNumber, wireType, ctx, valueFrom);
            }
            else
            {
                ctx.LoadState();
                ctx.LoadValue((int)fieldNumber);
                ctx.LoadValue((int)wireType);
                ctx.EmitCall(typeof(ProtoWriter.State).GetMethod(nameof(ProtoWriter.State.WriteFieldHeader)));
                Tail.EmitWrite(ctx, valueFrom);
            }
        }

        public bool CanEmitDirectWrite()
            => Tail is IDirectWriteNode dw && dw.CanEmitDirectWrite(wireType);

        public void EmitDirectWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
            => ((IDirectWriteNode)Tail).EmitDirectWrite(fieldNumber, wireType, ctx, valueFrom);

        protected override void EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            if (strict || NeedsHint)
            {
                ctx.LoadState();
                ctx.LoadValue((int)wireType);
                string name = strict ? nameof(ProtoReader.State.Assert) : nameof(ProtoReader.State.Hint);
                ctx.EmitCall(typeof(ProtoReader.State).GetMethod(name, new[] { typeof(WireType) }));
            }
            Tail.EmitRead(ctx, valueFrom);
        }
    }
}