#if !NO_RUNTIME
using System;
using System.Collections;
using System.Reflection;
using ProtoBuf.Compiler;
using ProtoBuf.Meta;
using System.Diagnostics;

namespace ProtoBuf.Serializers
{
    sealed class ListDecorator : ProtoDecoratorBase
    {
        private readonly Type declaredType, concreteType;
        private readonly bool isList;
        private readonly MethodInfo add;

        private readonly int packedFieldNumber;
        private readonly WireType packedWireType;
        public ListDecorator(Type declaredType, Type concreteType, IProtoSerializer tail, int packedFieldNumber, WireType packedWireType) : base(tail)
        {
            this.packedWireType = WireType.None;
            if (packedFieldNumber != 0)
            {
                if (packedFieldNumber < 0) throw new ArgumentOutOfRangeException("packedFieldNumber");
                switch(packedWireType)
                {
                    case WireType.Fixed32:
                    case WireType.Fixed64:
                    case WireType.SignedVariant:
                    case WireType.Variant:
                        break;
                    default:
                        throw new ArgumentException("Packed buffers are not supported for wire-type: " + packedWireType, "packedFieldNumber");
                }
                this.packedFieldNumber = packedFieldNumber;
                this.packedWireType = packedWireType;
            }
            if (declaredType == null) throw new ArgumentNullException("declaredType");
            if (declaredType.IsArray) throw new ArgumentException("Cannot treat arrays as lists", "declaredType");
            this.declaredType = declaredType;
            this.concreteType = concreteType;
            
            // look for a public list.Add(typedObject) method
            add = TypeModel.ResolveListAdd(declaredType, tail.ExpectedType, out isList);
            if (add == null) throw new InvalidOperationException();
        }

        public override Type ExpectedType { get { return declaredType;  } }
        public override bool RequiresOldValue { get { return true; } }
        public override bool ReturnsValue { get { return true; } }
#if FEAT_COMPILER
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

                EmitReadList(ctx, list, Tail, add, packedWireType);
                ctx.LoadValue(list);
            }
        }

        internal static void EmitReadList(ProtoBuf.Compiler.CompilerContext ctx, Compiler.Local list, IProtoSerializer tail, MethodInfo add, WireType packedWireType)
        {
            using (Compiler.Local fieldNumber = new Compiler.Local(ctx, typeof(int)))
            {
                Compiler.CodeLabel readPacked = packedWireType == WireType.None ? new CodeLabel() : ctx.DefineLabel();                                   
                if (packedWireType != WireType.None)
                {
                    ctx.LoadReaderWriter();
                    ctx.LoadValue(typeof(ProtoReader).GetProperty("WireType"));
                    ctx.LoadValue((int)WireType.String);
                    ctx.BranchIfEqual(readPacked, false);
                }
                ctx.LoadReaderWriter();
                ctx.LoadValue(typeof(ProtoReader).GetProperty("FieldNumber"));
                ctx.StoreValue(fieldNumber);

                Compiler.CodeLabel @continue = ctx.DefineLabel();
                ctx.MarkLabel(@continue);

                EmitReadAndAddItem(ctx, list, tail, add);

                ctx.LoadReaderWriter();
                ctx.LoadValue(fieldNumber);
                ctx.EmitCall(typeof(ProtoReader).GetMethod("TryReadFieldHeader"));
                ctx.BranchIfTrue(@continue, false);

                if (packedWireType != WireType.None)
                {
                    Compiler.CodeLabel allDone = ctx.DefineLabel();
                    ctx.Branch(allDone, false);
                    ctx.MarkLabel(readPacked);

                    ctx.LoadReaderWriter();
                    ctx.EmitCall(typeof(ProtoReader).GetMethod("StartSubItem"));

                    Compiler.CodeLabel testForData = ctx.DefineLabel(), noMoreData = ctx.DefineLabel();
                    ctx.MarkLabel(testForData);
                    ctx.LoadValue((int)packedWireType);
                    ctx.LoadReaderWriter();
                    ctx.EmitCall(typeof(ProtoReader).GetMethod("HasSubValue"));
                    ctx.BranchIfFalse(noMoreData, false);

                    EmitReadAndAddItem(ctx, list, tail, add);
                    ctx.Branch(testForData, false);

                    ctx.MarkLabel(noMoreData);
                    ctx.LoadReaderWriter();
                    ctx.EmitCall(typeof(ProtoReader).GetMethod("EndSubItem"));
                    ctx.MarkLabel(allDone);
                }


                
            }
        }

        private static void EmitReadAndAddItem(CompilerContext ctx, Local list, IProtoSerializer tail, MethodInfo add)
        {
            ctx.LoadValue(list);
            Type itemType = tail.ExpectedType;
            if (tail.RequiresOldValue)
            {
                if (itemType.IsValueType || !tail.ReturnsValue)
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
                        tail.EmitRead(ctx, item);
                        if (!tail.ReturnsValue) { ctx.LoadValue(item); }
                    }
                }
                else
                {    // no variable; pass the null on the stack and take the value *off* the stack
                    ctx.LoadNullRef();
                    tail.EmitRead(ctx, null);
                }
            }
            else
            {
                if (tail.ReturnsValue)
                {   // out only (on the stack); just emit it
                    tail.EmitRead(ctx, null);
                }
                else
                {   // doesn't take anything in nor return anything! WTF?
                    throw new InvalidOperationException();
                }
            }
            // our "Add" is chosen either to take the correct type, or to take "object";
            // we may need to box the value
                
            Type addParamType = add.GetParameters()[0].ParameterType;
            if(addParamType != itemType) {
                if (addParamType == typeof(object))
                {
                    ctx.CastToObject(itemType);
                }
                else
                {
                    throw new InvalidOperationException("Conflicting item/add type");
                }
            }
            ctx.EmitCall(add);
            if (add.ReturnType != typeof(void))
            {
                ctx.DiscardValue();
            }
        }
