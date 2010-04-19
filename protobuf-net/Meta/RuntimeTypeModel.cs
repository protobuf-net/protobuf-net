#if !NO_RUNTIME
using System;
using System.Collections;
using System.Reflection;
#if FEAT_COMPILER
using System.Reflection.Emit;
#endif

using ProtoBuf.Serializers;


namespace ProtoBuf.Meta
{
    /// <summary>
    /// Provides protobuf serialization support for a number of types that can be defined at runtime
    /// </summary>
    public class RuntimeTypeModel : TypeModel
    {
        private class Singleton
        {
            private Singleton() { }
            internal static readonly RuntimeTypeModel Value = new RuntimeTypeModel(true);
        }
        public static RuntimeTypeModel Default
        {
            get { return Singleton.Value; }
        }
        public IEnumerable GetTypes() { return types; }
        private readonly bool isDefault;
        internal RuntimeTypeModel(bool isDefault)
        {
            AutoAddMissingTypes = true;
            this.isDefault = isDefault;
        }
        public MetaType this[Type type] { get { return (MetaType)types[FindOrAddAuto(type, true)]; } }
        MetaType FindWithoutAdd(Type type)
        {
            // this list is thread-safe for reading
            foreach (MetaType metaType in types)
            {
                if (metaType.Type == type) return metaType;
            }
            // if that failed, check for a proxy
            Type underlyingType = ResolveProxies(type);
            return underlyingType == null ? null : FindWithoutAdd(underlyingType);
        }
        sealed class TypeFinder : BasicList.IPredicate
        {
            private readonly Type type;
            public TypeFinder(Type type) { this.type = type; }
            public bool IsMatch(object obj)
            {
                return ((MetaType)obj).Type == type;
            }
        }
        internal int FindOrAddAuto(Type type, bool demand)
        {
            TypeFinder predicate = new TypeFinder(type);
            int key = types.IndexOf(predicate);

            if (key < 0)
            {
                // check for proxy types
                Type underlyingType = ResolveProxies(type);
                if (underlyingType != null)
                {
                    predicate = new TypeFinder(underlyingType);
                    key = types.IndexOf(predicate);
                }
            }

            if (key < 0)
            {
                if (!autoAddMissingTypes)
                {
                    if(demand)throw new ArgumentException("type");
                    return key;
                }
                MetaType metaType = Create(type);
                metaType.ApplyDefaultBehaviour();
                lock (types)
                {   // double-checked
                    int winner = types.IndexOf(predicate);
                    if (winner < 0)
                    {
                        ThrowIfFrozen();
                        key = types.Add(metaType);
                    }
                    else
                    {
                        key = winner;
                    }
                }
            }
            return key;
        }
        private MetaType Create(Type type)
        {
            ThrowIfFrozen();
            return new MetaType(this, type);
        }
        public MetaType Add(Type type, bool applyDefaultBehaviour)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (FindWithoutAdd(type) != null) throw new ArgumentException("Duplicate type", "type");
            MetaType newType = Create(type);
            if (applyDefaultBehaviour) { newType.ApplyDefaultBehaviour(); }
            lock (types)
            {
                // double checked
                if (FindWithoutAdd(type) != null) throw new ArgumentException("Duplicate type", "type");
                ThrowIfFrozen();
                types.Add(newType);
            }
            if (applyDefaultBehaviour && FindWithoutAdd(type.BaseType) == null
                && MetaType.GetContractFamily(type.BaseType, null) != MetaType.AttributeFamily.None)
            {
                FindOrAddAuto(type, true);
            }
            return newType;
        }        

        bool frozen, autoAddMissingTypes;
        public bool AutoAddMissingTypes
        {
            get { return autoAddMissingTypes; }
            set {
                if (!value && isDefault)
                {
                    throw new InvalidOperationException("The default model must allow missing types");
                }
                ThrowIfFrozen();
                autoAddMissingTypes = value;
            }
        }
        protected void ThrowIfFrozen()
        {
            if (frozen) throw new InvalidOperationException("The model cannot be changed once frozen");
        }
        public void Freeze()
        {
            if (isDefault) throw new InvalidOperationException("The default model cannot be frozen");
            frozen = true;
        }

        private readonly BasicList types = new BasicList();

        protected override int GetKeyImpl(Type type)
        {
            return GetKey(type, true, true);
        }
        internal int GetKey(Type type, bool demand, bool getBaseKey)
        {
            int typeIndex = FindOrAddAuto(type, demand);
            if (typeIndex >= 0)
            {
                MetaType mt = (MetaType)types[typeIndex], baseType;
                if (getBaseKey && (baseType = mt.BaseType) != null)
                {   // part of an inheritance tree; pick the base-key
                    while (baseType != null) { mt = baseType; baseType = baseType.BaseType;}
                    typeIndex = FindOrAddAuto(mt.Type, true);
                }
            }
            return typeIndex;
        }
        protected internal override void Serialize(int key, object value, ProtoWriter dest)
        {
            //Helpers.DebugWriteLine("Serialize", value);
            ((MetaType)types[key]).Serializer.Write(value, dest);
        }

