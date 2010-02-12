using System;
using System.Reflection;
using ProtoBuf.Compiler;

namespace ProtoBuf.Serializers
{
    sealed class PropertyDecorator : ProtoDecoratorBase
    {
        public override Type ExpectedType { get { return property.DeclaringType; } }
        private readonly PropertyInfo property;
        public PropertyDecorator(PropertyInfo property, IProtoSerializer tail)
            : base(tail)
        {
            if (property == null) throw new ArgumentNullException("property");
            this.property = property;
        }
        public override void Write(object value, ProtoWriter dest)
        {
            value = property.GetValue(value, null);
            if(value != null) Tail.Write(value, dest);
        }
        protected override void Write(CompilerContext ctx)
        {
            ctx.LoadValue(property);
            ctx.NullCheckedTail(property.PropertyType, Tail);
        }
    }
}
