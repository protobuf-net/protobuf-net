// Members that upstream descriptor.proto has since *removed* (the field numbers are reserved
// there), but which shipped in protobuf-net.Reflection's public API and so are kept for binary
// compatibility. They live here rather than in Descriptor.cs so that file stays purely the
// output of regeneration (see google/README.md for the procedure).

#pragma warning disable CS0612, CS0618, CS1591, CS3021, CS8981, IDE0079, IDE1006, RCS1036, RCS1057, RCS1085, RCS1192
namespace Google.Protobuf.Reflection
{
    public partial class FileOptions
    {
        // removed in protobuf 26.x; `reserved 42; reserved "php_generic_services";` upstream
        [global::ProtoBuf.ProtoMember(42, Name = @"php_generic_services")]
        [global::System.ComponentModel.DefaultValue(false)]
        [global::System.Obsolete("php_generic_services has been removed from descriptor.proto; modern protoc rejects it")]
        public bool PhpGenericServices
        {
            get => __pbn__PhpGenericServices ?? false;
            set => __pbn__PhpGenericServices = value;
        }
        [global::System.Obsolete("php_generic_services has been removed from descriptor.proto; modern protoc rejects it")]
        public bool ShouldSerializePhpGenericServices() => __pbn__PhpGenericServices != null;
        [global::System.Obsolete("php_generic_services has been removed from descriptor.proto; modern protoc rejects it")]
        public void ResetPhpGenericServices() => __pbn__PhpGenericServices = null;
        private bool? __pbn__PhpGenericServices;
    }
}
#pragma warning restore CS0612, CS0618, CS1591, CS3021, CS8981, IDE0079, IDE1006, RCS1036, RCS1057, RCS1085, RCS1192
