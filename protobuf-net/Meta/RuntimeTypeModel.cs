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
        public MetaType this[Type type] { get { return Find(type); } }
        MetaType Find(Type type)
        {
            // this list is thread-safe for reading
            foreach (MetaType metaType in types)
            {
                if (metaType.Type == type) return metaType;
            }
            return null;
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
            if (Find(type) != null) throw new ArgumentException("Duplicate type", "type");
            MetaType newType = Create(type);
            if (applyDefaultBehaviour) { newType.ApplyDefaultBehaviour(); }
            lock (types)
            {
                // double checked
                if (Find(type) != null) throw new ArgumentException("Duplicate type", "type");
                ThrowIfFrozen();
                types.Add(newType);
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

        protected override int GetKey(Type type)
        {
            return FindOrAddAuto(type, true);
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
                int pos = source.Position;
                value = ser.Read(Activator.CreateInstance(ser.ExpectedType), source);
                return pos == source.Position ? null : value; // but null if nothing was read
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
        internal class SerializerPair
        {
            public readonly int MetaKey;
            public readonly MethodBuilder Serialize, Deserialize;
            public SerializerPair(int metaKey, MethodBuilder serialize, MethodBuilder deserialize)
            {
                this.MetaKey = metaKey;
                this.Serialize = serialize;
                this.Deserialize = deserialize;
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
            // the keys in the model are guaranteed to be unique, but may not
            // be contiguous (threading etc); we'll normalize the keys
            ILGenerator[] serBodies = new ILGenerator[types.Count],
                deserBodies = new ILGenerator[types.Count];
            BasicList methodPairs = new BasicList();

            int index = 0;
            foreach (MetaType metaType in types)
            {
                MethodBuilder writeMethod = type.DefineMethod("Write",
                    MethodAttributes.Private | MethodAttributes.Static, CallingConventions.Standard,
                    typeof(void), new Type[] { metaType.Type, typeof(ProtoWriter) });
                serBodies[index] = writeMethod.GetILGenerator();

                MethodBuilder readMethod = type.DefineMethod("Read",
                    MethodAttributes.Private | MethodAttributes.Static, CallingConventions.Standard,
                    metaType.Type, new Type[] { metaType.Type, typeof(ProtoReader) });
                deserBodies[index] = readMethod.GetILGenerator();

                methodPairs.Add(new SerializerPair(GetKey(metaType.Type), writeMethod, readMethod));                
                index++;
            }
            index = 0;
            foreach (MetaType metaType in types)
            {
                ctx = new Compiler.CompilerContext(serBodies[index], true, methodPairs);
                metaType.Serializer.EmitWrite(ctx, Compiler.Local.InputValue);
                ctx.Return();

                ctx = new Compiler.CompilerContext(deserBodies[index], true, methodPairs);
                metaType.Serializer.EmitRead(ctx, Compiler.Local.InputValue);
                ctx.LoadValue(Compiler.Local.InputValue);
                ctx.Return();

                index++;
            }

            FieldBuilder knownTypes = type.DefineField("knownTypes", typeof(Type[]), FieldAttributes.Private | FieldAttributes.InitOnly);
            
            ILGenerator il = Override(type, "GetKey");
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, knownTypes);
            il.Emit(OpCodes.Ldarg_1);
            // note that Array.IndexOf is not supported under CF
            il.EmitCall(OpCodes.Callvirt,typeof(IList).GetMethod(
                "IndexOf", new Type[] { typeof(object) }), null);
            il.Emit(OpCodes.Ret);
            
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
                il.MarkLabel(jumpTable[i]);
                il.Emit(OpCodes.Ldarg_2);
                ctx.CastFromObject(((MetaType)types[i]).Type);
                il.Emit(OpCodes.Ldarg_3);
                il.EmitCall(OpCodes.Call, ((SerializerPair)methodPairs[i]).Serialize, null);
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
                il.MarkLabel(jumpTable[i]);
                Type keyType = ((MetaType)types[i]).Type;
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
                    il.EmitCall(OpCodes.Call, ((SerializerPair)methodPairs[i]).Deserialize, null);
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
            foreach(MetaType metaType in types)
            {
                il.Emit(OpCodes.Dup);
                Compiler.CompilerContext.LoadValue(il, index);
                il.Emit(OpCodes.Ldtoken, metaType.Type);
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

        private static MethodBuilder EmitBoxedSerializer(TypeBuilder type, int i, Type valueType, BasicList methodPairs)
        {
            MethodInfo dedicated = ((SerializerPair)methodPairs[i]).Deserialize;
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
            using(Compiler.Local position = new Compiler.Local(ctx, typeof(int)))
            {
                MethodInfo getPos = typeof(ProtoReader).GetProperty("Position").GetGetMethod(ctx.NonPublic);

                ctx.LoadReaderWriter();
                ctx.EmitCall(getPos);
                

                // create a new valueType
                ctx.LoadAddress(typedVal, valueType);
                ctx.EmitCtor(valueType);
                ctx.LoadValue(typedVal);
                ctx.LoadReaderWriter();
                ctx.EmitCall(dedicated);
                ctx.StoreValue(typedVal);

                ctx.LoadReaderWriter();
                ctx.EmitCall(getPos);
                Compiler.CodeLabel noData = ctx.DefineLabel();
                ctx.BranchIfEqual(noData, true);

                ctx.LoadValue(typedVal);
                ctx.CastToObject(valueType);
                ctx.Return();

                ctx.MarkLabel(noData);
                ctx.LoadNullRef();
                ctx.Return();
            }
            return boxedSerializer;
        }
        
#endif

        internal bool IsDefined(Type type, int fieldNumber)
        {
            return Find(type).IsDefined(fieldNumber);
        }
    }
    
}
#endif