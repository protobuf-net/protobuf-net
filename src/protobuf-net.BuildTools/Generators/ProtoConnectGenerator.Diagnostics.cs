#nullable enable
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal.Grpc;

namespace ProtoBuf.BuildTools.Generators
{
    partial class ProtoConnectGenerator
    {
        private const string Category = "ProtoBuf.Connect";

        /// <summary>
        /// Maps a shared-parse diagnostic onto this generator's own descriptor.
        /// </summary>
        /// <remarks>
        /// The kinds come from <see cref="GrpcDiagnosticKind"/>, which is a gRPC name for a
        /// transport-neutral idea - each member says what is wrong with a <em>contract</em>, not with a
        /// transport - so reusing them beats a parallel enum that would have to be kept in step. What is
        /// not shared is the descriptors: ids and wording are per-generator, which is the whole reason
        /// the parse stopped asserting that a runtime fallback exists.
        /// </remarks>
        private static Diagnostic ToDiagnostic(DiagnosticInfo info)
            => Diagnostic.Create(Describe(info.Kind), info.Location.ToLocation(), info.ToMessageArgs());

        private static DiagnosticDescriptor Describe(GrpcDiagnosticKind kind) => kind switch
        {
            GrpcDiagnosticKind.LanguageVersionTooLow => LanguageVersionTooLow,
            GrpcDiagnosticKind.UnsupportedMethodShape => UnsupportedMethodShape,
            GrpcDiagnosticKind.NotAServiceContract => NotAServiceContract,
            GrpcDiagnosticKind.NoOperationsFound => NoOperationsFound,
            GrpcDiagnosticKind.ImplementationDoesNotImplement => ImplementationDoesNotImplement,
            GrpcDiagnosticKind.GenericInterfaceNotSupported => GenericInterfaceNotSupported,
            _ => UnsupportedContract,
        };

        internal static readonly DiagnosticDescriptor LanguageVersionTooLow = new(
            id: "PBN5000",
            title: "Language version too low for build-time Connect proxies",
            messageFormat: "'{0}' was not given build-time Connect proxies because the language version "
                + "is below C# 12; set <LangVersion>12.0</LangVersion> or higher",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor UnsupportedMethodShape = new(
            id: "PBN5001",
            title: "Service method shape is not supported by the Connect generator",
            messageFormat: "Method '{0}.{1}' is not emitted because {2}; the whole contract is left out, "
                + "and calling it will throw at run time",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor NotAServiceContract = new(
            id: "PBN5002",
            title: "Type named by [ProtoService] is not a service contract",
            messageFormat: "'{0}' was named by [ProtoService] but is not a service contract; mark it "
                + "[Service] or [ServiceContract]",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor NoOperationsFound = new(
            id: "PBN5003",
            title: "Service contract declares no recognised operations",
            messageFormat: "'{0}' was named by [ProtoService] but declares no methods recognised as "
                + "operations, so nothing is emitted for it",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ImplementationDoesNotImplement = new(
            id: "PBN5004",
            title: "Implementation named by [ProtoService] does not implement the contract",
            messageFormat: "'{0}' was named as the implementation of '{1}' but does not implement it, "
                + "so no server bindings are emitted",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor GenericInterfaceNotSupported = new(
            id: "PBN5005",
            title: "Open generic service contract is not supported",
            messageFormat: "'{0}' is an open generic contract, so there is no one request or response "
                + "type to build a method descriptor from",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor UnsupportedContract = new(
            id: "PBN5006",
            title: "Service contract is not supported by the Connect generator",
            messageFormat: "'{0}' was not emitted: {1}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
    }
}