        protected internal override object Deserialize(int key, object value, ProtoReader source)
        {
            //Helpers.DebugWriteLine("Deserialize", value);
            IProtoSerializer ser = ((MetaType)types[key]).Serializer;
            if (value == null && ser.ExpectedType.IsValueType) {
                return ser.Read(Activator.CreateInstance(ser.ExpectedType), source);
            } else {
                return ser.Read(value, source);
            }
        }

#if FEAT_COMPILER
        internal static Compiler.ProtoSerializer GetSerializer(IProtoSerializer serializer, bool compiled)
        {
            if (serializer == null) throw new ArgumentNullException("serializer");
#if FEAT_COMPILER && !FX11
            if (compiled) return Compiler.CompilerContext.BuildSerializer(serializer);
#endif
            return new Compiler.ProtoSerializer(serializer.Write);
        }

#if !FX11
        /// <summary>
        /// Compiles the serializers individually; this is *not* a full
        /// standalone compile, but can significantly boost performance
        /// while allowing additional types to be added.
        /// </summary>
        public void CompileInPlace()
        {
            foreach (MetaType type in types)
            {
                type.CompileInPlace();
            }
        }
#endif
#endif
        //internal override IProtoSerializer GetTypeSerializer(Type type)
        //{   // this list is thread-safe for reading
        //    .Serializer;
        //}
        //internal override IProtoSerializer GetTypeSerializer(int key)
        //{   // this list is thread-safe for reading
        //    MetaType type = (MetaType)types.TryGet(key);
        //    if (type != null) return type.Serializer;
        //    throw new KeyNotFoundException();

        //}

#if FEAT_COMPILER
        internal class SerializerPair : IComparable
        {
            int IComparable.CompareTo(object obj)
            {
                if (obj == null) throw new ArgumentException("obj");
                SerializerPair other = (SerializerPair)obj;

                // we want to bunch all the items with the same base-type together, but we need the items with a
                // different base **first**.
                if (this.BaseKey == this.MetaKey)
                {
                    if (other.BaseKey == other.MetaKey)
                    { // neither is a subclass
                        return this.MetaKey.CompareTo(other.MetaKey);
                    }
                    else
                    { // "other" (only) is involved in inheritance; "other" should be first
                        return 1;
                    }
                }
                else
                {
                    if (other.BaseKey == other.MetaKey)
                    { // "this" (only) is involved in inheritance; "this" should be first
                        return -1;
                    }
                    else
                    { // both are involved in inheritance
                        int result = this.BaseKey.CompareTo(other.BaseKey);
                        if (result == 0) result = this.MetaKey.CompareTo(other.MetaKey);
                        return result;
                    }
                }
            }
            public readonly int MetaKey, BaseKey;
            public readonly MetaType Type;
            public readonly MethodBuilder Serialize, Deserialize;
            public readonly ILGenerator SerializeBody, DeserializeBody;
            public SerializerPair(int metaKey, int baseKey, MetaType type, MethodBuilder serialize, MethodBuilder deserialize,
                ILGenerator serializeBody, ILGenerator deserializeBody)
            {
                this.MetaKey = metaKey;
                this.BaseKey = baseKey;
                this.Serialize = serialize;
                this.Deserialize = deserialize;
                this.SerializeBody = serializeBody;
                this.DeserializeBody = deserializeBody;
                this.Type = type;
            }
        }

