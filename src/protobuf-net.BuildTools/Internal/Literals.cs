#nullable enable
namespace ProtoBuf.BuildTools.Internal
{
    internal static class Literals
    {
        public const string CategoryUsage = "Usage";

        public const string AdditionalFileMetadataPrefix = "build_metadata.AdditionalFiles.";

        /// <summary>
        /// One switch that turns off everything protobuf-net contributes at build time.
        /// </summary>
        /// <remarks>
        /// Exists so that shipping the tooling by default is cheap to decline: a consumer who does not
        /// want any of it sets <c>&lt;ProtoBufDisableBuildTools&gt;true&lt;/ProtoBufDisableBuildTools&gt;</c>
        /// and every analyzer and generator returns before touching a symbol, so the cost of being
        /// installed-but-unwanted is one property lookup rather than a walk of the compilation.
        /// </remarks>
        public const string DisableProperty = "build_property.ProtoBufDisableBuildTools";
    }
}
