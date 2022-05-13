using ProtoBuf.Internal;
using System;
using System.Diagnostics;
using System.Reflection;

namespace ProtoBuf.Internal.Serializers
{
    internal sealed class PropertyDecorator : ProtoDecoratorBase
    {
        public override Type ExpectedType { get; }
        private readonly PropertyInfo property;
        public override bool RequiresOldValue => true;
        public override bool ReturnsValue => false;
        private readonly bool readOptionsWriteValue;
        private readonly MethodInfo shadowSetter;

        public PropertyDecorator(Type forType, PropertyInfo property, IRuntimeProtoSerializerNode tail) : base(tail)
        {
            if (tail is null) ThrowHelper.ThrowArgumentNullException(nameof(tail));
            if (property is null) ThrowHelper.ThrowArgumentNullException(nameof(property));
            if (forType is null) ThrowHelper.ThrowArgumentNullException(nameof(forType));
            ExpectedType = forType;
            this.property = property;
            SanityCheck(property, tail, out readOptionsWriteValue, true, true);
            shadowSetter = GetShadowSetter(property);
        }

        private static void SanityCheck(PropertyInfo property, IRuntimeProtoSerializerNode tail, out bool writeValue, bool nonPublic, bool allowInternal)
        {
            if (property is null) throw new ArgumentNullException(nameof(property));

            writeValue = tail.ReturnsValue && (GetShadowSetter(property) is not null || (property.CanWrite && Helpers.GetSetMethod(property, nonPublic, allowInternal) is not null));
            if (!property.CanRead || Helpers.GetGetMethod(property, nonPublic, allowInternal) is null)
            {
                throw new InvalidOperationException($"Cannot serialize property without an accessible get accessor: {property.DeclaringType.FullName}.{property.Name}");
            }
            if (!writeValue && (!tail.RequiresOldValue || tail.ExpectedType.IsValueType))
            { // so we can't save the value, and the tail doesn't use it either... not helpful
                // or: can't write the value, so the struct value will be lost
                throw new InvalidOperationException($"Cannot apply changes to property {property.DeclaringType.FullName}.{property.Name}");
            }
        }
        private static MethodInfo GetShadowSetter(PropertyInfo property)
        {
            Type reflectedType = property.ReflectedType;
            MethodInfo method = Helpers.GetInstanceMethod(reflectedType, "Set" + property.Name, new Type[] { property.PropertyType });

            if (method is null || !method.IsPublic || method.ReturnType != typeof(void)) return null;
            return method;
        }

        public override void Write(ref ProtoWriter.State state, object value)
        {
            Debug.Assert(value is not null);
            value = property.GetValue(value, null);
            if (value is not null) Tail.Write(ref state, value);
        }

        public override object Read(ref ProtoReader.State state, object value)
        {
            Debug.Assert(value is not null);

            object oldVal = Tail.RequiresOldValue ? property.GetValue(value, null) : null;
            object newVal = Tail.Read(ref state, oldVal);
            if (readOptionsWriteValue && newVal is not null) // if the tail returns a null, intepret that as *no assign*
            {
                if (shadowSetter is null)
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

        protected override void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadAddress(valueFrom, ExpectedType);
            ctx.LoadValue(property);
            ctx.WriteNullCheckedTail(property.PropertyType, Tail, null);
        }
        
        internal static Type ChooseReadLocalType(Type memberType, Type tailType)
        {
            if (memberType == tailType) return memberType;
            if (memberType.IsClass && tailType.IsClass) return tailType;

            if (memberType.IsValueType && tailType.IsValueType
                && tailType == Nullable.GetUnderlyingType(memberType))
            {
                // it will have been wrapped on the way out by ReadNullCheckedTail
                return memberType;
            }

            return tailType;
        }
        protected override void EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            SanityCheck(property, Tail, out bool writeValue, ctx.NonPublic, ctx.AllowInternal(property));
            if (ExpectedType.IsValueType && valueFrom is null)
            {
                throw new InvalidOperationException("Attempt to mutate struct on the head of the stack; changes would be lost");
            }

            using Compiler.Local loc = ctx.GetLocalWithValue(ExpectedType, valueFrom);
            if (Tail.RequiresOldValue)
            {
                ctx.LoadAddress(loc, ExpectedType); // stack is: old-addr
                ctx.LoadValue(property); // stack is: old-value
            }
            Type propertyType = property.PropertyType;
            ctx.ReadNullCheckedTail(propertyType, Tail, null); // stack is [new-value]

            if (writeValue)
            {
                var localType = ChooseReadLocalType(property.PropertyType, Tail.ExpectedType);
                using Compiler.Local newVal = new Compiler.Local(ctx, localType);
                ctx.StoreValue(newVal); // stack is empty

                Compiler.CodeLabel allDone = new Compiler.CodeLabel(); // <=== default structs

                if (!localType.IsValueType)
                { // if the tail returns a null, intepret that as *no assign*
                    allDone = ctx.DefineLabel();
                    ctx.LoadValue(newVal); // stack is: new-value
                    ctx.BranchIfFalse(@allDone, true); // stack is empty
                }

                // assign the value
                ctx.LoadAddress(loc, ExpectedType); // parent-addr
                ctx.LoadValue(newVal); // parent-obj|new-value

                // cast if needed (this is mostly for ReadMap/ReadRepeated)
                if (!property.PropertyType.IsValueType && !localType.IsValueType
                    && !property.PropertyType.IsAssignableFrom(localType))
                {
                    ctx.Cast(property.PropertyType);
                }

                if (shadowSetter is null)
                {
                    ctx.StoreValue(property); // empty
                }
                else
                {
                    ctx.EmitCall(shadowSetter); // empty
                }
                if (!propertyType.IsValueType)
                {
                    ctx.MarkLabel(allDone);
                }
            }
            else
            { // don't want return value; drop it if anything there
              // stack is [new-value]
                if (Tail.ReturnsValue) { ctx.DiscardValue(); }
            }
        }
    }
}