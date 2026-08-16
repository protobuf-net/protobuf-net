#nullable enable
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal.Aot;
using System;

namespace ProtoBuf.BuildTools.Generators
{
    partial class ProtoModelGenerator
    {
        private const string Category = "ProtoBuf";

        // Warning, not Error: an omitted contract leaves the model incomplete, and TypeModel's
        // inherited "no serializer for type X" throw is the runtime backstop. Failing the build
        // instead would make the generator unusable while its coverage is still partial; anyone
        // wanting strictness can escalate these through WarningsAsErrors.

        internal static readonly DiagnosticDescriptor UnsupportedMember = new(
            id: "PBN3001",
            title: "Contract omitted: unsupported member",
            messageFormat: "Contract '{0}' is omitted from the AOT model: member '{1}' {2}.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor UnsupportedContract = new(
            id: "PBN3002",
            title: "Contract omitted: unsupported declaration",
            messageFormat: "Contract '{0}' is omitted from the AOT model: {1}.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor UnsupportedOption = new(
            id: "PBN3003",
            title: "Contract omitted: unsupported protobuf-net option",
            messageFormat: "Contract '{0}' is omitted from the AOT model: {1} is not supported yet.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor OmittedCascade = new(
            id: "PBN3004",
            title: "Contract omitted: references an omitted contract",
            messageFormat: "Contract '{0}' is omitted from the AOT model because '{1}', which it references, is also omitted.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        // the [ProtoSchema] block. These are about the DECLARATION rather than about a contract,
        // so they are anchored on the attribute and say what was available instead - which a
        // typeof() of a not-yet-generated type could never do

        internal static readonly DiagnosticDescriptor SchemaNotFound = new(
            id: "PBN3020",
            title: "Schema not found",
            messageFormat: "No additional file matches the schema '{0}' ({1}); add it to <AdditionalFiles>.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor SchemaAmbiguous = new(
            id: "PBN3021",
            title: "Schema is ambiguous",
            messageFormat: "The schema '{0}' matches more than one additional file ({1}); include enough of the path to identify one.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor SchemaInvalid = new(
            id: "PBN3022",
            title: "Schema could not be parsed",
            messageFormat: "The schema '{0}' could not be parsed: {1}.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor SchemaUnsupported = new(
            id: "PBN3023",
            title: "Schema shape not supported",
            messageFormat: "The schema '{0}' is omitted from the AOT model: {1}.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private static DiagnosticDescriptor GetDescriptor(ProtoDiagnosticKind kind) => kind switch
        {
            ProtoDiagnosticKind.UnsupportedMember => UnsupportedMember,
            ProtoDiagnosticKind.UnsupportedContract => UnsupportedContract,
            ProtoDiagnosticKind.UnsupportedOption => UnsupportedOption,
            ProtoDiagnosticKind.OmittedCascade => OmittedCascade,
            ProtoDiagnosticKind.SchemaNotFound => SchemaNotFound,
            ProtoDiagnosticKind.SchemaAmbiguous => SchemaAmbiguous,
            ProtoDiagnosticKind.SchemaInvalid => SchemaInvalid,
            ProtoDiagnosticKind.SchemaUnsupported => SchemaUnsupported,
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        private static Diagnostic ToDiagnostic(PlanDiagnostic diagnostic)
            => Diagnostic.Create(GetDescriptor(diagnostic.Kind), diagnostic.Location.ToLocation(),
                diagnostic.ToMessageArgs());
    }
}
