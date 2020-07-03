using System;
using System.Collections.Immutable;

namespace ProtoBuf.Meta
{
    /// <summary>
    /// Options for controlling schema generation
    /// </summary>
    public sealed class SchemaGenerationOptions
    {
        /// <summary>
        /// Default options
        /// </summary>
        public static SchemaGenerationOptions Default { get; } = new SchemaGenerationOptions(ProtoSyntax.Default);

        /// <summary>
        /// Indiate the variant of the protobuf .proto DSL syntax to use
        /// </summary>
        public ProtoSyntax Syntax { get; }

        /// <summary>
        /// Additional flags to control schema generation
        /// </summary>
        public SchemaGenerationFlags Flags { get; }

        /// <summary>
        /// The package to use for generation (<c>null</c> to try to infer)
        /// </summary>
        public string Package { get; }

        /// <summary>
        /// The services to consider as part of this operation.
        /// </summary>
        public ImmutableArray<Service> Services { get; }

        /// <summary>
        /// Create a new <see cref="SchemaGenerationOptions"/> instance
        /// </summary>
        public SchemaGenerationOptions(ProtoSyntax syntax, SchemaGenerationFlags flags = default, string package = null, ImmutableArray<Service> services = default)
        {
            Syntax = syntax;
            Flags = flags;
            Package = package;
            Services = services.IsDefault ? ImmutableArray<Service>.Empty : services;
        }

        internal bool HasServices => !Services.IsDefaultOrEmpty;
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
    }


}
