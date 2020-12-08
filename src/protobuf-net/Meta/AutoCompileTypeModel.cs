using ProtoBuf.Internal;
using ProtoBuf.Serializers;
using System;
using System.Collections;
using System.Linq;
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
            if (type is null) ThrowHelper.ThrowArgumentNullException(nameof(type));
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
            if (assembly is null) ThrowHelper.ThrowArgumentNullException(nameof(assembly));
            if (options is null)
            {
                var obj = (TypeModel)s_assemblyModels[assembly];
                if (obj is object) return obj;
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
        private static TypeModel ForAssembly(Type type)
            => type is null ? NullModel.Singleton : CreateForAssembly(type.Assembly, null);

        /// <inheritdoc/>
        public override string GetSchema(SchemaGenerationOptions options)
            => ForAssembly(options.HasTypes ? options.Types.First() : null).GetSchema(options);

        /// <inheritdoc/>
        protected override ISerializer<T> GetSerializer<T>()
            => ForAssembly(typeof(T)).GetSerializerCore<T>(default);

        internal override bool IsKnownType<T>(CompatibilityLevel ambient)
            => ForAssembly(typeof(T)).IsKnownType<T>(ambient);


        private static TypeModel CreateForAssemblyImpl(Assembly assembly, CompilerOptions options)
        {
            if (assembly is null) throw new ArgumentNullException(nameof(assembly));
            lock (assembly)
            {
                var found = (TypeModel)s_assemblyModels[assembly];
                if (found is object) return found;

                RuntimeTypeModel model = null;
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsGenericTypeDefinition) continue;
                    if (!IsFullyPublic(type)) continue;
                    if (!type.IsDefined(typeof(ProtoContractAttribute), true)) continue;

                    if (options is object && !options.OnIncludeType(type)) continue;

                    (model ??= RuntimeTypeModel.Create()).Add(type, true);
                }
                if (model is null)
                    throw new InvalidOperationException($"No types marked [ProtoContract] found in assembly '{assembly.GetName().Name}'");
                var compiled = model.Compile(options);
                s_assemblyModels[assembly] = compiled;
                return compiled;
            }
        }
    }
}
