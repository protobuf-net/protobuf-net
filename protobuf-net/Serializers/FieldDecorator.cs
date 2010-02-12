using System;
using System.Reflection;
using ProtoBuf.Compiler;

namespace ProtoBuf.Serializers
{
    sealed class FieldDecorator : ProtoDecoratorBase
    {

        public override Type ExpectedType { get { return field.DeclaringType; } }
        private readonly FieldInfo field;
        public FieldDecorator(FieldInfo field, IProtoSerializer tail)
            : base(tail)
        {
            if (field == null) throw new ArgumentNullException("field");
            this.field = field;
        }
        public override void Write(object value, ProtoWriter dest)
        {
            value = field.GetValue(value);
            if(value != null) Tail.Write(value, dest);
        }
        protected override void Write(CompilerContext ctx)
        {
            ctx.LoadValue(field);
            ctx.NullCheckedTail(field.FieldType, Tail);
        }
    }
}
