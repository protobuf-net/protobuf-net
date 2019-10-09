using ProtoBuf.Serializers;
using System;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Meta
{
    /// <summary>
    /// A type model that performs per-assembly auto-compilation
    /// </summary>
    public sealed class AutoCompileTypeModel : TypeModel
    {
        /// <summary>
        /// Gets the instance of this serializer
        /// </summary>
        public static TypeModel Instance { get; } = new AutoCompileTypeModel();

        private AutoCompileTypeModel() { }

        [MethodImpl(ProtoReader.HotPath)]
        private TypeModel ForAssembly(Type type)
            => type == null ? NullModel.Instance : RuntimeTypeModel.CreateForAssembly(type.Assembly, null);

        /// <summary>See TypeModel.GetSchema</summary>
        public override string GetSchema(Type type, ProtoSyntax syntax)
            => ForAssembly(type).GetSchema(type, syntax);

        /// <summary>See TypeModel.GetSerializer</summary>
        protected internal override ISerializer<T> GetSerializer<T>()
            => ForAssembly(typeof(T)).GetSerializer<T>();

        internal override bool IsKnownType<T>()
            => ForAssembly(typeof(T)).IsKnownType<T>();
    }
}