#endif
        MethodInfo GetEnumeratorInfo(out MethodInfo moveNext, out MethodInfo current)
        {
            MethodInfo getEnumerator = Helpers.GetInstanceMethod(ExpectedType,"GetEnumerator",null);
            Type iteratorType, itemType = Tail.ExpectedType;
            
            if (getEnumerator != null)
            {
                iteratorType = getEnumerator.ReturnType;
                moveNext = Helpers.GetInstanceMethod(iteratorType, "MoveNext", null);
                PropertyInfo prop = iteratorType.GetProperty("Current", BindingFlags.Public | BindingFlags.Instance);
                current = prop == null ? null : prop.GetGetMethod(false);
                if (moveNext == null && typeof(IEnumerator).IsAssignableFrom(iteratorType))
                {
                    moveNext = Helpers.GetInstanceMethod(typeof(IEnumerator), "MoveNext", null);
                }
                // fully typed
                if (moveNext != null && moveNext.ReturnType == typeof(bool)
                    && current != null && current.ReturnType == itemType)
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
                current = iteratorType.GetProperty("Current").GetGetMethod(false);
                return getEnumerator;
            }

            enumeratorType = typeof(IEnumerable);
            getEnumerator = enumeratorType.GetMethod("GetEnumerator");
            iteratorType = getEnumerator.ReturnType;
            moveNext = iteratorType.GetMethod("MoveNext");
            current = iteratorType.GetProperty("Current").GetGetMethod(false);
            return getEnumerator;
        }
