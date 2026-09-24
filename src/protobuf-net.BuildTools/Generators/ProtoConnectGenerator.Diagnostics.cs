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
            GrpcDiagnosticKind.MetadataNotConstructible => MetadataNotConstructible,
            GrpcDiagnosticKind.NoSideEffectsNotUnary => NoSideEffectsNotUnary,
            GrpcDiagnosticKind.InertHttpMethodAttribute => InertHttpMethodAttribute,
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

        /// <summary>
        /// Endpoint metadata could not be reconstructed, so the endpoint carries none.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The gRPC generator's equivalent, <c>PBN4019</c>, says the operation "keeps the reflective
        /// metadata lookup" - and that is a genuine fallback, so it is close to harmless. There is no
        /// such fallback here: <c>MapConnectService</c> deliberately does not reflect, which is what
        /// makes it AOT-safe, so the metadata this generator does not construct simply does not exist.
        /// </para>
        /// <para>
        /// Which makes this the <em>same failure</em> <c>PBN5007</c> exists for, arrived at from the
        /// other direction - a more permissive endpoint with no error anywhere - and it is worded to say
        /// what actually happens rather than to describe a fallback that is not there.
        /// </para>
        /// <para>
        /// Reported per operation, and the metadata for that operation is dropped <em>whole</em>: the
        /// shared parse gives up on the first attribute it cannot render rather than emitting a partial
        /// list, because a short list is a more permissive endpoint and nothing would notice.
        /// </para>
        /// </remarks>
        internal static readonly DiagnosticDescriptor MetadataNotConstructible = new(
            id: "PBN5008",
            title: "Endpoint metadata could not be reconstructed, so the endpoint carries none",
            messageFormat: "'{0}.{1}' is bound without its endpoint metadata because {2}; nothing on "
                + "this path reflects, so an authorization attribute would not be honoured. Chain "
                + ".RequireAuthorization(...) on the returned builder, or make the attribute "
                + "constructible from this assembly",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>
        /// <c>[NoSideEffects]</c> on an operation that cannot be served over <c>GET</c>.
        /// </summary>
        /// <remarks>
        /// Connect GET is a unary-only form - there is no way to carry a stream in a query string - so
        /// the attribute has nothing to act on anywhere else and would otherwise be silently inert.
        /// Reported from this generator rather than from the shared parse, because a gRPC-only project
        /// that happens to carry the attribute is not doing anything wrong.
        /// </remarks>
        internal static readonly DiagnosticDescriptor NoSideEffectsNotUnary = new(
            id: "PBN5009",
            title: "[NoSideEffects] is only meaningful on a unary operation",
            messageFormat: "'{0}.{1}' is marked [NoSideEffects] but is {2}, and Connect GET is unary "
                + "only - the attribute has no effect here",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>
        /// An ASP.NET Core MVC verb attribute on a Connect contract method, where it does nothing.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Reported rather than honoured, and the reasons are worth keeping because <c>[HttpGet]</c> is
        /// a perfectly reasonable thing for a .NET developer to reach for:
        /// </para>
        /// <list type="bullet">
        /// <item><description>
        /// a contract assembly usually <b>cannot see it</b> - MVC lives in the
        /// <c>Microsoft.AspNetCore.App</c> shared framework, and a transport-neutral contract shared with
        /// a client has no reason to take a framework reference;
        /// </description></item>
        /// <item><description>
        /// it means something <b>narrower and exclusive</b> - "this action answers the GET verb" - where
        /// Connect GET binds <b>GET and POST</b>, since a client that cannot use GET must still be able
        /// to POST;
        /// </description></item>
        /// <item><description>
        /// it has <b>no schema representation</b>, so the property would not survive into the
        /// <c>.proto</c> and would be invisible to a client in another language - where
        /// <c>[NoSideEffects]</c> mirrors <c>option idempotency_level = NO_SIDE_EFFECTS;</c>.
        /// </description></item>
        /// </list>
        /// <para>
        /// So the value here is purely discoverability: point the consumer at the attribute that does
        /// work, rather than leaving theirs silently inert.
        /// </para>
        /// <para>
        /// <b>It is anchored on the contract rather than on the offending method</b>, as <c>PBN5009</c>
        /// is, and that is deliberate rather than unfinished: a per-operation location would have to
        /// ride on <c>GrpcOperationModel</c>, which is the cached <em>plan</em>, and a location shifts
        /// whenever anything above it moves - so the emit step would stop being cached across edits that
        /// only move code around. The message names the method instead. Don't "fix" it by putting a
        /// location on the plan.
        /// </para>
        /// </remarks>
        internal static readonly DiagnosticDescriptor InertHttpMethodAttribute = new(
            id: "PBN5010",
            title: "ASP.NET Core MVC verb attributes do nothing on a Connect contract",
            messageFormat: "'{0}.{1}' carries [{2}], which has no effect here - a Connect method's path "
                + "and verbs come from the protocol, not from MVC routing. To make a unary method "
                + "servable over GET, mark it [NoSideEffects]",
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
