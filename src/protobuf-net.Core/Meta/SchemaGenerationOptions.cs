using System;

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

        internal bool MultipleNamespaceSupport => (Flags & SchemaGenerationFlags.MultipleNamespaceSupport) != 0;

        /// <summary>
        /// Create a new <see cref="SchemaGenerationOptions"/> instance
        /// </summary>
        public SchemaGenerationOptions(ProtoSyntax syntax, SchemaGenerationFlags flags = default)
        {
            Syntax = syntax;
            Flags = flags;
        }
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
    }


}
