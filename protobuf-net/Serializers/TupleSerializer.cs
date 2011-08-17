
using System;
using System.Reflection;
using ProtoBuf.Meta;
namespace ProtoBuf.Serializers
{
    sealed class TupleSerializer : IProtoTypeSerializer
    {
        private readonly MemberInfo[] members;
        private readonly ConstructorInfo ctor;
        private IProtoSerializer[] tails;
        public TupleSerializer(RuntimeTypeModel model, ConstructorInfo ctor, MemberInfo[] members)
        {
            if (ctor == null) throw new ArgumentNullException("ctor");
            if (members == null) throw new ArgumentNullException("members");
            this.ctor = ctor;
            this.members = members;
            this.tails = new IProtoSerializer[members.Length];

            ParameterInfo[] parameters = ctor.GetParameters();
            for(int i = 0 ; i < members.Length ; i++)
            {
                WireType wireType;
                Type finalType = parameters[i].ParameterType;

                Type itemType = null, defaultType = null;

                MetaType.ResolveListTypes(finalType, ref itemType, ref defaultType);
                Type tmp = itemType == null ? finalType : itemType;
                IProtoSerializer tail = ValueMember.TryGetCoreSerializer(model, DataFormat.Default, tmp, out wireType, false, false), serializer;
                if (tail == null) throw new InvalidOperationException("No serializer defined for type: " + tmp.FullName);

                tail = new TagDecorator(i + 1, wireType, false, tail);
                if(itemType == null)
                {
                    serializer = tail;
                }
                else
                {
                    if (finalType.IsArray)
                    {
                        serializer = new ArrayDecorator(tail, i + 1, false, wireType, finalType, false);
                    }
                    else
                    {
                        serializer = new ListDecorator(finalType, defaultType, tail, i + 1, false, wireType, true, false);
                    }
                }
                tails[i] = serializer;
            }
        }
        public bool HasCallbacks(Meta.TypeModel.CallbackType callbackType)
        {
            return false;
        }

        public void Callback(object value, Meta.TypeModel.CallbackType callbackType, SerializationContext context){}
#if FEAT_COMPILER
        public void EmitCallback(Compiler.CompilerContext ctx, Compiler.Local valueFrom, Meta.TypeModel.CallbackType callbackType){}
#endif
        public System.Type ExpectedType
        {
            get { return ctor.DeclaringType; }
        }

        public void Write(object value, ProtoWriter dest)
        {
            for(int i = 0 ; i < tails.Length ; i++)
            {
                object val = GetValue(value, i);
                if(val != null) tails[i].Write(val, dest);
            }
        }
        private object GetValue(object obj, int index)
        {
            switch (members[index].MemberType)
            {
                case MemberTypes.Field:
                    FieldInfo field = (FieldInfo)members[index];
                    if(obj == null)
                        return field.FieldType.IsValueType ? Activator.CreateInstance(field.FieldType) : null;
                    return field.GetValue(obj);
                case MemberTypes.Property:
                    PropertyInfo prop = (PropertyInfo)members[index];
                    if (obj == null) return prop.PropertyType.IsValueType ? Activator.CreateInstance(prop.PropertyType) : null;
                    return prop.GetValue(obj, null);
                default:
                    throw new InvalidOperationException();
            }
           
        }

        public object Read(object value, ProtoReader source)
        {
            object[] values = new object[members.Length];
            bool invokeCtor = false;
            if (value == null)
            {
                invokeCtor = true;
            }
            for (int i = 0; i < values.Length; i++)
                    values[i] = GetValue(value, i);
            int field;
            while((field = source.ReadFieldHeader()) > 0)
            {
                invokeCtor = true;
                if(field <= tails.Length)
                {
                    IProtoSerializer tail = tails[field - 1];
                    values[field - 1] = tails[field - 1].Read(tail.RequiresOldValue ? values[field - 1] : null, source);
                }
                else
                {
                    source.SkipField();
                }
            }
            return invokeCtor ? ctor.Invoke(values) : value;
        }

        public bool RequiresOldValue
        {
            get { return true; }
        }

        public bool ReturnsValue
        {
            get { return false; }
        }
        Type GetMemberType(int index)
        {
            switch (members[index].MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)members[index]).FieldType;
                case MemberTypes.Property:
                    return ((PropertyInfo)members[index]).PropertyType;
                default:
                    throw new InvalidOperationException();
            }
        }