#if FEAT_COMPILER
        protected override void EmitWrite(ProtoBuf.Compiler.CompilerContext ctx, ProtoBuf.Compiler.Local valueFrom)
        {
            using (Compiler.Local list = ctx.GetLocalWithValue(ExpectedType, valueFrom))
            {
                MethodInfo moveNext, current, getEnumerator = GetEnumeratorInfo(out moveNext, out current);
                Helpers.DebugAssert(moveNext != null);
                Helpers.DebugAssert(current != null);
                Helpers.DebugAssert(getEnumerator != null);
                Type enumeratorType = getEnumerator.ReturnType;
                using (Compiler.Local iter = new Compiler.Local(ctx, enumeratorType))
                using (Compiler.Local token = packedFieldNumber > 0 ? new Compiler.Local(ctx, typeof(SubItemToken)) : null)
                {
                    ctx.LoadAddress(list, ExpectedType);
                    ctx.EmitCall(getEnumerator);
                    ctx.StoreValue(iter);
                    using (ctx.Using(iter))
                    {
                        Compiler.CodeLabel body = ctx.DefineLabel(),
                                           @next = ctx.DefineLabel(),
                                           nothingToWrite = packedFieldNumber > 0 ? ctx.DefineLabel() : new Compiler.CodeLabel();
                        if (packedFieldNumber > 0)
                        {
                            ctx.LoadAddress(iter, enumeratorType);
                            ctx.EmitCall(moveNext);
                            ctx.BranchIfFalse(nothingToWrite, false);

                            ctx.LoadValue(packedFieldNumber);
                            ctx.LoadValue((int)WireType.String);
                            ctx.LoadReaderWriter();
                            ctx.EmitCall(typeof(ProtoWriter).GetMethod("WriteFieldHeader"));

                            ctx.LoadValue(list);
                            ctx.LoadReaderWriter();
                            ctx.EmitCall(typeof(ProtoWriter).GetMethod("StartSubItem"));
                            ctx.StoreValue(token);

                            ctx.LoadValue(packedFieldNumber);
                            ctx.LoadReaderWriter();
                            ctx.EmitCall(typeof(ProtoWriter).GetMethod("SetPackedField"));
                        }
                        else { 
                            ctx.Branch(@next, false);
                        }
                        ctx.MarkLabel(body);

                        ctx.LoadAddress(iter, enumeratorType);
                        ctx.EmitCall(current);
                        Type itemType = Tail.ExpectedType;
                        if (itemType != typeof(object) && current.ReturnType == typeof(object))
                        {
                            ctx.CastFromObject(itemType);
                        }
                        Tail.EmitWrite(ctx, null);

                        ctx.MarkLabel(@next);
                        ctx.LoadAddress(iter, enumeratorType);
                        ctx.EmitCall(moveNext);
                        ctx.BranchIfTrue(body, false);
                        if(packedFieldNumber > 0)
                        {
                            ctx.LoadValue(token);
                            ctx.LoadReaderWriter();
                            ctx.EmitCall(typeof(ProtoWriter).GetMethod("EndSubItem"));
                            ctx.MarkLabel(nothingToWrite);
                        }
                    }
                    
                }
            }
        }
#endif
        public override void Write(object value, ProtoWriter dest)
        {
            if (packedFieldNumber > 0)
            {
                IEnumerator iter = ((IEnumerable) value).GetEnumerator();
                using(iter as IDisposable)
                {
                    if (iter.MoveNext())
                    {
                        ProtoWriter.WriteFieldHeader(packedFieldNumber, WireType.String, dest);
                        SubItemToken token = ProtoWriter.StartSubItem(value, dest);
                        ProtoWriter.SetPackedField(packedFieldNumber, dest);
                        do
                        {
                            object subItem = iter.Current;
                            if (subItem == null)
                            {
                                throw new NullReferenceException();
                            }
                            Tail.Write(subItem, dest);
                        } while (iter.MoveNext());
                        ProtoWriter.EndSubItem(token, dest);
                    }
                }
            }
            else
            {
                foreach (object subItem in (IEnumerable)value)
                {
                    if (subItem == null) { throw new NullReferenceException(); }
                    Tail.Write(subItem, dest);
                } 
            }
        }
        public override object Read(object value, ProtoReader source)
        {
            int field = source.FieldNumber;
            if (value == null) value = Activator.CreateInstance(concreteType);

            if (packedWireType != WireType.None && source.WireType == WireType.String)
            {
                SubItemToken token = ProtoReader.StartSubItem(source);
                if (isList)
                {
                    IList list = (IList)value;
                    while (ProtoReader.HasSubValue(packedWireType, source))
                    {
                        list.Add(Tail.Read(null, source));
                    }
                }
                else {
                    object[] args = new object[1];
                    while (ProtoReader.HasSubValue(packedWireType, source))
                    {
                        args[0] = Tail.Read(null, source);
                        add.Invoke(value, args);
                    }
                }
                ProtoReader.EndSubItem(token, source);
            }
            else { 
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
            }
            return value;
        }

    }
}
#endif