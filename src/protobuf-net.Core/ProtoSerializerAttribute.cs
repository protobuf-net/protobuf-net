using System;
using System.Diagnostics.CodeAnalysis;

namespace ProtoBuf
{
    /// <summary>
    /// Declares that a type is serialized by a hand-written serializer, for types that cannot carry
    /// <see cref="ProtoContractAttribute.Serializer"/> themselves — a BCL type, or anything else you
    /// do not own or cannot couple to protobuf-net.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the compile-time equivalent of <c>MetaType.SerializerType</c>, and is read by the
    /// protobuf-net source generator; it has no effect on the reflection-based model.
    /// </para>
    /// <para>
    /// Apply it to a generated model to configure that model alone, or to an <b>assembly</b> to
    /// offer the pairing to every model that references it — which is how a library ships
    /// serializers for the types it supports, without each consumer restating them. A model's own
    /// declaration wins over one it merely references, and over the type's own
    /// <see cref="ProtoContractAttribute.Serializer"/>; an assembly's does not.
    /// </para>
    /// <para>
    /// <see cref="Type"/> and <see cref="Serializer"/> may both be open generic definitions of the
    /// same arity, in which case the serializer is closed with the type arguments of each use site;
    /// a closed declaration wins over the open mapping for that one type.
    /// </para>
    /// <para>
    /// An open serializer's own generic constraints are <b>not</b> validated against each use site at
    /// compile time. If a use-site type argument violates a constraint the serializer itself declares
    /// (e.g. <c>where T : class</c>), the generator still closes the mapping and emits a reference the
    /// consumer's build will fail to compile — a real, if rare, class of error. A consumer declaring an
    /// open serializer with constraints should either keep the serializer unconstrained (protobuf-net's
    /// own de-facto policy) or verify by hand that every reachable instantiation satisfies them.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    [Experimental(ProtoModelAttribute.DiagnosticId)]
    public sealed class ProtoSerializerAttribute : Attribute
    {
        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="type">The type being serialized.</param>
        /// <param name="serializer">The hand-written serializer that carries its wire shape.</param>
        public ProtoSerializerAttribute(Type type, Type serializer)
        {
            Type = type;
            Serializer = serializer;
        }

        /// <summary>
        /// The type being serialized.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// The hand-written serializer; a concrete class with a parameterless constructor,
        /// implementing <c>ISerializer&lt;T&gt;</c> for <see cref="Type"/>.
        /// </summary>
        public Type Serializer { get; }

        /// <summary>
        /// States the serializer's category outright — the only route that survives into metadata,
        /// and so the only one available when the serializer lives in a compiled reference. Setting
        /// it to <c>false</c> is an explicit message-category declaration, distinct from omitting it
        /// (which defers the framing to the serializer's own <c>Features</c>).
        /// </summary>
        public bool IsScalar { get; set; }
    }
}
