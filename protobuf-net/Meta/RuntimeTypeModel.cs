using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

using ProtoBuf.Serializers;
using System.Diagnostics;

namespace ProtoBuf.Meta
{
    public class RuntimeTypeModel : TypeModel
    {
        private readonly string name;
        public string Name
        {
            get { return name; }
        }
        private class Singleton
        {
            private Singleton() { }
            internal static readonly RuntimeTypeModel Value = new RuntimeTypeModel("DefaultTypeModel", true);
        }
        public static RuntimeTypeModel Default
        {
            get { return Singleton.Value; }
        }
        public IEnumerable GetTypes() { return types; }
        private readonly bool isDefault;
        internal RuntimeTypeModel(string name, bool isDefault)
        {
            if (Helpers.IsNullOrEmpty(name) && !isDefault) throw new ArgumentException("name");
            AutoAddMissingTypes = true;
            this.name = name;
            this.isDefault = isDefault;
        }
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
        int FindOrAddAuto(Type type)
        {
            TypeFinder predicate = new TypeFinder(type);
            int key = types.IndexOf(predicate);
            if (key < 0)
            {
                if (!autoAddMissingTypes) throw new ArgumentException("type");
                MetaType metaType = Create(type);
                metaType.ApplyAttributes();
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
            return new MetaType(type);
        }
        public MetaType Add(Type type, bool applyAttributes)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (Find(type) != null) throw new ArgumentException("Duplicate type", "type");
            MetaType newType = Create(type);
            if (applyAttributes) { newType.ApplyAttributes(); }
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
            return FindOrAddAuto(type);
        }
        protected internal override void Serialize(int key, object value, ProtoWriter dest)
        {
            ((MetaType)types[key]).Serializer.Write(value, dest);
        }
    
