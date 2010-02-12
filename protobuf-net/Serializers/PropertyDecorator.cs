using System;
using System.Reflection;
using System.Diagnostics;


namespace ProtoBuf.Serializers
{
    sealed class PropertyDecorator : ProtoDecoratorBase
    {
        public override Type ExpectedType { get { return property.DeclaringType; } }
        private readonly PropertyInfo property;
        public PropertyDecorator(PropertyInfo property, IProtoSerializer tail)
            : base(tail)
        {
            Debug.Assert(property != null);
            this.property = property;
        }
        public override void Write(object value, ProtoWriter dest)
        {
            value = property.GetValue(value, null);
            if(value != null) Tail.Write(value, dest);
        }
#if FEAT_COMPILER
        protected override void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadAddress(valueFrom, ExpectedType);
            ctx.LoadValue(property);
            ctx.NullCheckedTail(property.PropertyType, Tail, null);
        }
#endif
    }
}
