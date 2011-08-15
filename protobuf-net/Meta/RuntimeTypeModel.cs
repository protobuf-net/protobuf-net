#if !NO_RUNTIME
using System;
using System.Collections;
using System.Reflection;
#if FEAT_COMPILER
using System.Reflection.Emit;
#endif

using ProtoBuf.Serializers;
using System.Threading;


namespace ProtoBuf.Meta
{
    /// <summary>
    /// Provides protobuf serialization support for a number of types that can be defined at runtime
    /// </summary>
    public sealed class RuntimeTypeModel : TypeModel
    {
        private byte options;
        private const byte
           OPTIONS_InferTagFromNameDefault = 1,
           OPTIONS_IsDefaultModel = 2,
           OPTIONS_Frozen = 4,
           OPTIONS_AutoAddMissingTypes = 8,
#if FEAT_COMPILER && !FX11
           OPTIONS_AutoCompile = 16,
#endif
           OPTIONS_UseImplicitZeroDefaults = 32;
        private bool GetOption(byte option)
        {
            return (options & option) == option;
        }
        private void SetOption(byte option, bool value)
        {
            if (value) options |= option;
            else options &= (byte)~option;
        }
        /// <summary>
        /// Global default for that
        /// enables/disables automatic tag generation based on the existing name / order
        /// of the defined members. See <seealso cref="ProtoContractAttribute.InferTagFromName"/>
        /// for usage and <b>important warning</b> / explanation.
        /// You must set the global default before attempting to serialize/deserialize any
        /// impacted type.
        /// </summary>
        public bool InferTagFromNameDefault
        {
            get { return GetOption(OPTIONS_InferTagFromNameDefault); }
            set { SetOption(OPTIONS_InferTagFromNameDefault, value); }
        }
        /// <summary>
        /// Global switch that enables or disables the implicit
        /// handling of "zero defaults"; meanning: if no other default is specified,
        /// it assumes bools always default to false, integers to zero, etc.
        /// 
        /// If this is disabled, no such assumptions are made and only *explicit*
        /// default values are processed. This is enabled by default to 
        /// preserve similar logic to v1.
        /// </summary>
        public bool UseImplicitZeroDefaults
        {
            get {return GetOption(OPTIONS_UseImplicitZeroDefaults);}
            set {
                if (!value && GetOption(OPTIONS_IsDefaultModel))
                {
                    throw new InvalidOperationException("UseImplicitZeroDefaults cannot be disabled on the default model");
                }
                SetOption(OPTIONS_UseImplicitZeroDefaults, value);
            }
        }

        private class Singleton
        {
            private Singleton() { }
            internal static readonly RuntimeTypeModel Value = new RuntimeTypeModel(true);
        }
        /// <summary>
        /// The default model, used to support ProtoBuf.Serializer
        /// </summary>
        public static RuntimeTypeModel Default
        {
            get { return Singleton.Value; }
        }
        /// <summary>
        /// Returns a sequence of the Type instances that can be
        /// processed by this model.
        /// </summary>
        public IEnumerable GetTypes() { return types; }

        internal RuntimeTypeModel(bool isDefault)
        {
            AutoAddMissingTypes = true;
            UseImplicitZeroDefaults = true;
            SetOption(OPTIONS_IsDefaultModel, isDefault);
#if FEAT_COMPILER && !FX11 && !DEBUG
            AutoCompile = true;
#endif
        }
        /// <summary>
        /// Obtains the MetaType associated with a given Type for the current model,
        /// allowing additional configuration.
        /// </summary>
        public MetaType this[Type type] { get { return (MetaType)types[FindOrAddAuto(type, true, false,false)]; } }
        
