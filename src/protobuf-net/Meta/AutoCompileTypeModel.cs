using ProtoBuf.Internal;
using ProtoBuf.Serializers;
using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using static ProtoBuf.Meta.RuntimeTypeModel;

namespace ProtoBuf.Meta
{
    /// <summary>
    /// A type model that performs per-assembly auto-compilation
    /// </summary>
    public sealed class AutoCompileTypeModel : TypeModel
    {
        /// <summary>
        /// Create a model that serializes all types from an
        /// assembly specified by type
        /// </summary>
        public static new TypeModel CreateForAssembly<T>()
            => CreateForAssembly(typeof(T).Assembly, null);

        /// <summary>
        /// Create a model that serializes all types from an
        /// assembly specified by type
        /// </summary>
        public static new TypeModel CreateForAssembly(Type type)
        {
            if (type == null) ThrowHelper.ThrowArgumentNullException(nameof(type));
            return CreateForAssembly(type.Assembly, null);
        }

        /// <summary>
        /// Create a model that serializes all types from an assembly
        /// </summary>
        public static new TypeModel CreateForAssembly(Assembly assembly)
            => CreateForAssembly(assembly, null);

        /// <summary>
        /// Create a model that serializes all types from an assembly
        /// </summary>
        public static TypeModel CreateForAssembly(Assembly assembly, CompilerOptions options)
        {
            if (assembly == null) ThrowHelper.ThrowArgumentNullException(nameof(assembly));
            if (options == null)
            {
                var obj = (TypeModel)s_assemblyModels[assembly];
                if (obj != null) return obj;
            }
            return CreateForAssemblyImpl(assembly, options);
        }

        private readonly static Hashtable s_assemblyModels = new Hashtable();

        /// <summary>
        /// Gets the instance of this serializer
        /// </summary>
        public static TypeModel Instance { get; } = new AutoCompileTypeModel();

        private AutoCompileTypeModel() { }

        [MethodImpl(ProtoReader.HotPath)]
        private TypeModel ForAssembly(Type type)
            => type == null ? NullModel.Singleton : CreateForAssembly(type.Assembly, null);

        /// <summary>See TypeModel.GetSchema</summary>
        public override string GetSchema(Type type, ProtoSyntax syntax)
            => ForAssembly(type).GetSchema(type, syntax);

        /// <summary>See TypeModel.GetSerializer</summary>
        protected override ISerializer<T> GetSerializer<T>()
            => ForAssembly(typeof(T)).GetSerializerCore<T>(default);

        internal override bool IsKnownType<T>(CompatibilityLevel ambient)
            => ForAssembly(typeof(T)).IsKnownType<T>(ambient);


        private static TypeModel CreateForAssemblyImpl(Assembly assembly, CompilerOptions options)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            lock (assembly)
            {
                var found = (TypeModel)s_assemblyModels[assembly];
                if (found != null) return found;

                RuntimeTypeModel model = null;
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsGenericTypeDefinition) continue;
                    if (!IsFullyPublic(type)) continue;
                    if (!type.IsDefined(typeof(ProtoContractAttribute), true)) continue;

                    if (options != null && !options.OnIncludeType(type)) continue;

                    (model ??= RuntimeTypeModel.Create()).Add(type, true);
                }
                if (model == null)
                    throw new InvalidOperationException($"No types marked [ProtoContract] found in assembly '{assembly.GetName().Name}'");
                var compiled = model.Compile(options);
                s_assemblyModels[assembly] = compiled;
                return compiled;
            }
        }
    }
}
