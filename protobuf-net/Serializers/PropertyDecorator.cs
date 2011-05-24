#if !NO_RUNTIME
using System;
using System.Reflection;



namespace ProtoBuf.Serializers
{
    sealed class PropertyDecorator : ProtoDecoratorBase
    {
        public override Type ExpectedType { get { return forType; } }
        private readonly PropertyInfo property;
        private readonly Type forType;
        public override bool RequiresOldValue { get { return true; } }
        public override bool ReturnsValue { get { return false; } }
        private readonly bool readOptionsWriteValue;
        public PropertyDecorator(Type forType, PropertyInfo property, IProtoSerializer tail) : base(tail)
        {
            Helpers.DebugAssert(forType != null);
            Helpers.DebugAssert(property != null);
            this.forType = forType;
            this.property = property;
            SanityCheck(property, tail, out readOptionsWriteValue, true);
        }
        private static void SanityCheck(PropertyInfo property, IProtoSerializer tail, out bool writeValue, bool nonPublic) {
            if(property == null) throw new ArgumentNullException("property");
            
            writeValue = tail.ReturnsValue && (GetShadowSetter(property) != null || (property.CanWrite && property.GetSetMethod(nonPublic) != null));
            if (!property.CanRead || property.GetGetMethod(nonPublic) == null) throw new InvalidOperationException("Cannot serialize property without a get accessor");
            if (!writeValue && (!tail.RequiresOldValue || tail.ExpectedType.IsValueType))
            { // so we can't save the value, and the tail doesn't use it either... not helpful
                // or: can't write the value, so the struct value will be lost
                throw new InvalidOperationException("Cannot apply changes to property " + property.DeclaringType.FullName + "." + property.Name);
            }
        }
        static MethodInfo GetShadowSetter(PropertyInfo property)
        {
            MethodInfo method = property.ReflectedType.GetMethod("Set" + property.Name, BindingFlags.Public | BindingFlags.Instance, null, new Type[] { property.PropertyType }, null);
            if (method == null || method.ReturnType != typeof(void)) return null;
            return method;
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

            object oldVal = Tail.RequiresOldValue ? property.GetValue(value, null) : null;
            object newVal = Tail.Read(oldVal, source);
            if (readOptionsWriteValue)
            {
                MethodInfo shadow = GetShadowSetter(property);
                if (shadow == null)
                {
                    property.SetValue(value, newVal, null);
                }
                else
                {
                    shadow.Invoke(value, new object[] { newVal });
                }
            }
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

            bool writeValue;
            SanityCheck(property, Tail, out writeValue, ctx.NonPublic);
            if (ExpectedType.IsValueType && valueFrom == null)
            {
                throw new InvalidOperationException("Attempt to mutate struct on the head of the stack; changes would be lost");
            }

            ctx.LoadAddress(valueFrom, ExpectedType); // stack is: old-addr
            if (writeValue && Tail.RequiresOldValue)
            { // need to read and write
                ctx.CopyValue();
            }
            // stack is: [old-addr]|old-addr
            if (Tail.RequiresOldValue)
            {
                ctx.LoadValue(property); // stack is: [old-addr]|old-value
            }
            ctx.ReadNullCheckedTail(property.PropertyType, Tail, null); // stack is [old-addr]|[new-value]
            
            if (writeValue)
            {   // stack is old-addr|new-value
                MethodInfo shadow = GetShadowSetter(property);
                if (shadow == null)
                {
                    ctx.StoreValue(property);
                }
                else
                {
                    ctx.EmitCall(shadow);
                }
            }
            else
            { // don't want return value; drop it if anything there
                // stack is [new-value]
                if (Tail.ReturnsValue) { ctx.DiscardValue(); }
            }
        }
#endif
    }
}
#endif