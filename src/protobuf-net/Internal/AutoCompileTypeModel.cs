using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Internal
{
    /// <summary>
    /// A type model that performs per-assembly auto-compilation
    /// </summary>
    internal sealed class AutoCompileTypeModel : TypeModel
    {
        /// <summary>
        /// Gets the instance of this serializer
        /// </summary>
        public static TypeModel Instance { get; } = new AutoCompileTypeModel();

        private AutoCompileTypeModel() { }

        [MethodImpl(ProtoReader.HotPath)]
        TypeModel ForAssembly(Type type)
            => type == null ? NullModel.Instance : RuntimeTypeModel.CreateForAssembly(type.Assembly, null);

        public override string GetSchema(Type type, ProtoSyntax syntax)
            => ForAssembly(type).GetSchema(type, syntax);

        protected internal override ISerializer<T> GetSerializer<T>()
            => ForAssembly(typeof(T)).GetSerializer<T>();

        internal override bool IsKnownType<T>()
            => ForAssembly(typeof(T)).IsKnownType<T>();
    }
}
