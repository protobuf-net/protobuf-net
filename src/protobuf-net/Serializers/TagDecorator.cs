#if !NO_RUNTIME
using System;
using System.Reflection;

using ProtoBuf.Meta;

namespace ProtoBuf.Serializers
{
    internal sealed class TagDecorator : ProtoDecoratorBase, IProtoTypeSerializer
    {
        public bool HasCallbacks(TypeModel.CallbackType callbackType)
        {
            return Tail is IProtoTypeSerializer pts && pts.HasCallbacks(callbackType);
        }

        public bool CanCreateInstance()
        {
            return Tail is IProtoTypeSerializer pts && pts.CanCreateInstance();
        }

        public object CreateInstance(ProtoReader source)
        {
            return ((IProtoTypeSerializer)Tail).CreateInstance(source);
        }

        public void Callback(object value, TypeModel.CallbackType callbackType, SerializationContext context)
        {
            if (Tail is IProtoTypeSerializer pts)
            {
                pts.Callback(value, callbackType, context);
            }
        }

#if FEAT_COMPILER
        public void EmitCallback(Compiler.CompilerContext ctx, Compiler.Local valueFrom, TypeModel.CallbackType callbackType)
        {
            // we only expect this to be invoked if HasCallbacks returned true, so implicitly Tail
            // **must** be of the correct type
            ((IProtoTypeSerializer)Tail).EmitCallback(ctx, valueFrom, callbackType);
        }

        public void EmitCreateInstance(Compiler.CompilerContext ctx)
        {
            ((IProtoTypeSerializer)Tail).EmitCreateInstance(ctx);
        }
#endif
        public override Type ExpectedType => Tail.ExpectedType;

        public TagDecorator(int fieldNumber, WireType wireType, bool strict, IProtoSerializer tail)
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

        public override object Read(ProtoReader source, ref ProtoReader.State state, object value)
        {
            Helpers.DebugAssert(fieldNumber == source.FieldNumber);
            if (strict) { source.Assert(ref state, wireType); }
            else if (NeedsHint) { source.Hint(wireType); }
            return Tail.Read(source, ref state, value);
        }

        public override void Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
        {
            ProtoWriter.WriteFieldHeader(fieldNumber, wireType, dest, ref state);
            Tail.Write(dest, ref state, value);
        }

#if FEAT_COMPILER
        protected override void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadValue((int)fieldNumber);
            ctx.LoadValue((int)wireType);
            ctx.LoadWriter(true);
            ctx.EmitCall(Compiler.WriterUtil.GetStaticMethod("WriteFieldHeader"));
            Tail.EmitWrite(ctx, valueFrom);
        }

        protected override void EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            if (strict || NeedsHint)
            {
                ctx.LoadReader(strict);
                ctx.LoadValue((int)wireType);
                if (strict)
                {
                    ctx.EmitCall(typeof(ProtoReader).GetMethod("Assert",
                        new[] { Compiler.ReaderUtil.ByRefStateType, typeof(WireType) }));
                }
                else
                {
                    ctx.EmitCall(typeof(ProtoReader).GetMethod("Hint"));
                }
            }
            Tail.EmitRead(ctx, valueFrom);
        }
#endif
    }
}
#endif