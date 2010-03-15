#if !NO_RUNTIME
using System;

using System.Reflection;


namespace ProtoBuf.Serializers
{
    sealed class TagDecorator : ProtoDecoratorBase
    {
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