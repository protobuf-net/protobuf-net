using System;


namespace ProtoBuf.Serializers
{
    sealed class TagDecorator : ProtoDecoratorBase
    {
        public override Type ExpectedType
        {
            get { return Tail.ExpectedType; }
        }
        public TagDecorator(int fieldNumber, WireType wireType, IProtoSerializer tail)
            : base(tail)
        {
            this.fieldNumber = fieldNumber;
            this.wireType = wireType;
        }
        
        private readonly int fieldNumber;
        private readonly WireType wireType;
        public override void Write(object value, ProtoWriter dest)
        {
            dest.WriteFieldHeader(fieldNumber, wireType);
            Tail.Write(value, dest);
        }
#if FEAT_COMPILER
        protected override void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            using (Compiler.Local loc = ctx.GetLocalWithValue(Tail.ExpectedType, valueFrom))
            {
                ctx.LoadDest();
                ctx.LoadValue((int)fieldNumber);
                ctx.LoadValue((int)wireType);
                ctx.EmitWrite("WriteFieldHeader");
                Tail.EmitWrite(ctx, loc);
            }            
        }
#endif
    }
    
}
