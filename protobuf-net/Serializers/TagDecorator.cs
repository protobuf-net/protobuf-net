#if !NO_RUNTIME
using System;

using System.Reflection;
using ProtoBuf.Meta;


namespace ProtoBuf.Serializers
{
    sealed class TagDecorator : ProtoDecoratorBase, IProtoTypeSerializer
    {

        public bool HasCallbacks(TypeModel.CallbackType callbackType)
        {
            IProtoTypeSerializer pts = Tail as IProtoTypeSerializer;
            return pts != null && pts.HasCallbacks(callbackType);
        }
        public void Callback(object value, TypeModel.CallbackType callbackType, SerializationContext context)
        {
            IProtoTypeSerializer pts = Tail as IProtoTypeSerializer;
            if (pts != null) pts.Callback(value, callbackType, context);
        }
#if FEAT_COMPILER
        public void EmitCallback(Compiler.CompilerContext ctx, Compiler.Local valueFrom, TypeModel.CallbackType callbackType)
        {
            // we only expect this to be invoked if HasCallbacks returned true, so implicitly Tail
            // **must** be of the correct type
            ((IProtoTypeSerializer)Tail).EmitCallback(ctx, valueFrom, callbackType);
        }
#endif
        public override Type ExpectedType
        {
            get { return Tail.ExpectedType; }
        }
        public TagDecorator(int fieldNumber, WireType wireType, bool strict, IProtoSerializer tail)
            : base(tail)
        {
            this.fieldNumber = fieldNumber;
            this.wireType = wireType;
            this.strict = strict;
        }
        public override bool RequiresOldValue { get { return Tail.RequiresOldValue; } }
        public override bool ReturnsValue { get { return Tail.ReturnsValue; } }
        private readonly bool strict;
        private readonly int fieldNumber;
        private readonly WireType wireType;
        public override void Write(object value, ProtoWriter dest)
        {
            ProtoWriter.WriteFieldHeader(fieldNumber, wireType, dest);
            Tail.Write(value, dest);
        }
        private bool NeedsHint
        {
            get { return ((int)wireType & ~7) != 0; }
        }
        public override object Read(object value, ProtoReader source)
        {
            Helpers.DebugAssert(fieldNumber == source.FieldNumber);
            if (strict) { source.Assert(wireType); }
            else if (NeedsHint) { source.Hint(wireType); }
            return Tail.Read(value, source);
        }
#if FEAT_COMPILER
        protected override void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadValue((int)fieldNumber);
            ctx.LoadValue((int)wireType);
            ctx.LoadReaderWriter();
            ctx.EmitCall(typeof(ProtoWriter).GetMethod("WriteFieldHeader"));
            Tail.EmitWrite(ctx, valueFrom);    
        }
        protected override void EmitRead(ProtoBuf.Compiler.CompilerContext ctx, ProtoBuf.Compiler.Local valueFrom)
        {
            if (strict || NeedsHint)
            {
                ctx.LoadReaderWriter();
                ctx.LoadValue((int)wireType);
                ctx.EmitCall(typeof(ProtoReader).GetMethod(strict ? "Assert" : "Hint"));
            }
            Tail.EmitRead(ctx, valueFrom);
        }
#endif
    }
    
}
#endif