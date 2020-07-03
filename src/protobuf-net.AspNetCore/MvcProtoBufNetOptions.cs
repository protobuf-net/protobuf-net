using ProtoBuf.Meta;

namespace ProtoBuf.AspNetCore
{
    /// <summary>
    /// Options for configuring protobuf-net usage inside ASP.NET Core
    /// </summary>
    public sealed class MvcProtoBufNetOptions
    {
        /// <summary>
        /// The type-model to use for serialization and deserialization; if omitted, the default model is assumed
        /// </summary>
        public TypeModel Model { get; set; }

        /// <summary>
        /// The maximum length of payloads to write; no limit by default
        /// </summary>
        public long WriteMaxLength { get; set; } = -1;

        /// <summary>
        /// The amount of memory to use for in-memory buffering if needed
        /// </summary>
        public int ReadMemoryBufferThreshold { get; set; } = 256 * 1024; // kinda arbitrary
    }
}
