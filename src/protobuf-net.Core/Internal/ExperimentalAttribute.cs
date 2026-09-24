#if !NET8_0_OR_GREATER
// ExperimentalAttribute arrived in .NET 8; below that we declare our own so that the public API can
// be annotated uniformly across every target. It is internal, so it never collides with the real one
// and never becomes part of our surface — the compiler only honours it on net8.0+ anyway, which is
// where the warning that matters is produced.
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class
        | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor
        | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field
        | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate,
        Inherited = false)]
    internal sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute(string diagnosticId) => DiagnosticId = diagnosticId;

        public string DiagnosticId { get; }

        public string UrlFormat { get; set; }
    }
}
#endif
