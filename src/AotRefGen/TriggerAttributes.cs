using System;

namespace ProtoBuf
{
    // Mirrors the attributes that ProtoModelGenerator emits via RegisterPostInitializationOutput.
    // The fixtures are shared source with the generator tests, so this tool has to supply the same
    // trigger attributes in order to compile them; keep the two in step. If these ever move into
    // protobuf-net.Core, delete this file rather than leaving an ambiguous duplicate.

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal sealed class ProtoModelAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    internal sealed class ProtoSerializableAttribute : Attribute
    {
        public ProtoSerializableAttribute(Type type) => Type = type;

        public Type Type { get; }
    }
}
