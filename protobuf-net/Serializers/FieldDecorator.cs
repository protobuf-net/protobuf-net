#if !NO_RUNTIME
using System;
using System.Reflection;



namespace ProtoBuf.Serializers
{
    sealed class FieldDecorator : ProtoDecoratorBase
    {

        public override Type ExpectedType { get { return field.DeclaringType; } }
        private readonly FieldInfo field;
        public override bool RequiresOldValue { get { return true; } }
        public override bool ReturnsValue { get { return false; } }
        public FieldDecorator(FieldInfo field, IProtoSerializer tail) : base(tail)
        {
            Helpers.DebugAssert(field != null);
            this.field = field;
        }
        public override void Write(object value, ProtoWriter dest)
        {
            Helpers.DebugAssert(value != null);
            value = field.GetValue(value);
            if(value != null) Tail.Write(value, dest);
        }
        public override object Read(object value, ProtoReader source)
        {
            Helpers.DebugAssert(value != null);
            field.SetValue(
                value,
                Tail.Read((Tail.RequiresOldValue ? field.GetValue(value) : null), source));
            return null;
        }
#if FEAT_COMPILER
        protected override void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadAddress(valueFrom, ExpectedType);
            ctx.LoadValue(field);
            ctx.WriteNullCheckedTail(field.FieldType, Tail, null);
        }
        protected override void EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            using (Compiler.Local loc = ctx.GetLocalWithValue(ExpectedType, valueFrom))
            {
                ctx.LoadAddress(loc, ExpectedType);
                if (Tail.RequiresOldValue)
                {
                    ctx.CopyValue();
                    ctx.LoadValue(field);  
                }
                // value is either now on the stack or not needed
                ctx.ReadNullCheckedTail(field.FieldType, Tail, null);
                // stack is now the return value
                ctx.StoreValue(field);
            }
        }
#endif
    }
}
#endif