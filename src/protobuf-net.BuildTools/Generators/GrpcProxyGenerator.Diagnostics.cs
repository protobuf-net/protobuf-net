#nullable enable
using Microsoft.CodeAnalysis;

namespace ProtoBuf.BuildTools.Generators
{
    public sealed partial class GrpcProxyGenerator
    {
        private const string Category = "ProtoBuf.Grpc";

        /// <summary>
        /// The generated code is nullable-annotated, so it cannot be emitted below C# 8; the runtime
        /// (ref-emit) path still applies, so this is informational rather than an error.
        /// </summary>
        internal static readonly DiagnosticDescriptor LanguageVersionTooLow = new(
            id: "PBN3000",
            title: "Language version too low for build-time gRPC proxies",
            messageFormat: "Service contract '{0}' was not given a build-time proxy because the language version is below C# {1}; the runtime proxy will be used, which is not trim/AOT-friendly",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor InterfaceMustNotBeNested = new(
            id: "PBN3001",
            title: "Service interface cannot be nested",
            messageFormat: "Interface '{0}' is a service contract but is nested inside another type; build-time proxies are only generated for top-level interfaces. Move the interface out of '{1}'",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor UnsupportedMethodShape = new(
            id: "PBN3002",
            title: "Service method shape is not supported by the generator",
            messageFormat: "Method '{0}.{1}' has a signature that is not emitted at build time; the runtime proxy will be used for this contract, which is not trim/AOT-friendly",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor GenericInterfaceNotSupported = new(
            id: "PBN3003",
            title: "Generic service interfaces are not supported",
            messageFormat: "Interface '{0}' is generic; build-time proxies are not generated for generic service contracts",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>
        /// Registration happens from a <c>[ModuleInitializer]</c>, which is net5.0+ unless the consumer
        /// declares the attribute themselves.
        /// </summary>
        internal static readonly DiagnosticDescriptor ModuleInitializerUnavailable = new(
            id: "PBN3004",
            title: "Target framework cannot support build-time gRPC proxies",
            messageFormat: "Service contract '{0}' was not given a build-time proxy because [ModuleInitializer] is not available on this target framework; the runtime proxy will be used, which is not trim/AOT-friendly",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);
    }
}
