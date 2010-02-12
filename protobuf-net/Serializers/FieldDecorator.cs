using System;
using System.Reflection;
using System.Diagnostics;


namespace ProtoBuf.Serializers
{
    sealed class FieldDecorator : ProtoDecoratorBase
    {

        public override Type ExpectedType { get { return field.DeclaringType; } }
        private readonly FieldInfo field;
        public FieldDecorator(FieldInfo field, IProtoSerializer tail)
            : base(tail)
        {
            Debug.Assert(field != null);
            this.field = field;
        }
        public override void Write(object value, ProtoWriter dest)
        {
            value = field.GetValue(value);
            if(value != null) Tail.Write(value, dest);
        }
#if FEAT_COMPILER
        protected override void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadAddress(valueFrom, ExpectedType);
            ctx.LoadValue(field);
            ctx.NullCheckedTail(field.FieldType, Tail, null);
        }
#endif
    }
}