        internal MetaType FindWithoutAdd(Type type)
        {
            // this list is thread-safe for reading
            foreach (MetaType metaType in types)
            {
                if (metaType.Type == type)
                {
                    if (metaType.Pending) WaitOnLock(metaType);
                    return metaType;
                }
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
        private void WaitOnLock(MetaType type)
        {
            bool lockTaken = false;
            try
            {
                TakeLock(ref lockTaken);
            }
            finally
            {
                ReleaseLock(lockTaken);
            }
        }
        internal int FindOrAddAuto(Type type, bool demand, bool addWithContractOnly, bool addEvenIfAutoDisabled)
        {
            TypeFinder predicate = new TypeFinder(type);
            int key = types.IndexOf(predicate);
            MetaType metaType;
            if (key >= 0 && (metaType = ((MetaType)types[key])).Pending)
            {
                WaitOnLock(metaType);
            }
            if (key < 0)
            {
                // check for proxy types
                Type underlyingType = ResolveProxies(type);
                if (underlyingType != null)
                {
                    predicate = new TypeFinder(underlyingType);
                    key = types.IndexOf(predicate);
                    type = underlyingType; // if new added, make it reflect the underlying type
                }
            }

            if (key < 0)
            {
                bool lockTaken = false;
                try
                {
                    TakeLock(ref lockTaken);
                    // try to recognise a few familiar patterns...
                    if ((metaType = RecogniseCommonTypes(type)) == null)
                    { // otherwise, check if it is a contract
                        MetaType.AttributeFamily family = MetaType.GetContractFamily(type, null);
                        if (family == MetaType.AttributeFamily.AutoTuple) addEvenIfAutoDisabled = true; // always add basic tuples, such as KeyValuePair

                        bool shouldAdd = AutoAddMissingTypes || addEvenIfAutoDisabled;
                        if (!shouldAdd || (
                            !type.IsEnum && addWithContractOnly && family == MetaType.AttributeFamily.None)
                            )
                        {
                            if (demand) ThrowUnexpectedType(type);
                            return key;
                        }
                        metaType = Create(type);
                    }
                    metaType.Pending = true;                    
                    bool weAdded = false;

                    // double-checked
                    int winner = types.IndexOf(predicate);
                    if (winner < 0)
                    {
                        ThrowIfFrozen();
                        key = types.Add(metaType);
                        weAdded = true;
                    }
                    else
                    {
                        key = winner;
                    }
                    if (weAdded)
                    {
                        metaType.ApplyDefaultBehaviour();
                        metaType.Pending = false;
                    }
                }
                finally
                {
                    ReleaseLock(lockTaken);
                }
            }
            return key;
        }

        private MetaType RecogniseCommonTypes(Type type)
        {
//#if !NO_GENERICS
//            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.KeyValuePair<,>))
//            {
//                MetaType mt = new MetaType(this, type);

//                Type surrogate = typeof (KeyValuePairSurrogate<,>).MakeGenericType(type.GetGenericArguments());

//                mt.SetSurrogate(surrogate);
//                mt.IncludeSerializerMethod = false;
//                mt.Freeze();

//                MetaType surrogateMeta = (MetaType)types[FindOrAddAuto(surrogate, true, true, true)]; // this forcibly adds it if needed
//                if(surrogateMeta.IncludeSerializerMethod)
//                { // don't blindly set - it might be frozen
//                    surrogateMeta.IncludeSerializerMethod = false;
//                }
//                surrogateMeta.Freeze();
//                return mt;
//            }
//#endif
            return null;
        }
        private MetaType Create(Type type)
        {
            ThrowIfFrozen();
            return new MetaType(this, type);
        }


        /// <summary>
        /// Adds support for an additional type in this model, optionally
        /// appplying inbuilt patterns. If the type is already known to the
        /// model, the existing type is returned **without** applying
        /// any additional behaviour.
        /// </summary>
        /// <remarks>Inbuilt patterns include:
        /// [ProtoContract]/[ProtoMember(n)]
        /// [DataContract]/[DataMember(Order=n)]
        /// [XmlType]/[XmlElement(Order=n)]
        /// [On{Des|S}erializ{ing|ed}]
        /// ShouldSerialize*/*Specified
        /// </remarks>
        /// <param name="type">The type to be supported</param>
        /// <param name="applyDefaultBehaviour">Whether to apply the inbuilt configuration patterns (via attributes etc), or
        /// just add the type with no additional configuration (the type must then be manually configured).</param>
        /// <returns>The MetaType representing this type, allowing
        /// further configuration.</returns>
        public MetaType Add(Type type, bool applyDefaultBehaviour)
        {
            if (type == null) throw new ArgumentNullException("type");
            MetaType newType = FindWithoutAdd(type);
            if (newType != null) return newType; // return existing
            bool lockTaken = false;

            if (type.IsInterface && typeof(IEnumerable).IsAssignableFrom(type)
                && GetListItemType(type) == null)
            {
                throw new ArgumentException("IEnumerable[<T>] data cannot be used as a meta-type unless an Add method can be resolved");
            }
            try
            {
                newType = RecogniseCommonTypes(type);
                if(newType != null)
                {
                    if(!applyDefaultBehaviour) {
                        throw new ArgumentException(
                            "Default behaviour must be observed for certain types with special handling; " + type.Name,
                            "applyDefaultBehaviour");
                    }
                    // we should assume that type is fully configured, though; no need to re-run:
                    applyDefaultBehaviour = false;
                }
                if(newType == null) newType = Create(type);
                newType.Pending = true;
                TakeLock(ref lockTaken);
                // double checked
                if (FindWithoutAdd(type) != null) throw new ArgumentException("Duplicate type", "type");
                ThrowIfFrozen();
                types.Add(newType);
                if (applyDefaultBehaviour) { newType.ApplyDefaultBehaviour(); }
                newType.Pending = false;
            }
            finally
            {
                ReleaseLock(lockTaken);
            }
            
            return newType;
        }

#if FEAT_COMPILER && !FX11
        /// <summary>
        /// Should serializers be compiled on demand? It may be useful
        /// to disable this for debugging purposes.
        /// </summary>
        public bool AutoCompile
        {
            get { return GetOption(OPTIONS_AutoCompile); }
            set { SetOption(OPTIONS_AutoCompile, value); }
        }
#endif
        /// <summary>
        /// Should support for unexpected types be added automatically?
        /// If false, an exception is thrown when unexpected types
        /// are encountered.
        /// </summary>
        public bool AutoAddMissingTypes
        {
            get { return GetOption(OPTIONS_AutoAddMissingTypes); }
            set {
                if (!value && GetOption(OPTIONS_IsDefaultModel))
                {
                    throw new InvalidOperationException("The default model must allow missing types");
                }
                ThrowIfFrozen();
                SetOption(OPTIONS_AutoAddMissingTypes, value);
            }
        }
        /// <summary>
        /// Verifies that the model is still open to changes; if not, an exception is thrown
        /// </summary>
        private void ThrowIfFrozen()
        {
            if (GetOption(OPTIONS_Frozen)) throw new InvalidOperationException("The model cannot be changed once frozen");
        }
        /// <summary>
        /// Prevents further changes to this model
        /// </summary>
        public void Freeze()
        {
            if (GetOption(OPTIONS_IsDefaultModel)) throw new InvalidOperationException("The default model cannot be frozen");
            SetOption(OPTIONS_Frozen, true);
        }

        private readonly BasicList types = new BasicList();

        /// <summary>
        /// Provides the key that represents a given type in the current model.
        /// </summary>
        protected override int GetKeyImpl(Type type)
        {
            return GetKey(type, false, true);
        }
        internal int GetKey(Type type, bool demand, bool getBaseKey)
        {
            Helpers.DebugAssert(type != null);
            try
            {
                int typeIndex = FindOrAddAuto(type, demand, true, false);
                if (typeIndex >= 0)
                {
                    MetaType mt = (MetaType)types[typeIndex];
                    if (getBaseKey)
                    {
                        mt = MetaType.GetRootType(mt);
                        typeIndex = FindOrAddAuto(mt.Type, true, true, false);                        
                    }
                }
                return typeIndex;
            }
            catch (NotSupportedException)
            {
                throw; // re-surface "as-is"
            }
            catch (Exception ex)
            {
                if (ex.Message.IndexOf(type.FullName) >= 0) throw;  // already enough info
                throw new ProtoException(ex.Message + " (" + type.FullName + ")", ex);
            }
        }
        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied stream.
        /// </summary>
        /// <param name="key">Represents the type (including inheritance) to consider.</param>
        /// <param name="value">The existing instance to be serialized (cannot be null).</param>
        /// <param name="dest">The destination stream to write to.</param>
        protected internal override void Serialize(int key, object value, ProtoWriter dest)
        {
            //Helpers.DebugWriteLine("Serialize", value);
            ((MetaType)types[key]).Serializer.Write(value, dest);
        }
        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (which may be null).
        /// </summary>
        /// <param name="key">Represents the type (including inheritance) to consider.</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
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
        /// <remarks>An in-place compile can access non-public types / members</remarks>
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

        /// <summary>
        /// Fully compiles the current model into a static-compiled model instance
        /// </summary>
        /// <remarks>A full compilation is restricted to accessing public types / members</remarks>
        /// <returns>An instance of the newly created compiled type-model</returns>
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
        private void BuildAllSerializers()
        {
            // note that types.Count may increase during this operation, as some serializers
            // bring other types into play
            for (int i = 0; i < types.Count; i++)
            {
                // the primary purpose of this is to force the creation of the Serializer
                MetaType mt = (MetaType) types[i];
                if (mt.Serializer == null)
                    throw new InvalidOperationException("No serializer available for " + mt.Type.Name);
            }
        }
        /// <summary>
        /// Fully compiles the current model into a static-compiled serialization dll
        /// (the serialization dll still requires protobuf-net for support services).
        /// </summary>
        /// <remarks>A full compilation is restricted to accessing public types / members</remarks>
        /// <param name="name">The name of the TypeModel class to create</param>
        /// <param name="path">The path for the new dll</param>
        /// <returns>An instance of the newly created compiled type-model</returns>
        public TypeModel Compile(string name, string path)
        {
            BuildAllSerializers();
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
                ctx = new Compiler.CompilerContext(pair.SerializeBody, true, true, methodPairs);
                pair.Type.Serializer.EmitWrite(ctx, Compiler.Local.InputValue);
                ctx.Return();

                ctx = new Compiler.CompilerContext(pair.DeserializeBody, true, false, methodPairs);
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
                il.DeclareLocal(typeof(int)); // loc-0
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
            ctx = new Compiler.CompilerContext(il, false, true, methodPairs);
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
            ctx = new Compiler.CompilerContext(il, false, false, methodPairs);
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
            Compiler.CompilerContext ctx = new Compiler.CompilerContext(boxedSerializer.GetILGenerator(), true, false, methodPairs);
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

        internal EnumSerializer.EnumPair[] GetEnumMap(Type type)
        {
            int index = FindOrAddAuto(type, false, false, false);
            return index < 0 ? null : ((MetaType)types[index]).GetEnumMap();
        }

        private int metadataTimeoutMilliseconds = 5000;
        /// <summary>
        /// The amount of time to wait if there are concurrent metadata access operations
        /// </summary>
        public int MetadataTimeoutMilliseconds
        {
            get { return metadataTimeoutMilliseconds; }
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException("MetadataTimeoutMilliseconds");
                metadataTimeoutMilliseconds = value;
            }
        }

        internal void TakeLock(ref bool lockTaken)
        {
            lockTaken = false;
#if CF2 || CF35
            int remaining = metadataTimeoutMilliseconds;
            do {
                lockTaken = Monitor.TryEnter(types);
                if(!lockTaken)
                {
                    if(remaining <= 0) throw new TimeoutException("Timeout while inspecting metadata; this may indicate a deadlock. This can often be avoided by preparing necessary serializers during application initialization, rather than allowing multiple threads to perform the initial metadata inspection");
                    remaining -= 50;
                    Thread.Sleep(50);
                }
            } while(!lockTaken);
#else
            if (Monitor.TryEnter(types, metadataTimeoutMilliseconds))
            {
                lockTaken = true;
            }
            else
            {
#if FX11
                throw new InvalidOperationException("Timeout while inspecting metadata; this may indicate a deadlock. This can often be avoided by preparing necessary serializers during application initialization, rather than allowing multiple threads to perform the initial metadata inspection");
#else
                throw new TimeoutException("Timeout while inspecting metadata; this may indicate a deadlock. This can often be avoided by preparing necessary serializers during application initialization, rather than allowing multiple threads to perform the initial metadata inspection");
#endif
            }
#endif
        }

        internal void ReleaseLock(bool lockTaken)
        {
            if (lockTaken)
            {
                Monitor.Exit(types);
            }
        }
    }
    
}
#endif