#if FEAT_COMPILER
        public void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            using (Compiler.Local loc = ctx.GetLocalWithValue(ctor.DeclaringType, valueFrom))
            {
                for (int i = 0; i < tails.Length; i++)
                {
                    Type type = GetMemberType(i);
                    ctx.LoadAddress(loc, ExpectedType);
                    switch(members[i].MemberType)
                    {
                        case MemberTypes.Field:
                            ctx.LoadValue((FieldInfo)members[i]);
                            break;
                        case MemberTypes.Property:
                            ctx.LoadValue((PropertyInfo)members[i]);
                            break;
                    }
                    ctx.WriteNullCheckedTail(type, tails[i], null);
                }
            }
        }

        public void EmitRead(Compiler.CompilerContext ctx, Compiler.Local incoming)
        {
            using (Compiler.Local objValue = ctx.GetLocalWithValue(ExpectedType, incoming))
            {
                Compiler.Local[] locals = new Compiler.Local[members.Length];
                try
                {
                    for (int i = 0; i < locals.Length; i++)
                    {
                        Type type = GetMemberType(i);

                        locals[i] = new Compiler.Local(ctx, type);
                        if (!ExpectedType.IsValueType)
                        {
                            // value-types always read the old value
                            if (type.IsValueType)
                            {
                                switch (Type.GetTypeCode(type))
                                {
                                    case TypeCode.Boolean:
                                    case TypeCode.Byte:
                                    case TypeCode.Int16:
                                    case TypeCode.Int32:
                                    case TypeCode.SByte:
                                    case TypeCode.UInt16:
                                    case TypeCode.UInt32:
                                        ctx.LoadValue(0);
                                        break;
                                    case TypeCode.Int64:
                                    case TypeCode.UInt64:
                                        ctx.LoadValue(0L);
                                        break;
                                    case TypeCode.Single:
                                        ctx.LoadValue(0.0F);
                                        break;
                                    case TypeCode.Double:
                                        ctx.LoadValue(0.0D);
                                        break;
                                    case TypeCode.Decimal:
                                        ctx.LoadValue(0M);
                                        break;
                                    default:
                                        if (type == typeof (Guid))
                                        {
                                            ctx.LoadValue(Guid.Empty);
                                        }
                                        else
                                        {
                                            ctx.EmitCtor(type);
                                        }
                                        break;
                                }
                            }
                            else
                            {
                                ctx.LoadNullRef();
                            }
                            ctx.StoreValue(locals[i]);
                        }
                    }

                    Compiler.CodeLabel skipOld = ExpectedType.IsValueType
                                                        ? new Compiler.CodeLabel()
                                                        : ctx.DefineLabel();
                    if (!ExpectedType.IsValueType)
                    {
                        ctx.LoadAddress(objValue, ExpectedType);
                        ctx.BranchIfFalse(skipOld, false);
                    }
                    for (int i = 0; i < members.Length; i++)
                    {
                        ctx.LoadAddress(objValue, ExpectedType);
                        switch (members[i].MemberType)
                        {
                            case MemberTypes.Field:
                                ctx.LoadValue((FieldInfo) members[i]);
                                break;
                            case MemberTypes.Property:
                                ctx.LoadValue((PropertyInfo) members[i]);
                                break;
                        }
                        ctx.StoreValue(locals[i]);
                    }

                    if (!ExpectedType.IsValueType) ctx.MarkLabel(skipOld);

                    using (Compiler.Local fieldNumber = new Compiler.Local(ctx, typeof (int)))
                    {
                        Compiler.CodeLabel @continue = ctx.DefineLabel(),
                                           processField = ctx.DefineLabel(),
                                           notRecognised = ctx.DefineLabel();
                        ctx.Branch(@continue, false);

                        Compiler.CodeLabel[] handlers = new Compiler.CodeLabel[members.Length];
                        for (int i = 0; i < members.Length; i++)
                        {
                            handlers[i] = ctx.DefineLabel();
                        }

                        ctx.MarkLabel(processField);

                        ctx.LoadValue(fieldNumber);
                        ctx.LoadValue(1);
                        ctx.Subtract(); // jump-table is zero-based
                        ctx.Switch(handlers);

                        // and the default:
                        ctx.Branch(notRecognised, false);
                        for (int i = 0; i < handlers.Length; i++)
                        {
                            ctx.MarkLabel(handlers[i]);
                            IProtoSerializer tail = tails[i];
                            Compiler.Local oldValIfNeeded = tail.RequiresOldValue ? locals[i] : null;
                            ctx.ReadNullCheckedTail(locals[i].Type, tail, oldValIfNeeded);
                            if (tail.ReturnsValue)
                            {
                                if (locals[i].Type.IsValueType)
                                {
                                    ctx.StoreValue(locals[i]);
                                }
                                else
                                {
                                    Compiler.CodeLabel hasValue = ctx.DefineLabel(), allDone = ctx.DefineLabel();

                                    ctx.CopyValue();
                                    ctx.BranchIfTrue(hasValue, true); // interpret null as "don't assign"
                                    ctx.DiscardValue();
                                    ctx.Branch(allDone, true);
                                    ctx.MarkLabel(hasValue);
                                    ctx.StoreValue(locals[i]);
                                    ctx.MarkLabel(allDone);
                                }
                            }
                            ctx.Branch(@continue, false);
                        }

                        ctx.MarkLabel(notRecognised);
                        ctx.LoadReaderWriter();
                        ctx.EmitCall(typeof (ProtoReader).GetMethod("SkipField"));

                        ctx.MarkLabel(@continue);
                        ctx.EmitBasicRead("ReadFieldHeader", typeof (int));
                        ctx.CopyValue();
                        ctx.StoreValue(fieldNumber);
                        ctx.LoadValue(0);
                        ctx.BranchIfGreater(processField, false);
                    }
                    for (int i = 0; i < locals.Length; i++)
                    {
                        ctx.LoadValue(locals[i]);
                    }

                    ctx.EmitCtor(ctor);
                    ctx.StoreValue(objValue);
                }
                finally
                {
                    for (int i = 0; i < locals.Length; i++)
                    {
                        if (locals[i] != null)
                            locals[i].Dispose(); // release for re-use
                    }
                }
            }

        }
#endif
    }
}

