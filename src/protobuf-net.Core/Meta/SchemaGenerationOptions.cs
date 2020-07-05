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
