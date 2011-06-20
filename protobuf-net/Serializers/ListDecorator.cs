#if !NO_RUNTIME
using System;
using System.Collections;
using System.Reflection;
using ProtoBuf.Meta;

namespace ProtoBuf.Serializers
{
    sealed class ListDecorator : ProtoDecoratorBase
    {
        internal static bool CanPack(WireType wireType)
        {
            switch (wireType)
            {
                case WireType.Fixed32:
                case WireType.Fixed64:
                case WireType.SignedVariant:
                case WireType.Variant:
                    return true;
                default:
                    return false;
            }
        }
        private readonly byte options;
        private const byte OPTIONS_IsList = 1,
                           OPTIONS_SuppressIList = 2,
                           OPTIONS_WritePacked = 4,
                           OPTIONS_ReturnList = 8,
                           OPTIONS_OverwriteList = 16;

        private readonly Type declaredType, concreteType;

        private readonly MethodInfo add;

        private readonly int fieldNumber;

        private bool IsList { get { return (options & OPTIONS_IsList) != 0; } }
        private bool SuppressIList { get { return (options & OPTIONS_SuppressIList) != 0; } }
        private bool WritePacked { get { return (options & OPTIONS_WritePacked) != 0; } }
        private bool ReturnList { get { return (options & OPTIONS_ReturnList) != 0; } }
        private readonly WireType packedWireType;

        public ListDecorator(Type declaredType, Type concreteType, IProtoSerializer tail, int fieldNumber, bool writePacked, WireType packedWireType, bool returnList, bool overwriteList) : base(tail)
        {
            if (returnList) options |= OPTIONS_ReturnList;
            if (overwriteList) options |= OPTIONS_OverwriteList;
            if ((writePacked || packedWireType != WireType.None) && fieldNumber <= 0) throw new ArgumentOutOfRangeException("fieldNumber");
            if (!CanPack(packedWireType))
            {
                if (writePacked) throw new InvalidOperationException("Only simple data-types can use packed encoding");
                packedWireType = WireType.None;
            }            

            this.fieldNumber = fieldNumber;
            if (writePacked) options |= OPTIONS_WritePacked;
            this.packedWireType = packedWireType;
            if (declaredType == null) throw new ArgumentNullException("declaredType");
            if (declaredType.IsArray) throw new ArgumentException("Cannot treat arrays as lists", "declaredType");
            this.declaredType = declaredType;
            this.concreteType = concreteType;
            
            // look for a public list.Add(typedObject) method
            bool isList;
            add = TypeModel.ResolveListAdd(declaredType, tail.ExpectedType, out isList);
            if (isList)
            {
                options |= OPTIONS_IsList;
                if (declaredType.FullName.StartsWith("System.Data.Linq.EntitySet`1[["))
                { // see http://stackoverflow.com/questions/6194639/entityset-is-there-a-sane-reason-that-ilist-add-doesnt-set-assigned
                    options |= OPTIONS_SuppressIList;
                }
            }
            if (add == null) throw new InvalidOperationException();
        }

        public override Type ExpectedType { get { return declaredType;  } }
        public override bool RequiresOldValue { get { return AppendToCollection; } }
        public override bool ReturnsValue { get { return ReturnList; } }

        private bool AppendToCollection
        {
            get { return (options & OPTIONS_OverwriteList) == 0; }
        }

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
            bool returnList = ReturnList;
            using (Compiler.Local list = AppendToCollection ? ctx.GetLocalWithValue(ExpectedType, valueFrom) : new Compiler.Local(ctx, declaredType))
            using (Compiler.Local origlist = (returnList && AppendToCollection) ? new Compiler.Local(ctx, ExpectedType) : null)
            {
                if (!AppendToCollection)
                { // always new
                    ctx.LoadNullRef();
                    ctx.StoreValue(list);
                }
                else if (returnList)
                { // need a copy
                    ctx.LoadValue(list);
                    ctx.StoreValue(origlist);
                }
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

                if (returnList)
                {
                    if (AppendToCollection)
                    {
                        // remember ^^^^ we had a spare copy of the list on the stack; now we'll compare
                        ctx.LoadValue(origlist);
                        ctx.LoadValue(list); // [orig] [new-value]
                        Compiler.CodeLabel sameList = ctx.DefineLabel(), allDone = ctx.DefineLabel();
                        ctx.BranchIfEqual(sameList, true);
                        ctx.LoadValue(list);
                        ctx.Branch(allDone, true);
                        ctx.MarkLabel(sameList);
                        ctx.LoadNullRef();
                        ctx.MarkLabel(allDone);
                    }
                    else
                    {
                        ctx.LoadValue(list);
                    }
                }
            }
        }

