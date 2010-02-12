using System;
using ProtoBuf.Compiler;

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
        protected override void Write(CompilerContext ctx)
        {
            using (Local loc = ctx.GetLocal(Tail.ExpectedType))
            {
                ctx.StoreValue(loc);
                ctx.LoadDest();
                ctx.LoadValue((int)fieldNumber);
                ctx.LoadValue((int)wireType);
                ctx.EmitWrite("WriteFieldHeader");
                ctx.LoadValue(loc);
            }
            Tail.Write(ctx);
        }
    }
    
}
