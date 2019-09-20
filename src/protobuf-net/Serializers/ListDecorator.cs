using System;
using System.Collections;
using ProtoBuf.Meta;
using System.Reflection;
using System.Diagnostics;

namespace ProtoBuf.Serializers
{
    internal class ListDecorator : ProtoDecoratorBase
    {
        internal static bool CanPack(WireType wireType)
        {
            switch (wireType)
            {
                case WireType.Fixed32:
                case WireType.Fixed64:
                case WireType.SignedVarint:
                case WireType.Varint:
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
                           OPTIONS_OverwriteList = 16,
                           OPTIONS_SupportNull = 32;

        private readonly Type declaredType, concreteType;

        private readonly MethodInfo add;

        private readonly int fieldNumber;

        private bool IsList { get { return (options & OPTIONS_IsList) != 0; } }
        private bool SuppressIList { get { return (options & OPTIONS_SuppressIList) != 0; } }
        private bool WritePacked { get { return (options & OPTIONS_WritePacked) != 0; } }
        private bool SupportNull { get { return (options & OPTIONS_SupportNull) != 0; } }
        private bool ReturnList { get { return (options & OPTIONS_ReturnList) != 0; } }
        protected readonly WireType packedWireType;

        internal static ListDecorator Create(Type declaredType, Type concreteType, IRuntimeProtoSerializerNode tail, int fieldNumber, bool writePacked, WireType packedWireType, bool returnList, bool overwriteList, bool supportNull)
        {
            if (returnList && ImmutableCollectionDecorator.IdentifyImmutable(declaredType,
                out MethodInfo builderFactory,
                out PropertyInfo isEmpty,
                out PropertyInfo length,
                out MethodInfo add,
                out MethodInfo addRange,
                out MethodInfo finish))
            {
                return new ImmutableCollectionDecorator(
                    declaredType, concreteType, tail, fieldNumber, writePacked, packedWireType, returnList, overwriteList, supportNull,
                    builderFactory, isEmpty, length, add, addRange, finish);
            }

            return new ListDecorator(declaredType, concreteType, tail, fieldNumber, writePacked, packedWireType, returnList, overwriteList, supportNull);
        }

        protected ListDecorator(Type declaredType, Type concreteType, IRuntimeProtoSerializerNode tail, int fieldNumber, bool writePacked, WireType packedWireType, bool returnList, bool overwriteList, bool supportNull)
            : base(tail)
        {
            if (returnList) options |= OPTIONS_ReturnList;
            if (overwriteList) options |= OPTIONS_OverwriteList;
            if (supportNull) options |= OPTIONS_SupportNull;
            if ((writePacked || packedWireType != WireType.None) && fieldNumber <= 0) throw new ArgumentOutOfRangeException(nameof(fieldNumber));
            if (!CanPack(packedWireType))
            {
                if (writePacked) throw new InvalidOperationException("Only simple data-types can use packed encoding");
                packedWireType = WireType.None;
            }

            this.fieldNumber = fieldNumber;
            if (writePacked) options |= OPTIONS_WritePacked;
            this.packedWireType = packedWireType;
            if (declaredType == null) throw new ArgumentNullException(nameof(declaredType));
            if (declaredType.IsArray) throw new ArgumentException("Cannot treat arrays as lists", nameof(declaredType));
            this.declaredType = declaredType;
            this.concreteType = concreteType;

            // look for a public list.Add(typedObject) method
            if (RequireAdd)
            {
                add = TypeModel.ResolveListAdd(declaredType, tail.ExpectedType, out var isList);
                if (isList)
                {
                    options |= OPTIONS_IsList;
                    string fullName = declaredType.FullName;
                    if (fullName != null && fullName.StartsWith("System.Data.Linq.EntitySet`1[["))
                    { // see http://stackoverflow.com/questions/6194639/entityset-is-there-a-sane-reason-that-ilist-add-doesnt-set-assigned
                        options |= OPTIONS_SuppressIList;
                    }
                }
                if (add == null) throw new InvalidOperationException("Unable to resolve a suitable Add method for " + declaredType.FullName);
            }
        }
        protected virtual bool RequireAdd => true;

        public override Type ExpectedType => declaredType;

        public override bool RequiresOldValue => AppendToCollection;

        public override bool ReturnsValue => ReturnList;

        protected bool AppendToCollection
        {
            get { return (options & OPTIONS_OverwriteList) == 0; }
        }

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

            using Compiler.Local list = AppendToCollection ? ctx.GetLocalWithValue(ExpectedType, valueFrom) : new Compiler.Local(ctx, declaredType);
            using Compiler.Local origlist = (returnList && AppendToCollection && !ExpectedType.IsValueType) ? new Compiler.Local(ctx, ExpectedType) : null;
            if (!AppendToCollection)
            { // always new
                ctx.LoadNullRef();
                ctx.StoreValue(list);
            }
            else if (returnList && origlist != null)
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

            bool castListForAdd = !add.DeclaringType.IsAssignableFrom(declaredType);
            EmitReadList(ctx, list, Tail, add, packedWireType, castListForAdd);

            if (returnList)
            {
                if (AppendToCollection && origlist != null)
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

        internal static void EmitReadList(ProtoBuf.Compiler.CompilerContext ctx, Compiler.Local list, IRuntimeProtoSerializerNode tail, MethodInfo add, WireType packedWireType, bool castListForAdd)
        {
            using Compiler.Local fieldNumber = new Compiler.Local(ctx, typeof(int));
            Compiler.CodeLabel readPacked = packedWireType == WireType.None ? new Compiler.CodeLabel() : ctx.DefineLabel();
            if (packedWireType != WireType.None)
            {
                ctx.LoadState();
                ctx.LoadValue(typeof(ProtoReader.State).GetProperty(nameof(ProtoReader.State.WireType)));
                ctx.LoadValue((int)WireType.String);
                ctx.BranchIfEqual(readPacked, false);
            }
            ctx.LoadState();
            ctx.LoadValue(typeof(ProtoReader.State).GetProperty(nameof(ProtoReader.State.FieldNumber)));
            ctx.StoreValue(fieldNumber);

            Compiler.CodeLabel @continue = ctx.DefineLabel();
            ctx.MarkLabel(@continue);

            EmitReadAndAddItem(ctx, list, tail, add, castListForAdd);

            ctx.LoadState();
            ctx.LoadValue(fieldNumber);
            ctx.EmitCall(typeof(ProtoReader.State).GetMethod(nameof(ProtoReader.State.TryReadFieldHeader),
                new[] { typeof(int) }));
            ctx.BranchIfTrue(@continue, false);

            if (packedWireType != WireType.None)
            {
                Compiler.CodeLabel allDone = ctx.DefineLabel();
                ctx.Branch(allDone, false);
                ctx.MarkLabel(readPacked);

                using var tok = new Compiler.Local(ctx, typeof(SubItemToken));
                ctx.LoadState();
                ctx.EmitCall(typeof(ProtoReader.State).GetMethod(nameof(ProtoReader.State.StartSubItem), Type.EmptyTypes));
                ctx.StoreValue(tok);

                Compiler.CodeLabel testForData = ctx.DefineLabel(), noMoreData = ctx.DefineLabel();
                ctx.MarkLabel(testForData);
                ctx.LoadState();
                ctx.LoadValue((int)packedWireType);
                ctx.EmitCall(typeof(ProtoReader.State).GetMethod(nameof(ProtoReader.State.HasSubValue)));
                ctx.BranchIfFalse(noMoreData, false);

                EmitReadAndAddItem(ctx, list, tail, add, castListForAdd);
                ctx.Branch(testForData, false);

                ctx.MarkLabel(noMoreData);
                ctx.LoadState();
                ctx.LoadValue(tok);
                ctx.EmitCall(typeof(ProtoReader.State).GetMethod(nameof(ProtoReader.State.EndSubItem),
                    new[] { typeof(SubItemToken) }));
                ctx.MarkLabel(allDone);
            }
        }

        private static void EmitReadAndAddItem(Compiler.CompilerContext ctx, Compiler.Local list, IRuntimeProtoSerializerNode tail, MethodInfo add, bool castListForAdd)
        {
            ctx.LoadAddress(list, list.Type); // needs to be the reference in case the list is value-type (static-call)
            if (castListForAdd) ctx.Cast(add.DeclaringType);

            Type itemType = tail.ExpectedType;
            bool tailReturnsValue = tail.ReturnsValue;
            if (tail.RequiresOldValue)
            {
                if (itemType.IsValueType || !tailReturnsValue)
                {
                    // going to need a variable
                    using Compiler.Local item = new Compiler.Local(ctx, itemType);
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
                    if (!tailReturnsValue) { ctx.LoadValue(item); }
                }
                else
                {    // no variable; pass the null on the stack and take the value *off* the stack
                    ctx.LoadNullRef();
                    tail.EmitRead(ctx, null);
                }
            }
            else
            {
                if (tailReturnsValue)
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
            if (addParamType != itemType)
            {
                if (addParamType == typeof(object))
                {
                    ctx.CastToObject(itemType);
                }
                else if (Nullable.GetUnderlyingType(addParamType) == itemType)
                { // list is nullable
                    ConstructorInfo ctor = Helpers.GetConstructor(addParamType, new Type[] { itemType }, false);
                    ctx.EmitCtor(ctor); // the itemType on the stack is now a Nullable<ItemType>
                }
                else
                {
                    throw new InvalidOperationException("Conflicting item/add type");
                }
            }
            ctx.EmitCall(add, list.Type);
            if (add.ReturnType != typeof(void))
            {
                ctx.DiscardValue();
            }
        }

        private static readonly System.Type ienumeratorType = typeof(IEnumerator), ienumerableType = typeof(IEnumerable);

        protected MethodInfo GetEnumeratorInfo(out MethodInfo moveNext, out MethodInfo current)
            => GetEnumeratorInfo(ExpectedType, Tail.ExpectedType, out moveNext, out current);
        internal static MethodInfo GetEnumeratorInfo(Type expectedType, Type itemType, out MethodInfo moveNext, out MethodInfo current)
        {
            Type enumeratorType = null, iteratorType;

            // try a custom enumerator
            MethodInfo getEnumerator = Helpers.GetInstanceMethod(expectedType, "GetEnumerator", null);

            Type getReturnType;
            if (getEnumerator != null)
            {
                getReturnType = getEnumerator.ReturnType;
                iteratorType = getReturnType
                    ;
                moveNext = Helpers.GetInstanceMethod(iteratorType, "MoveNext", null);
                PropertyInfo prop = Helpers.GetProperty(iteratorType, "Current", false);
                current = prop == null ? null : Helpers.GetGetMethod(prop, false, false);
                if (moveNext == null && (ienumeratorType.IsAssignableFrom(iteratorType)))
                {
                    moveNext = Helpers.GetInstanceMethod(ienumeratorType, "MoveNext", null);
                }
                // fully typed
                if (moveNext != null && moveNext.ReturnType == typeof(bool)
                    && current != null && current.ReturnType == itemType)
                {
                    return getEnumerator;
                }
#pragma warning disable IDE0059 // Unnecessary assignment of a value
                moveNext = current = getEnumerator = null;
#pragma warning restore IDE0059 // Unnecessary assignment of a value
            }

            // try IEnumerable<T>
            Type tmp = typeof(System.Collections.Generic.IEnumerable<>);
            if (tmp != null)
            {
                tmp = tmp.MakeGenericType(itemType);

                enumeratorType = tmp;
            }

            if (enumeratorType != null && enumeratorType.IsAssignableFrom(expectedType))
            {
                getEnumerator = Helpers.GetInstanceMethod(enumeratorType, "GetEnumerator");
                getReturnType = getEnumerator.ReturnType;
                iteratorType = getReturnType;

                moveNext = Helpers.GetInstanceMethod(ienumeratorType, "MoveNext");
                current = Helpers.GetGetMethod(Helpers.GetProperty(iteratorType, "Current", false), false, false);
                return getEnumerator;
            }
            // give up and fall-back to non-generic IEnumerable
            enumeratorType = ienumerableType;
            getEnumerator = Helpers.GetInstanceMethod(enumeratorType, "GetEnumerator");
            getReturnType = getEnumerator.ReturnType;
            iteratorType = getReturnType;
            moveNext = Helpers.GetInstanceMethod(iteratorType, "MoveNext");
            current = Helpers.GetGetMethod(Helpers.GetProperty(iteratorType, "Current", false), false, false);
            return getEnumerator;
        }

        protected override void EmitWrite(ProtoBuf.Compiler.CompilerContext ctx, ProtoBuf.Compiler.Local valueFrom)
        {
            using Compiler.Local list = ctx.GetLocalWithValue(ExpectedType, valueFrom);
            MethodInfo getEnumerator = GetEnumeratorInfo(out MethodInfo moveNext, out MethodInfo current);
            Debug.Assert(moveNext != null);
            Debug.Assert(current != null);
            Debug.Assert(getEnumerator != null);
            Type enumeratorType = getEnumerator.ReturnType;
            bool writePacked = WritePacked;
            using Compiler.Local iter = new Compiler.Local(ctx, enumeratorType);
            using Compiler.Local token = writePacked ? new Compiler.Local(ctx, typeof(SubItemToken)) : null;
            if (writePacked)
            {
                ctx.LoadState();
                ctx.LoadValue(fieldNumber);
                ctx.LoadValue((int)WireType.String);
                ctx.EmitCall(typeof(ProtoWriter.State).GetMethod(nameof(ProtoWriter.State.WriteFieldHeader)));

                ctx.LoadValue(list);
                ctx.LoadWriter(true);
                ctx.EmitCall(Compiler.WriterUtil.GetStaticMethod("StartSubItem", this));
                ctx.StoreValue(token);

                ctx.LoadValue(fieldNumber);
                ctx.LoadWriter(false);
                ctx.EmitCall(typeof(ProtoWriter).GetMethod("SetPackedField"));
            }

            ctx.LoadAddress(list, ExpectedType);
            ctx.EmitCall(getEnumerator, ExpectedType);
            ctx.StoreValue(iter);
            using (ctx.Using(iter))
            {
                Compiler.CodeLabel body = ctx.DefineLabel(), next = ctx.DefineLabel();
                ctx.Branch(next, false);

                ctx.MarkLabel(body);

                ctx.LoadAddress(iter, enumeratorType);
                ctx.EmitCall(current, enumeratorType);
                Type itemType = Tail.ExpectedType;
                if (itemType != typeof(object) && current.ReturnType == typeof(object))
                {
                    ctx.CastFromObject(itemType);
                }
                Tail.EmitWrite(ctx, null);

                ctx.MarkLabel(@next);
                ctx.LoadAddress(iter, enumeratorType);
                ctx.EmitCall(moveNext, enumeratorType);
                ctx.BranchIfTrue(body, false);
            }

            if (writePacked)
            {
                ctx.LoadValue(token);
                ctx.LoadWriter(true);
                ctx.EmitCall(Compiler.WriterUtil.GetStaticMethod("EndSubItem", this));
            }
        }

        public override void Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
        {
            SubItemToken token;
            bool writePacked = WritePacked;
            bool fixedSizePacked = (writePacked & CanUsePackedPrefix()) && value is ICollection;
            if (writePacked)
            {
                state.WriteFieldHeader(fieldNumber, WireType.String);
                if (fixedSizePacked)
                {
                    ProtoWriter.WritePackedPrefix(((ICollection)value).Count, packedWireType, dest, ref state);
                    token = default;
                }
                else
                {
                    token = ProtoWriter.StartSubItem(value, dest, ref state);
                }
                ProtoWriter.SetPackedField(fieldNumber, dest);
            }
            else
            {
                token = new SubItemToken(); // default
            }
            bool checkForNull = !SupportNull;
            foreach (object subItem in (IEnumerable)value)
            {
                if (checkForNull && subItem == null) { throw new NullReferenceException(); }
                Tail.Write(dest, ref state, subItem);
            }
            if (writePacked)
            {
                if (fixedSizePacked)
                {
                    ProtoWriter.ClearPackedField(fieldNumber, dest);
                }
                else
                {
                    ProtoWriter.EndSubItem(token, dest, ref state);
                }
            }
        }

        private bool CanUsePackedPrefix() =>
            ArrayDecorator.CanUsePackedPrefix(packedWireType, Tail.ExpectedType);

        public override object Read(ref ProtoReader.State state, object value)
        {
            try
            {
                int field = state.FieldNumber;
                object origValue = value;
                if (value == null) value = Activator.CreateInstance(concreteType, nonPublic: true);
                bool isList = IsList && !SuppressIList;
                if (packedWireType != WireType.None && state.WireType == WireType.String)
                {
                    SubItemToken token = state.StartSubItem();
                    if (isList)
                    {
                        IList list = (IList)value;
                        while (state.HasSubValue(packedWireType))
                        {
                            list.Add(Tail.Read(ref state, null));
                        }
                    }
                    else
                    {
                        object[] args = new object[1];
                        while (state.HasSubValue(packedWireType))
                        {
                            args[0] = Tail.Read(ref state, null);
                            add.Invoke(value, args);
                        }
                    }
                    state.EndSubItem(token);
                }
                else
                {
                    if (isList)
                    {
                        IList list = (IList)value;
                        do
                        {
                            list.Add(Tail.Read(ref state, null));
                        } while (state.TryReadFieldHeader(field));
                    }
                    else
                    {
                        object[] args = new object[1];
                        do
                        {
                            args[0] = Tail.Read(ref state, null);
                            add.Invoke(value, args);
                        } while (state.TryReadFieldHeader(field));
                    }
                }
                return origValue == value ? null : value;
            }
            catch (TargetInvocationException tie)
            {
                if (tie.InnerException != null) throw tie.InnerException;
                throw;
            }
        }
    }
}