#if !NO_RUNTIME
using System;
using System.Reflection;

using ProtoBuf.Meta;

namespace ProtoBuf.Serializers
{
    internal sealed class PropertyDecorator : ProtoDecoratorBase
    {
        public override Type ExpectedType { get; }
        private readonly PropertyInfo property;
        public override bool RequiresOldValue => true;
        public override bool ReturnsValue => false;
        private readonly bool readOptionsWriteValue;
        private readonly MethodInfo shadowSetter;

        public PropertyDecorator(Type forType, PropertyInfo property, IProtoSerializer tail) : base(tail)
        {
            Helpers.DebugAssert(forType != null);
            Helpers.DebugAssert(property != null);
            ExpectedType = forType;
            this.property = property;
            SanityCheck(property, tail, out readOptionsWriteValue, true, true);
            shadowSetter = GetShadowSetter(property);
        }

        private static void SanityCheck(PropertyInfo property, IProtoSerializer tail, out bool writeValue, bool nonPublic, bool allowInternal)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            writeValue = tail.ReturnsValue && (GetShadowSetter(property) != null || (property.CanWrite && Helpers.GetSetMethod(property, nonPublic, allowInternal) != null));
            if (!property.CanRead || Helpers.GetGetMethod(property, nonPublic, allowInternal) == null)
            {
                throw new InvalidOperationException("Cannot serialize property without a get accessor");
            }
            if (!writeValue && (!tail.RequiresOldValue || Helpers.IsValueType(tail.ExpectedType)))
            { // so we can't save the value, and the tail doesn't use it either... not helpful
                // or: can't write the value, so the struct value will be lost
                throw new InvalidOperationException("Cannot apply changes to property " + property.DeclaringType.FullName + "." + property.Name);
            }
        }
        private static MethodInfo GetShadowSetter(PropertyInfo property)
        {
            Type reflectedType = property.ReflectedType;
            MethodInfo method = Helpers.GetInstanceMethod(reflectedType, "Set" + property.Name, new Type[] { property.PropertyType });

            if (method == null || !method.IsPublic || method.ReturnType != typeof(void)) return null;
            return method;
        }

        public override void Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
        {
            Helpers.DebugAssert(value != null);
            value = property.GetValue(value, null);
            if (value != null) Tail.Write(dest, ref state, value);
        }

        public override object Read(ProtoReader source, ref ProtoReader.State state, object value)
        {
            Helpers.DebugAssert(value != null);

            object oldVal = Tail.RequiresOldValue ? property.GetValue(value, null) : null;
            object newVal = Tail.Read(source, ref state, oldVal);
            if (readOptionsWriteValue && newVal != null) // if the tail returns a null, intepret that as *no assign*
            {
                if (shadowSetter == null)
                {
                    property.SetValue(value, newVal, null);
                }
                else
                {
                    shadowSetter.Invoke(value, new object[] { newVal });
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
            SanityCheck(property, Tail, out bool writeValue, ctx.NonPublic, ctx.AllowInternal(property));
            if (Helpers.IsValueType(ExpectedType) && valueFrom == null)
            {
                throw new InvalidOperationException("Attempt to mutate struct on the head of the stack; changes would be lost");
            }

            using (Compiler.Local loc = ctx.GetLocalWithValue(ExpectedType, valueFrom))
            {
                if (Tail.RequiresOldValue)
                {
                    ctx.LoadAddress(loc, ExpectedType); // stack is: old-addr
                    ctx.LoadValue(property); // stack is: old-value
                }
                Type propertyType = property.PropertyType;
                ctx.ReadNullCheckedTail(propertyType, Tail, null); // stack is [new-value]

                if (writeValue)
                {
                    using (Compiler.Local newVal = new Compiler.Local(ctx, property.PropertyType))
                    {
                        ctx.StoreValue(newVal); // stack is empty

                        Compiler.CodeLabel allDone = new Compiler.CodeLabel(); // <=== default structs
                        if (!Helpers.IsValueType(propertyType))
                        { // if the tail returns a null, intepret that as *no assign*
                            allDone = ctx.DefineLabel();
                            ctx.LoadValue(newVal); // stack is: new-value
                            ctx.BranchIfFalse(@allDone, true); // stack is empty
                        }
                        // assign the value
                        ctx.LoadAddress(loc, ExpectedType); // parent-addr
                        ctx.LoadValue(newVal); // parent-obj|new-value
                        if (shadowSetter == null)
                        {
                            ctx.StoreValue(property); // empty
                        }
                        else
                        {
                            ctx.EmitCall(shadowSetter); // empty
                        }
                        if (!Helpers.IsValueType(propertyType))
                        {
                            ctx.MarkLabel(allDone);
                        }
                    }
                }
                else
                { // don't want return value; drop it if anything there
                    // stack is [new-value]
                    if (Tail.ReturnsValue) { ctx.DiscardValue(); }
                }
            }
        }
#endif

        internal static bool CanWrite(MemberInfo member)
        {
            if (member == null) throw new ArgumentNullException(nameof(member));

            if (member is PropertyInfo prop)
            {
                return prop.CanWrite || GetShadowSetter(prop) != null;
            }

            return member is FieldInfo; // fields are always writeable; anything else: JUST SAY NO!
        }
    }
}
#endif