        internal static void EmitReadList(ProtoBuf.Compiler.CompilerContext ctx, Compiler.Local list, IProtoSerializer tail, MethodInfo add, WireType packedWireType)
        {
            using (Compiler.Local fieldNumber = new Compiler.Local(ctx, typeof(int)))
            {
                Compiler.CodeLabel readPacked = packedWireType == WireType.None ? new Compiler.CodeLabel() : ctx.DefineLabel();                                   
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

        private static void EmitReadAndAddItem(Compiler.CompilerContext ctx, Compiler.Local list, IProtoSerializer tail, MethodInfo add)
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
            Type enumeratorType;
#if !NO_GENERICS
            enumeratorType = typeof(System.Collections.Generic.IEnumerable<>).MakeGenericType(itemType);
            if (enumeratorType.IsAssignableFrom(ExpectedType))
            {
                getEnumerator = enumeratorType.GetMethod("GetEnumerator");
                iteratorType = getEnumerator.ReturnType;
                moveNext = typeof(IEnumerator).GetMethod("MoveNext");
                current = iteratorType.GetProperty("Current").GetGetMethod(false);
                return getEnumerator;
            }
#endif
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
                bool writePacked = WritePacked;
                using (Compiler.Local iter = new Compiler.Local(ctx, enumeratorType))
                using (Compiler.Local token = writePacked ? new Compiler.Local(ctx, typeof(SubItemToken)) : null)
                {
                    if (writePacked)
                    {
                        ctx.LoadValue(fieldNumber);
                        ctx.LoadValue((int)WireType.String);
                        ctx.LoadReaderWriter();
                        ctx.EmitCall(typeof(ProtoWriter).GetMethod("WriteFieldHeader"));

                        ctx.LoadValue(list);
                        ctx.LoadReaderWriter();
                        ctx.EmitCall(typeof(ProtoWriter).GetMethod("StartSubItem"));
                        ctx.StoreValue(token);

                        ctx.LoadValue(fieldNumber);
                        ctx.LoadReaderWriter();
                        ctx.EmitCall(typeof(ProtoWriter).GetMethod("SetPackedField"));
                    }

                    ctx.LoadAddress(list, ExpectedType);
                    ctx.EmitCall(getEnumerator);
                    ctx.StoreValue(iter);
                    using (ctx.Using(iter))
                    {
                        Compiler.CodeLabel body = ctx.DefineLabel(),
                                           @next = ctx.DefineLabel();
                        
                        
                        ctx.Branch(@next, false);
                        
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
                    }

                    if (writePacked)
                    {
                        ctx.LoadValue(token);
                        ctx.LoadReaderWriter();
                        ctx.EmitCall(typeof(ProtoWriter).GetMethod("EndSubItem"));
                    }                    
                }
            }
        }
#endif
        public override void Write(object value, ProtoWriter dest)
        {
            SubItemToken token;
            bool writePacked = WritePacked;
            if (writePacked)
            {
                ProtoWriter.WriteFieldHeader(fieldNumber, WireType.String, dest);
                token = ProtoWriter.StartSubItem(value, dest);
                ProtoWriter.SetPackedField(fieldNumber, dest);
            }
            else
            {
                token = new SubItemToken(); // default
            }
            foreach (object subItem in (IEnumerable)value)
            {
                if (subItem == null) { throw new NullReferenceException(); }
                Tail.Write(subItem, dest);
            }
            if (writePacked)
            {
                ProtoWriter.EndSubItem(token, dest);
            }
        }
        public override object Read(object value, ProtoReader source)
        {
            int field = source.FieldNumber;
            object origValue = value;
            if (value == null) value = Activator.CreateInstance(concreteType);
            bool isList = IsList && !SuppressIList;
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
            return origValue == value ? null : value;
        }

    }
}
#endif