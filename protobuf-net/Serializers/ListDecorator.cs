#if !NO_RUNTIME
using System;
using System.Collections;
using System.Reflection;
using ProtoBuf.Meta;
using System.Diagnostics;

namespace ProtoBuf.Serializers
{
    sealed class ListDecorator : ProtoDecoratorBase
    {
        private readonly Type declaredType, concreteType;
        private readonly bool isList;
        private readonly MethodInfo add;
        const BindingFlags StandardFlags = BindingFlags.Public | BindingFlags.Instance;
        internal static Type GetItemType(Type listType)
        {
            Helpers.DebugAssert(listType != null);
            if (listType == typeof(string) || listType.IsArray
                || !typeof(IEnumerable).IsAssignableFrom(listType)) return null;

            BasicList candidates = new BasicList();
            foreach (MethodInfo method in listType.GetMethods(StandardFlags))
            {
                if (method.Name != "Add") continue;
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(object)
                    && !candidates.Contains(parameters[0].ParameterType))
                {
                    candidates.Add(parameters[0].ParameterType);
                }
            }
            foreach(Type iType in listType.GetInterfaces()) {
                if(iType.IsGenericType && iType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.ICollection<>)) {
                    Type[] iTypeArgs = iType.GetGenericArguments();
                    if (!candidates.Contains(iTypeArgs[0]))
                    {
                        candidates.Add(iTypeArgs[0]);
                    }
                }
            }
            return candidates.Count == 1 ? (Type)candidates[0] : null;
        }
        public ListDecorator(Type declaredType, Type concreteType, IProtoSerializer tail) : base(tail)
        {
            if (declaredType == null) throw new ArgumentNullException("declaredType");
            this.declaredType = declaredType;
            isList = typeof(IList).IsAssignableFrom(declaredType);

            Type[] types = { tail.ExpectedType };
            // look for a public list.Add(typedObject) method
            add = declaredType.GetMethod("Add", types);
            if (add == null)
            {   // fallback: look for ICollection<T>'s Add(typedObject) method
                Type listType = typeof(System.Collections.Generic.ICollection<>).MakeGenericType(types);
                if (listType.IsAssignableFrom(declaredType))
                {
                    add = listType.GetMethod("Add", types);
                }
            }

            if(add == null)
            {   // fallback: look for a public list.Add(object) method
                types[0] = typeof(object);
                add = declaredType.GetMethod("Add", types);
            }
            if (add == null && isList)
            {   // fallback: look for IList's Add(object) method
                add = typeof(IList).GetMethod("Add", types);
            }
            if (add == null) throw new InvalidOperationException();
        }

        public override Type ExpectedType { get { return declaredType;  } }
        public override bool RequiresOldValue { get { return true; } }
        public override bool ReturnsValue { get { return true; } }

        protected override void EmitRead(ProtoBuf.Compiler.CompilerContext ctx, ProtoBuf.Compiler.Local valueFrom)
        {
            /* This looks more complex than it is. Look at the non-compiled Read to
             * see what it is trying to do, but note that it needs to cope with a
             * few more scenarios. Note that it picks the **most specific** Add,
             * unlike the runtime version that uses IList when possible. The core
             * is just a "do {list.Add(readValue())} while {thereIsMore}"
             * 
             * The complexity is due to:
             *  - value types vs reference types (boxing etc)
             *  - initialization if we need to pass in a value to the tail
             *  - handling whether or not the tail *returns* the value vs updates the input
             */ 
            using (Compiler.Local list = ctx.GetLocalWithValue(ExpectedType, valueFrom))
            using (Compiler.Local position = new Compiler.Local(ctx, typeof(int)))
            {
                if (concreteType != null)
                {
                    ctx.LoadValue(list);
                    Compiler.CodeLabel notNull = ctx.DefineLabel();
                    ctx.BranchIfTrue(notNull, true);
                    ctx.EmitCtor(concreteType);
                    ctx.StoreValue(list);
                    ctx.MarkLabel(notNull);
                }

                ctx.LoadReaderWriter();
                ctx.LoadValue(typeof(ProtoReader).GetProperty("Position"));
                ctx.StoreValue(position);

                Compiler.CodeLabel @continue = ctx.DefineLabel();
                ctx.MarkLabel(@continue);

                ctx.LoadValue(list);
                Type itemType = Tail.ExpectedType;
                if (Tail.RequiresOldValue)
                {
                    if (itemType.IsValueType || !Tail.ReturnsValue)
                    {
                        // going to need a variable
                        using (Compiler.Local item = new Compiler.Local(ctx, itemType))
                        {
                            if (itemType.IsValueType)
                            {   // initialise the struct
                                ctx.LoadAddress(item, itemType);
                                ctx.EmitCtor(itemType);
                            }
                            else
                            {   // assign null
                                ctx.LoadNullRef();
                                ctx.StoreValue(item);
                            }
                            Tail.EmitRead(ctx, item);
                            ctx.LoadValue(item);
                        }
                    }
                    else
                    {    // no variable; pass the null on the stack and take the value *off* the stack
                        ctx.LoadNullRef();
                        Tail.EmitRead(ctx, null);
                    }
                }
                else
                {
                    if (Tail.ReturnsValue)
                    {   // out only (on the stack); just emit it
                        Tail.EmitRead(ctx, null);
                    }
                    else
                    {   // doesn't take anything in nor return anything! WTF?
                        throw new InvalidOperationException();
                    }
                }
                // our "Add" is chosen either to take the correct type, or to take "object";
                // we may need to box the value
                if (itemType.IsValueType && add.GetParameters()[0].ParameterType == typeof(object))
                {
                    ctx.CastToObject(itemType);
                }
                ctx.EmitCall(add);
                if (add.ReturnType != typeof(void))
                {
                    ctx.DiscardValue();
                }
                ctx.LoadReaderWriter();
                ctx.LoadValue(position);
                ctx.EmitCall(typeof(ProtoReader).GetMethod("TryReadFieldHeader"));
                ctx.BranchIfTrue(@continue, false);
                ctx.LoadValue(list);
            }
        }

        MethodInfo GetEnumeratorInfo(out MethodInfo moveNext, out MethodInfo current)
        {
            MethodInfo getEnumerator = Helpers.GetInstanceMethod(ExpectedType,"GetEnumerator",null);
            Type iteratorType, itemType = Tail.ExpectedType;
            
            if (getEnumerator != null)
            {
                iteratorType = getEnumerator.ReturnType;
                moveNext = Helpers.GetInstanceMethod(iteratorType, "MoveNext", null);
                PropertyInfo prop = iteratorType.GetProperty("Current", StandardFlags);
                current = prop == null ? null : prop.GetGetMethod();
                // fully typed
                if (moveNext.ReturnType == typeof(bool) && prop.PropertyType == itemType)
                {
                    return getEnumerator;
                }
                moveNext = current = getEnumerator = null;
            }

            Type enumeratorType = typeof(System.Collections.Generic.IEnumerable<>).MakeGenericType(itemType);
            if (enumeratorType.IsAssignableFrom(ExpectedType))
            {
                getEnumerator = enumeratorType.GetMethod("GetEnumerator");
                iteratorType = getEnumerator.ReturnType;
                moveNext = typeof(IEnumerator).GetMethod("MoveNext");
                current = iteratorType.GetProperty("Current").GetGetMethod();
                return getEnumerator;
            }

            enumeratorType = typeof(IEnumerable);
            getEnumerator = enumeratorType.GetMethod("GetEnumerator");
            iteratorType = getEnumerator.ReturnType;
            moveNext = iteratorType.GetMethod("MoveNext");
            current = iteratorType.GetProperty("Current").GetGetMethod();
            return getEnumerator;
        }
        protected override void EmitWrite(ProtoBuf.Compiler.CompilerContext ctx, ProtoBuf.Compiler.Local valueFrom)
        {
            /* Note: I considered (and discarded) the scenario of customer iterators (GetEnumerator()) that
             * are structs or sealed classes and which don't implement IDisposable (and thus could *never*
             * be disposable) - based on a search starting with a winform and cascading all references, there
             * are only 19 such in the CLR, and none interesting - so not worth special-casing the scenario.
             * This means we *always* need the try/catch.
             * 
             */

            using (Compiler.Local list = ctx.GetLocalWithValue(ExpectedType, valueFrom))
            {
                MethodInfo moveNext, current, getEnumerator = GetEnumeratorInfo(out moveNext, out current);
                Helpers.DebugAssert(moveNext != null);
                Helpers.DebugAssert(current != null);
                Helpers.DebugAssert(getEnumerator != null);
                Type enumeratorType = getEnumerator.ReturnType;
                using (Compiler.Local iter = new Compiler.Local(ctx, enumeratorType))
                {
                    ctx.LoadAddress(list, ExpectedType);
                    ctx.EmitCall(getEnumerator);
                    ctx.StoreValue(iter);

                    Compiler.CodeLabel @try = ctx.BeginTry();
                    Compiler.CodeLabel body = ctx.DefineLabel(), @next = ctx.DefineLabel();
                    ctx.Branch(@next, false);
                    ctx.MarkLabel(body);

                    ctx.LoadAddress(iter, enumeratorType);
                    ctx.EmitCall(current);
                    Tail.EmitWrite(ctx, null);

                    ctx.MarkLabel(@next);
                    ctx.LoadAddress(iter, enumeratorType);
                    ctx.EmitCall(moveNext);
                    ctx.BranchIfTrue(body, false);

                    ctx.EndTry(@try, false);
                    ctx.BeginFinally();
                    MethodInfo dispose = typeof(IDisposable).GetMethod("Dispose");
                    bool alwaysDisposable = typeof(IDisposable).IsAssignableFrom(enumeratorType);
                    if(enumeratorType.IsValueType) {
                        if(alwaysDisposable) {
                            // TODO: need to check re boxing
                            ctx.LoadAddress(iter, enumeratorType);
                            ctx.Constrain(enumeratorType);
                            ctx.EmitCall(dispose);
                        }
                        // but don't need to worry about "maybe", so no "else"
                    } else {
                        Compiler.CodeLabel @null = ctx.DefineLabel();
                        if(alwaysDisposable) {
                            // just needs a null-check                            
                            ctx.LoadValue(iter);
                            ctx.BranchIfFalse(@null, true);
                            ctx.LoadAddress(iter, enumeratorType);
                            ctx.EmitCall(dispose);
                        } else {
                            // test via "as"
                            using (Compiler.Local disp = new Compiler.Local(ctx, typeof(IDisposable)))
                            {
                                ctx.LoadValue(iter);
                                ctx.TryCast(typeof(IDisposable));
                                ctx.StoreValue(disp);
                                ctx.LoadValue(disp);
                                ctx.BranchIfFalse(@null, true);
                                ctx.LoadAddress(iter, enumeratorType);
                                ctx.EmitCall(dispose);   
                            }
                        }
                        ctx.MarkLabel(@null);
                    }
                    ctx.EndFinally();
                }
            }
        }
        public override void Write(object value, ProtoWriter dest)
        {
            foreach (object subItem in (IEnumerable)value)
            {
                if (subItem == null) { throw new NullReferenceException(); }
                Tail.Write(subItem, dest);
            }
        }
        public override object Read(object value, ProtoReader source)
        {
            int field = source.FieldNumber;
            if (value == null) value = Activator.CreateInstance(concreteType);
            if (isList)
            {
                IList list = (IList)value;
                do
                {
                    list.Add(Tail.Read(null, source));
                } while (source.TryReadFieldHeader(field));
            }
            else
            {
                object[] args = new object[1];
                do
                {
                    args[0] = Tail.Read(null, source);
                    add.Invoke(value, args);
                } while (source.TryReadFieldHeader(field));
            }
            
            return value;
        }

    }
}
#endif