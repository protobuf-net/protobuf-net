#nullable enable

namespace ProtoBuf.Internal.CodeGen;

interface ILocated
{
    object? Origin { get; }
}
interface IDiagnosticSink
{
    void ReportDiagnostic(CodeGenDiagnostic diagnostic, ILocated? source, params object[] messageArgs);
}
internal class CodeGenDiagnostic
{
    public CodeGenDiagnostic(string id, string title, string messageFormat, DiagnosticSeverity severity)
    {
        Id = id;
        Title = title;
        MessageFormat = messageFormat;
        Severity = severity;
    }

    public string Id { get; }
    public string Title { get; }
    public string MessageFormat { get; }
    public DiagnosticSeverity Severity { get; }

    internal static readonly CodeGenDiagnostic FeatureNotImplemented = new(
        id: "PBN4001",
        title: nameof(FeatureNotImplemented),
        messageFormat: "The required '{0}' feature is not yet implemented",
        severity: DiagnosticSeverity.Warning);


    //
    // Summary:
    //     Describes how severe a diagnostic is.
    public enum DiagnosticSeverity
    {
        //
        // Summary:
        //     Something that is an issue, as determined by some authority, but is not surfaced
        //     through normal means. There may be different mechanisms that act on these issues.
        Hidden,
        //
        // Summary:
        //     Information that does not indicate a problem (i.e. not prescriptive).
        Info,
        //
        // Summary:
        //     Something suspicious but allowed.
        Warning,
        //
        // Summary:
        //     Something not allowed by the rules of the language or other authority.
        Error
    }
}
