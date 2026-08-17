using System;
using ProtoBuf.Internal;
using System.Diagnostics.CodeAnalysis;

namespace ProtoBuf
{
    /// <summary>
    /// Declares that one type is serialized by way of another, for types that cannot carry
    /// <see cref="ProtoContractAttribute.Surrogate"/> themselves — a BCL type, or anything else
    /// you do not own.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the compile-time equivalent of <c>RuntimeTypeModel.SetSurrogate</c>, and is read by
    /// the protobuf-net source generator; it has no effect on the reflection-based model.
    /// </para>
    /// <para>
    /// Apply it to a generated model to configure that model alone, or to an <b>assembly</b> to
    /// offer the pairing to every model that references it — which is how a library ships surrogates
    /// for the types it supports, without each consumer restating them. A model's own declaration
    /// wins over one it merely references.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    [Experimental(Experiments.CompileTimeModel, UrlFormat = Experiments.UrlFormat)]
    public sealed class ProtoSurrogateAttribute : Attribute
    {
        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="type">The type being serialized.</param>
        /// <param name="surrogate">The type that carries its wire shape.</param>
        public ProtoSurrogateAttribute(Type type, Type surrogate)
        {
            Type = type;
            Surrogate = surrogate;
        }

        /// <summary>
        /// The type being serialized.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// The type that carries its wire shape.
        /// </summary>
        public Type Surrogate { get; }

        /// <summary>
        /// A type declaring the public static conversion methods named by <see cref="ToSurrogate"/>
        /// and <see cref="ToType"/>. When omitted, a cast is used in both directions, which covers
        /// the usual conversion operators.
        /// </summary>
        public Type Converter { get; set; }

        /// <summary>
        /// The <see cref="Converter"/> method converting <see cref="Type"/> to <see cref="Surrogate"/>.
        /// </summary>
        public string ToSurrogate { get; set; }

        /// <summary>
        /// The <see cref="Converter"/> method converting <see cref="Surrogate"/> back to <see cref="Type"/>.
        /// </summary>
        public string ToType { get; set; }
    }
}