        public TypeModel Compile()
        {
            return Compile(null, null);
        }
        static ILGenerator Override(TypeBuilder type, string name)
        {
            MethodInfo baseMethod = type.BaseType.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
            ParameterInfo[] parameters = baseMethod.GetParameters();
            Type[] paramTypes = new Type[parameters.Length];
            for(int i = 0 ; i < paramTypes.Length ; i++) {
                paramTypes[i] = parameters[i].ParameterType;
            }
            MethodBuilder newMethod = type.DefineMethod(baseMethod.Name,
                (baseMethod.Attributes & ~MethodAttributes.Abstract) | MethodAttributes.Final, baseMethod.CallingConvention, baseMethod.ReturnType, paramTypes);
            ILGenerator il = newMethod.GetILGenerator();
            type.DefineMethodOverride(newMethod, baseMethod);
            return il;
        }
        public TypeModel Compile(string name, string path)
        {
            Freeze();
            bool save = !Helpers.IsNullOrEmpty(path);
            if (Helpers.IsNullOrEmpty(name))
            {
                if (save) throw new ArgumentNullException("name");
                name = Guid.NewGuid().ToString();
            }

            AssemblyName an = new AssemblyName();
            an.Name = name;
            AssemblyBuilder asm = AppDomain.CurrentDomain.DefineDynamicAssembly(an,
                (save ? AssemblyBuilderAccess.RunAndSave : AssemblyBuilderAccess.Run)
                );

            ModuleBuilder module = save ? asm.DefineDynamicModule(name, path)
                : asm.DefineDynamicModule(name);
            Type baseType = typeof(TypeModel);
            TypeBuilder type = module.DefineType(name,
                (baseType.Attributes & ~TypeAttributes.Abstract) | TypeAttributes.Sealed,
                baseType);
            Compiler.CompilerContext ctx;
            
            int index = 0;
            bool hasInheritance = false;
            SerializerPair[] methodPairs = new SerializerPair[types.Count];
            foreach (MetaType metaType in types)
            {
                MethodBuilder writeMethod = type.DefineMethod("Write",
                    MethodAttributes.Private | MethodAttributes.Static, CallingConventions.Standard,
                    typeof(void), new Type[] { metaType.Type, typeof(ProtoWriter) });

                MethodBuilder readMethod = type.DefineMethod("Read",
                    MethodAttributes.Private | MethodAttributes.Static, CallingConventions.Standard,
                    metaType.Type, new Type[] { metaType.Type, typeof(ProtoReader) });

                SerializerPair pair = new SerializerPair(
                    GetKey(metaType.Type, true, false), GetKey(metaType.Type, true, true), metaType,
                    writeMethod, readMethod, writeMethod.GetILGenerator(), readMethod.GetILGenerator());
                methodPairs[index++] = pair;
                if (pair.MetaKey != pair.BaseKey) hasInheritance = true;
            }
            if (hasInheritance)
            {
                Array.Sort(methodPairs);
            }
            
            for(index = 0; index < methodPairs.Length ; index++)
            {
                SerializerPair pair = methodPairs[index];
                ctx = new Compiler.CompilerContext(pair.SerializeBody, true, methodPairs);
                pair.Type.Serializer.EmitWrite(ctx, Compiler.Local.InputValue);
                ctx.Return();

                ctx = new Compiler.CompilerContext(pair.DeserializeBody, true, methodPairs);
                pair.Type.Serializer.EmitRead(ctx, Compiler.Local.InputValue);
                ctx.LoadValue(Compiler.Local.InputValue);
                ctx.Return();
            }

            FieldBuilder knownTypes = type.DefineField("knownTypes", typeof(Type[]), FieldAttributes.Private | FieldAttributes.InitOnly);

            ILGenerator il = Override(type, "GetKeyImpl");
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, knownTypes);
            il.Emit(OpCodes.Ldarg_1);
            // note that Array.IndexOf is not supported under CF
            il.EmitCall(OpCodes.Callvirt,typeof(IList).GetMethod(
                "IndexOf", new Type[] { typeof(object) }), null);
            if (hasInheritance)
            {
                LocalBuilder keyVar = il.DeclareLocal(typeof(int));
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Stloc_0);

                BasicList getKeyLabels = new BasicList();
                int lastKey = -1;
                for (int i = 0; i < methodPairs.Length; i++)
                {
                    if (methodPairs[i].MetaKey == methodPairs[i].BaseKey) break;
                    if (lastKey == methodPairs[i].BaseKey)
                    {   // add the last label again
                        getKeyLabels.Add(getKeyLabels[getKeyLabels.Count - 1]);
                    }
                    else
                    {   // add a new unique label
                        getKeyLabels.Add(il.DefineLabel());
                        lastKey = methodPairs[i].BaseKey;
                    }                    
                }
                Label[] subtypeLabels = new Label[getKeyLabels.Count];
                getKeyLabels.CopyTo(subtypeLabels, 0);

                il.Emit(OpCodes.Switch, subtypeLabels);
                il.Emit(OpCodes.Ldloc_0); // not a sub-type; use the original value
                il.Emit(OpCodes.Ret);

