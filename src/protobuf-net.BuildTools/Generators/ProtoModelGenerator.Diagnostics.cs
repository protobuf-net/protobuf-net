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
            id: "PBN2001",
            title: "Contract omitted: unsupported member",
            messageFormat: "Contract '{0}' is omitted from the AOT model: member '{1}' {2}.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor UnsupportedContract = new(
            id: "PBN2002",
            title: "Contract omitted: unsupported declaration",
            messageFormat: "Contract '{0}' is omitted from the AOT model: {1}.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor UnsupportedOption = new(
            id: "PBN2003",
            title: "Contract omitted: unsupported protobuf-net option",
            messageFormat: "Contract '{0}' is omitted from the AOT model: {1} is not supported yet.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor OmittedCascade = new(
            id: "PBN2004",
            title: "Contract omitted: references an omitted contract",
            messageFormat: "Contract '{0}' is omitted from the AOT model because '{1}', which it references, is also omitted.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private static DiagnosticDescriptor GetDescriptor(ProtoDiagnosticKind kind) => kind switch
        {
            ProtoDiagnosticKind.UnsupportedMember => UnsupportedMember,
            ProtoDiagnosticKind.UnsupportedContract => UnsupportedContract,
            ProtoDiagnosticKind.UnsupportedOption => UnsupportedOption,
            ProtoDiagnosticKind.OmittedCascade => OmittedCascade,
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        private static Diagnostic ToDiagnostic(PlanDiagnostic diagnostic)
            => Diagnostic.Create(GetDescriptor(diagnostic.Kind), diagnostic.Location.ToLocation(),
                diagnostic.ToMessageArgs());
    }
}