        protected internal override object Deserialize(int key, object value, ProtoReader source)
        {
            IProtoSerializer ser = ((MetaType)types[key]).Serializer;
            if (value == null && ser.ExpectedType.IsValueType) {
                int pos = source.Position;
                value = ser.Read(Activator.CreateInstance(ser.ExpectedType), source);
                return pos == source.Position ? null : value; // but null nothing was read
            } else {
                return ser.Read(value, source);
            }
        }
#if FEAT_COMPILER
        internal Compiler.ProtoSerializer GetSerializer(Type type, bool compiled)
        {
            int key = GetKey(type);
            IProtoSerializer ser = ((MetaType)types[key]).Serializer;
            return GetSerializer(ser, compiled);
        }

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
        public TypeModel Compile()
        {
            return Compile(null);
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
        public TypeModel Compile(string path)
        {
            Freeze();
            bool save = !Helpers.IsNullOrEmpty(path);

            AssemblyName an = new AssemblyName();
            an.Name = Name;
            AssemblyBuilder asm = AppDomain.CurrentDomain.DefineDynamicAssembly(an,
                (save ? AssemblyBuilderAccess.RunAndSave : AssemblyBuilderAccess.Run)
                );

            ModuleBuilder module = save ? asm.DefineDynamicModule(Name, path)
                : asm.DefineDynamicModule(Name);
            Type baseType = typeof(TypeModel);
            TypeBuilder type = module.DefineType(Name,
                (baseType.Attributes & ~TypeAttributes.Abstract) | TypeAttributes.Sealed,
                baseType);
            Compiler.CompilerContext ctx;
            // the keys in the model are guaranteed to be unique, but may not
            // be contiguous (threading etc); we'll normalize the keys
            MethodBuilder[] typeSerializers = new MethodBuilder[types.Count],
                typeDeserializers = new MethodBuilder[types.Count];
            ILGenerator[] serBodies = new ILGenerator[types.Count],
                deserBodies = new ILGenerator[types.Count];

            int index = 0;
            foreach (MetaType metaType in types)
            {
                MethodBuilder typeMethod = type.DefineMethod("Write",
                    MethodAttributes.Private | MethodAttributes.Static, CallingConventions.Standard,
                    typeof(void), new Type[] { metaType.Type, typeof(ProtoWriter) });
                typeSerializers[index] = typeMethod;
                serBodies[index] = typeMethod.GetILGenerator();

                typeMethod = type.DefineMethod("Read",
                    MethodAttributes.Private | MethodAttributes.Static, CallingConventions.Standard,
                    metaType.Type, new Type[] { metaType.Type, typeof(ProtoReader) });
                typeDeserializers[index] = typeMethod;
                deserBodies[index] = typeMethod.GetILGenerator();
                
                index++;
            }
            index = 0;
            foreach (MetaType metaType in types)
            {
                ctx = new Compiler.CompilerContext(serBodies[index], true);
                metaType.Serializer.EmitWrite(ctx, Compiler.Local.InputValue);
                ctx.Return();

                ctx = new Compiler.CompilerContext(deserBodies[index], true);
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
            il.EmitCall(OpCodes.Call,typeof(Array).GetMethod("IndexOf",
                new Type[] { typeof(Array), typeof(object) }), null);
            il.Emit(OpCodes.Ret);
            
            il = Override(type, "Serialize");
            ctx = new Compiler.CompilerContext(il, false);
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
                il.EmitCall(OpCodes.Call, typeSerializers[i], null);
                ctx.Return();
            }

            il = Override(type, "Deserialize");
            ctx = new Compiler.CompilerContext(il, false);
            Compiler.Local pos = null;
            try
            {
                // arg0 = this, arg1 = key, arg2=obj, arg3=source
                for (int i = 0; i < jumpTable.Length; i++)
                {
                    jumpTable[i] = il.DefineLabel();
                }
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Switch, jumpTable);
                ctx.LoadNull();
                ctx.Return();
                for (int i = 0; i < jumpTable.Length; i++)
                {
                    il.MarkLabel(jumpTable[i]);
                    Type keyType = ((MetaType)types[i]).Type;
                    if (keyType.IsValueType)
                    {
                        
                        Compiler.CodeLabel ifNull = ctx.DefineLabel();
                        il.Emit(OpCodes.Ldarg_2);
                        ctx.LoadNull();
                        ctx.BranchIfEqual(ifNull);

                        // not null here; unbox and always return
                        il.Emit(OpCodes.Ldarg_2);
                        ctx.CastFromObject(keyType);
                        il.Emit(OpCodes.Ldarg_3);
                        il.EmitCall(OpCodes.Call, typeDeserializers[i], null);
                        ctx.CastToObject(keyType);
                        ctx.Return();

                        
                        ctx.MarkLabel(ifNull);
                        
                        if (pos == null) pos = new Compiler.Local(ctx, typeof(int));
                        using (Compiler.Local typedVar = new Compiler.Local(ctx, keyType))
                        {
                            PropertyInfo readerPos = typeof(ProtoReader).GetProperty("Position");
                            il.Emit(OpCodes.Ldarg_3);
                            ctx.LoadValue(readerPos);
                            ctx.StoreValue(pos);

                            ctx.LoadAddress(typedVar, keyType);
                            ctx.EmitCtor(keyType);
                            ctx.LoadValue(typedVar);
                            il.Emit(OpCodes.Ldarg_3);
                            il.EmitCall(OpCodes.Call, typeDeserializers[i], null);
                            ctx.StoreValue(typedVar);

                            ctx.LoadValue(pos);
                            il.Emit(OpCodes.Ldarg_3);
                            ctx.LoadValue(readerPos);
                            Compiler.CodeLabel noData = ctx.DefineLabel();
                            ctx.BranchIfEqual(noData);
                            // had data, so box and return
                            ctx.LoadValue(typedVar);
                            ctx.CastToObject(keyType);
                            ctx.Return();

                            ctx.MarkLabel(noData);   
                            ctx.LoadNull();
                            ctx.Return();
                        }
                        //ctx.LoadNull();
                        //ctx.Return();
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldarg_2);
                        ctx.CastFromObject(keyType);
                        il.Emit(OpCodes.Ldarg_3);
                        il.EmitCall(OpCodes.Call, typeDeserializers[i], null);
                        ctx.CastToObject(keyType);
                        ctx.Return();
                    }                    
                }
            }
            finally
            {
                if (pos != null) pos.Dispose();
            }

            

            ConstructorBuilder ctor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, Type.EmptyTypes);
            
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
            }

            return (TypeModel)Activator.CreateInstance(finalType);
        }
#endif
    }
}