                lastKey = -1;
                // now output the different branches per sub-type (not derived type)
                for (int i = subtypeLabels.Length - 1; i >= 0; i--)
                {
                    if (lastKey != methodPairs[i].BaseKey)
                    {
                        lastKey = methodPairs[i].BaseKey;
                        // find the actual base-index for this base-key (i.e. the index of
                        // the base-type)
                        int keyIndex = -1;
                        for (int j = subtypeLabels.Length; j < methodPairs.Length; j++)
                        {
                            if (methodPairs[j].BaseKey == lastKey && methodPairs[j].MetaKey == lastKey)
                            {
                                keyIndex = j;
                                break;
                            }
                        }
                        il.MarkLabel(subtypeLabels[i]);
                        Compiler.CompilerContext.LoadValue(il, keyIndex);
                        il.Emit(OpCodes.Ret);
                    }
                }
            }
            else
            {
                il.Emit(OpCodes.Ret);
            }
            
            il = Override(type, "Serialize");
            ctx = new Compiler.CompilerContext(il, false, methodPairs);
            // arg0 = this, arg1 = key, arg2=obj, arg3=dest
            Label[] jumpTable = new Label[types.Count];
            for (int i = 0; i < jumpTable.Length; i++) {
                jumpTable[i] = il.DefineLabel();
            }
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Switch, jumpTable);
            ctx.Return();
            for (int i = 0; i < jumpTable.Length; i++)
            {
                SerializerPair pair = methodPairs[i];
                il.MarkLabel(jumpTable[i]);
                il.Emit(OpCodes.Ldarg_2);
                ctx.CastFromObject(pair.Type.Type);
                il.Emit(OpCodes.Ldarg_3);
                il.EmitCall(OpCodes.Call, pair.Serialize, null);
                ctx.Return();
            }

            il = Override(type, "Deserialize");
            ctx = new Compiler.CompilerContext(il, false, methodPairs);
            // arg0 = this, arg1 = key, arg2=obj, arg3=source
            for (int i = 0; i < jumpTable.Length; i++)
            {
                jumpTable[i] = il.DefineLabel();
            }
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Switch, jumpTable);
            ctx.LoadNullRef();
            ctx.Return();
            for (int i = 0; i < jumpTable.Length; i++)
            {
                SerializerPair pair = methodPairs[i];
                il.MarkLabel(jumpTable[i]);
                Type keyType = pair.Type.Type;
                if (keyType.IsValueType)
                {
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Ldarg_3);
                    il.EmitCall(OpCodes.Call, EmitBoxedSerializer(type, i, keyType, methodPairs), null);
                    ctx.Return();
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_2);
                    ctx.CastFromObject(keyType);
                    il.Emit(OpCodes.Ldarg_3);
                    il.EmitCall(OpCodes.Call, pair.Deserialize, null);
                    ctx.Return();
                }
            }

            

            ConstructorBuilder ctor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, Helpers.EmptyTypes);
            
            il = ctor.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, baseType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0]);
            il.Emit(OpCodes.Ldarg_0);
            Compiler.CompilerContext.LoadValue(il, types.Count);
            il.Emit(OpCodes.Newarr, typeof(Type));
            
            index = 0;
            foreach(SerializerPair pair in methodPairs)
            {
                il.Emit(OpCodes.Dup);
                Compiler.CompilerContext.LoadValue(il, index);
                il.Emit(OpCodes.Ldtoken, pair.Type.Type);
                il.EmitCall(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"), null);
                il.Emit(OpCodes.Stelem_Ref);
                index++;
            }
            il.Emit(OpCodes.Stfld, knownTypes);            
            il.Emit(OpCodes.Ret);


            Type finalType = type.CreateType();
            if(!Helpers.IsNullOrEmpty(path))
            {
                asm.Save(path);
                Helpers.DebugWriteLine("Wrote dll:" + path);
            }

            return (TypeModel)Activator.CreateInstance(finalType);
        }

        private static MethodBuilder EmitBoxedSerializer(TypeBuilder type, int i, Type valueType, SerializerPair[] methodPairs)
        {
            MethodInfo dedicated = methodPairs[i].Deserialize;
            MethodBuilder boxedSerializer = type.DefineMethod("_" + i, MethodAttributes.Static, CallingConventions.Standard,
                typeof(object), new Type[] { typeof(object), typeof(ProtoReader) });
            Compiler.CompilerContext ctx = new Compiler.CompilerContext(boxedSerializer.GetILGenerator(), true, methodPairs);
            ctx.LoadValue(Compiler.Local.InputValue);
            Compiler.CodeLabel @null = ctx.DefineLabel();
            ctx.BranchIfFalse(@null, true);

            ctx.LoadValue(Compiler.Local.InputValue);
            ctx.CastFromObject(valueType);
            ctx.LoadReaderWriter();
            ctx.EmitCall(dedicated);
            ctx.CastToObject(valueType);
            ctx.Return();

            ctx.MarkLabel(@null);
            using(Compiler.Local typedVal = new Compiler.Local(ctx, valueType))
            {
                // create a new valueType
                ctx.LoadAddress(typedVal, valueType);
                ctx.EmitCtor(valueType);
                ctx.LoadValue(typedVal);
                ctx.LoadReaderWriter();
                ctx.EmitCall(dedicated);
                ctx.CastToObject(valueType);
                ctx.Return();
            }
            return boxedSerializer;
        }
        
#endif

        internal bool IsDefined(Type type, int fieldNumber)
        {
            return FindWithoutAdd(type).IsDefined(fieldNumber);
        }
    }
    
}
#endif