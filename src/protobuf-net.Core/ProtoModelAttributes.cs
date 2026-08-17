using System;
using ProtoBuf.Internal;
using System.Diagnostics.CodeAnalysis;

namespace ProtoBuf
{
    /// <summary>
    /// Marks a partial <see cref="Meta.TypeModel"/> subclass as a compile-time serialization model,
    /// to be populated by the protobuf-net source generator.
    /// </summary>
    /// <remarks>
    /// The model is closed over what is visible at compile time: it never consults the runtime model,
    /// which is what makes it usable under AOT. A contract the generator cannot handle is omitted with
    /// a diagnostic rather than being guessed at.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    [Experimental(Experiments.CompileTimeModel, UrlFormat = Experiments.UrlFormat)]
    public sealed class ProtoModelAttribute : Attribute
    {
        /// <summary>
        /// The diagnostic reported for use of the compile-time model APIs while they are experimental.
        /// </summary>
        public const string DiagnosticId = Experiments.CompileTimeModel;

        /// <summary>
        /// Whether types with a <c>ToString()</c> and a <c>Parse(string)</c> should be serialized as
        /// strings; the compile-time equivalent of <see cref="Meta.RuntimeTypeModel.AllowParseableTypes"/>.
        /// </summary>
        /// <remarks>
        /// Off by default, matching the runtime model. Turning it on changes the wire form of any
        /// member whose type qualifies, so it must be opted into on both sides or not at all.
        /// </remarks>
        public bool AllowParseableTypes { get; set; }
    }

    /// <summary>
    /// Declares a root type that the associated model can serialize; every contract reachable from a
    /// root is included in the model automatically.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    [Experimental(Experiments.CompileTimeModel, UrlFormat = Experiments.UrlFormat)]
    public sealed class ProtoSerializableAttribute : Attribute
    {
        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="type">The root type to include in the model.</param>
        public ProtoSerializableAttribute(Type type) => Type = type;

        /// <summary>
        /// The root type to include in the model.
        /// </summary>
        public Type Type { get; }
    }
}
