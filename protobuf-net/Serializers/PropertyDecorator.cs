#if !NO_RUNTIME
using System;
using System.Reflection;



namespace ProtoBuf.Serializers
{
    sealed class PropertyDecorator : ProtoDecoratorBase
    {
        public override Type ExpectedType { get { return property.DeclaringType; } }
        private readonly PropertyInfo property;
        public override bool RequiresOldValue { get { return true; } }
        public override bool ReturnsValue { get { return false; } }
        public PropertyDecorator(PropertyInfo property, IProtoSerializer tail) : base(tail)
        {
            Helpers.DebugAssert(property != null);
            this.property = property;
        }
        public override void Write(object value, ProtoWriter dest)
        {
            Helpers.DebugAssert(value != null);
            value = property.GetValue(value, null);
            if(value != null) Tail.Write(value, dest);
        }
        public override object Read(object value, ProtoReader source)
        {
            Helpers.DebugAssert(value != null);
            property.SetValue(
                value,
                Tail.Read((Tail.RequiresOldValue ? property.GetValue(value, null) : null), source),
                null);
            return null;
        }
#if FEAT_COMPILER
        protected override void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadAddress(valueFrom, ExpectedType);
            ctx.LoadValue(property);
            ctx.WriteNullCheckedTail(property.PropertyType, Tail, null);
        }
        protected override void EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            using (Compiler.Local loc = ctx.GetLocalWithValue(ExpectedType, valueFrom))
            {
                ctx.LoadAddress(loc, ExpectedType);
                if (Tail.RequiresOldValue)
                {
                    ctx.CopyValue();
                    ctx.LoadValue(property);
                }
                // value is either now on the stack or not needed
                ctx.ReadNullCheckedTail(property.PropertyType, Tail, null);
                // stack is now the return value
                ctx.StoreValue(property);
            }
        }
#endif
    }
}
#endif