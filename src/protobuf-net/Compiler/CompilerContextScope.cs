using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Compiler
{
    internal sealed class CompilerContextScope
    {
        internal static CompilerContextScope CreateInProcess()
        {
            return new CompilerContextScope(null, true);
        }

        internal static CompilerContextScope CreateForModule(ModuleBuilder module)
            => new CompilerContextScope(module, false);

        private CompilerContextScope(ModuleBuilder module, bool inProcess)
        {
            _module = module;
            _inProcess = inProcess;
        }

        private Dictionary<object, FieldInfo> _additionalSerializers;
        private ModuleBuilder _module;

        private ModuleBuilder GetModule()
            => _module ?? (_module = GetSharedModule());

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ModuleBuilder GetSharedModule() => SharedModule.Shared;
        static class SharedModule
        {
            internal static readonly ModuleBuilder Shared
                = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(nameof(SharedModule)), AssemblyBuilderAccess.Run)
                    .DefineDynamicModule(nameof(SharedModule));
        }

        private readonly bool _inProcess;
        internal bool TryGetAdditionalSerializerInstance(object key, out FieldInfo field)
        {
            field = null;
            return _additionalSerializers != null && _additionalSerializers.TryGetValue(key, out field);
        }

        internal FieldInfo DefineAdditionalSerializerInstance<T>(object key, Action<object, ILGenerator> serialize, Action<object, ILGenerator> deserialize)
        {
            if (_additionalSerializers == null) _additionalSerializers = new Dictionary<object, FieldInfo>();
            if (_additionalSerializers.ContainsKey(key)) throw new ArgumentException(nameof(key));

            const string InstanceFieldName = "Instance";
            var module = GetModule();
            lock (module)
            {
                var type = module.DefineType(typeof(T).Name + "_" + _additionalSerializers.Count,
                    TypeAttributes.NotPublic | TypeAttributes.Class | TypeAttributes.Sealed);

                var ctor = type.DefineDefaultConstructor(MethodAttributes.Private);
                var instance = type.DefineField(InstanceFieldName, type, FieldAttributes.Assembly | FieldAttributes.Static | FieldAttributes.InitOnly);
                var il = type.DefineTypeInitializer().GetILGenerator();
                il.Emit(OpCodes.Newobj, ctor);
                il.Emit(OpCodes.Stsfld, instance);
                il.Emit(OpCodes.Ret);

                var iType = typeof(IProtoSerializer<T, T>);
                type.AddInterfaceImplementation(iType);
                il = Implement(type, iType, nameof(IProtoSerializer<T, T>.Serialize));
                serialize(key, il);
                il.Emit(OpCodes.Ret);

                il = Implement(type, iType, nameof(IProtoSerializer<T, T>.Deserialize));
                deserialize(key, il);
                il.Emit(OpCodes.Ret);

                Type finalType = type.CreateTypeInfo();
                var result = finalType.GetField(InstanceFieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                // _inProcess ? : instance;
                _additionalSerializers.Add(key, result);
                return result;
            }
        }

        private static ILGenerator Implement(TypeBuilder type, Type interfaceType, string name)
        {
            var decl = interfaceType.GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
            if (decl == null) throw new ArgumentException(nameof(name));
            var args = decl.GetParameters();
            var method = type.DefineMethod(name, (decl.Attributes & ~MethodAttributes.Abstract) | MethodAttributes.Final,
                decl.ReturnType, Array.ConvertAll(args, x => x.ParameterType));
            for (int i = 0; i < args.Length; i++)
                method.DefineParameter(i + 1, args[i].Attributes, args[i].Name);
            type.DefineMethodOverride(method, decl);
            return method.GetILGenerator();
        }
    }
}
