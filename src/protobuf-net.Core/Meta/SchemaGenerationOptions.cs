using ProtoBuf.Internal;
using System;
using System.Collections.Generic;

namespace ProtoBuf.Meta
{
    /// <summary>
    /// Options for controlling schema generation
    /// </summary>
    public sealed class SchemaGenerationOptions
    {
        internal static readonly SchemaGenerationOptions Default = new SchemaGenerationOptions();

        /// <summary>
        /// Create a new instance
        /// </summary>
        public SchemaGenerationOptions() { }

        /// <summary>
        /// Create a new instance, copying the state of an existing instance
        /// </summary>
        /// <remarks>
        /// <para>
        /// The collections are copied by <em>content</em>, not by reference, so the two instances can
        /// be mutated independently - which is the entire point: callers that hold configuration on a
        /// long-lived object need a per-operation copy to add to, and sharing the lists would simply
        /// move the problem.
        /// </para>
        /// <para>
        /// This exists because the alternative is a hand-rolled copy in the caller, which silently
        /// stops being complete the moment a property is added here. <c>SchemaGenerationOptionsTests</c>
        /// guards that by reflecting over the properties rather than trusting this to be updated.
        /// </para>
        /// </remarks>
        /// <param name="source">The instance to copy.</param>
        public SchemaGenerationOptions(SchemaGenerationOptions source)
        {
            if (source is null) ThrowHelper.ThrowArgumentNullException(nameof(source));

            Syntax = source.Syntax;
            Flags = source.Flags;
            Package = source.Package;
            Origin = source.Origin;

            // via the backing fields, so copying an untouched instance does not allocate the lists
            if (source.HasServices) Services.AddRange(source._services);
            if (source.HasTypes) Types.AddRange(source._types);
        }

        /// <summary>
        /// Indiate the variant of the protobuf .proto DSL syntax to use
        /// </summary>
        public ProtoSyntax Syntax { get; set; } = ProtoSyntax.Default;

        /// <summary>
        /// Additional flags to control schema generation
        /// </summary>
        public SchemaGenerationFlags Flags { get; set; }

        /// <summary>
        /// The package to use for generation (<c>null</c> to try to infer)
        /// </summary>
        public string Package { get; set; }

        /// <summary>
        /// The services to consider as part of this operation.
        /// </summary>
        public List<Service> Services => _services ??= new List<Service>();

        /// <summary>
        /// The types to consider as part of this operation.
        /// </summary>
        public List<Type> Types => _types ??= new List<Type>();

        private List<Service> _services;
        private List<Type> _types;

        internal bool HasServices => (_services?.Count ?? 0) != 0;
        internal bool HasTypes => (_types?.Count ?? 0) != 0;

        /// <summary>
        /// The file that defines this type (as used with <c>import</c> in .proto); when non-empty, only
        /// types in the same <c>Origin</c> are included; this option is inferred if <c>null</c>.
        /// </summary>
        public string Origin { get; set; }
    }

    /// <summary>
    /// Additional flags to control schema generation
    /// </summary>
    [Flags]
    public enum SchemaGenerationFlags
    {
        /// <summary>
        /// No additional flags
        /// </summary>
        None = 0,

        /// <summary>
        /// Provide support for extended/multiple namespace details in schemas
        /// </summary>
        MultipleNamespaceSupport = 1 << 0,

        /// <summary>
        /// Record the sub-type relationship formally in schemas
        /// </summary>
        PreserveSubType = 1 << 1,

        /// <summary>
        /// Provides support for adding Prefix to names of Enum members in schemas
        /// </summary>
        IncludeEnumNamePrefix = 1 << 2,
    }


}
