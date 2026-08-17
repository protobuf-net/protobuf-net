#nullable enable
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal.Grpc;

namespace ProtoBuf.BuildTools.Generators
{
    public sealed partial class GrpcProxyGenerator
    {
        private const string Category = "ProtoBuf.Grpc";

        /// <summary>
        /// Turn a cached <see cref="DiagnosticInfo"/> back into a reportable diagnostic.
        /// </summary>
        /// <remarks>
        /// The model stores a <see cref="GrpcDiagnosticKind"/> and a <c>PlanLocation</c> rather than a
        /// descriptor and a <c>Location</c>, so that nothing in it is a Roslyn object; this is where the
        /// two are resolved. Mirrors <c>ProtoModelGenerator.ToDiagnostic</c>.
        /// </remarks>
        private static Diagnostic ToDiagnostic(DiagnosticInfo diagnostic)
            => Diagnostic.Create(GetDescriptor(diagnostic.Kind), diagnostic.Location.ToLocation(),
                diagnostic.ToMessageArgs());

        private static DiagnosticDescriptor GetDescriptor(GrpcDiagnosticKind kind) => kind switch
        {
            GrpcDiagnosticKind.LanguageVersionTooLow => LanguageVersionTooLow,
            GrpcDiagnosticKind.InterfaceMustNotBeNested => InterfaceMustNotBeNested,
            GrpcDiagnosticKind.UnsupportedMethodShape => UnsupportedMethodShape,
            GrpcDiagnosticKind.GenericInterfaceNotSupported => GenericInterfaceNotSupported,
            GrpcDiagnosticKind.UnsupportedBaseInterface => UnsupportedBaseInterface,
            GrpcDiagnosticKind.ModelMustBePartial => ModelMustBePartial,
            GrpcDiagnosticKind.ModelShapeNotSupported => ModelShapeNotSupported,
            GrpcDiagnosticKind.ModelMustDeriveClientFactory => ModelMustDeriveClientFactory,
            GrpcDiagnosticKind.NotAServiceContract => NotAServiceContract,
            GrpcDiagnosticKind.NoOperationsFound => NoOperationsFound,
            GrpcDiagnosticKind.ImplementationDoesNotImplement => ImplementationDoesNotImplement,
            GrpcDiagnosticKind.NoModelNamed => NoModelNamed,
            GrpcDiagnosticKind.ModelIsNotAProtoModel => ModelIsNotAProtoModel,
            GrpcDiagnosticKind.ModelCannotSerializePayload => ModelCannotSerializePayload,
            GrpcDiagnosticKind.UnresolvedContract => UnresolvedContract,
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };

        // PBN4000+ is this generator's block. The register of what is taken is
        // AnalyzerReleases.Unshipped.md - check it before adding an id, and add the id to it.
        // PBN0xxx is DataContractAnalyzer, PBN1xxx ProtoFileGenerator, PBN2xxx ServiceContractAnalyzer
        // (the gRPC *contract* checks), PBN3xxx the AOT serializer generator, PBN9001 the
        // [Experimental] gate.

        /// <summary>
        /// Whether a contract can be given a build-time proxy is a warning, never an error: an
        /// incomplete set of proxies still builds and still works under JIT, exactly as
        /// <c>PBN3001</c>-<c>PBN3004</c> do for the serializer model. Anyone wanting strictness can
        /// escalate with <c>WarningsAsErrors</c>.
        /// </summary>
        internal static readonly DiagnosticDescriptor LanguageVersionTooLow = new(
            id: "PBN4000",
            title: "Language version too low for build-time gRPC proxies",
            messageFormat: "'{0}' was not given build-time gRPC proxies because the language version is "
                + "below C# {1}; the runtime proxy will be used, which is not trim/AOT-friendly",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor InterfaceMustNotBeNested = new(
            id: "PBN4001",
            title: "Service interface cannot be nested",
            messageFormat: "Interface '{0}' is a service contract but is nested inside another type; "
                + "build-time proxies are only generated for top-level interfaces. Move the interface out of '{1}'",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>
        /// One unsupported operation takes the whole contract out.
        /// </summary>
        /// <remarks>
        /// The runtime path handles a wider set of shapes than this generator emits, so a proxy with
        /// some members missing would be a regression rather than a partial win. Same all-or-nothing
        /// reasoning the serializer generator's <c>DropUnsatisfiable</c> cascade uses.
        /// </remarks>
        internal static readonly DiagnosticDescriptor UnsupportedMethodShape = new(
            id: "PBN4002",
            title: "Service method shape is not supported by the generator",
            messageFormat: "Method '{0}.{1}' is not emitted at build time because {2}; the whole contract "
                + "is left to the runtime proxy, which is not trim/AOT-friendly",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor GenericInterfaceNotSupported = new(
            id: "PBN4003",
            title: "Open generic service interfaces are not supported",
            messageFormat: "Interface '{0}' is an open generic type; the generated proxy is a non-generic "
                + "type, so it has nowhere to put the type parameter. Name a closed construction - "
                + "[ProtoService(typeof(IFoo<Bar>))] - which is emitted like any other contract",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>
        /// The runtime binds an inherited interface's operations only when it is marked
        /// <c>[SubService]</c>; for any other base it emits a throwing stub on the client and binds
        /// nothing on the server. Rather than reproduce that, the contract goes to the runtime whole.
        /// </summary>
        internal static readonly DiagnosticDescriptor UnsupportedBaseInterface = new(
            id: "PBN4004",
            title: "Service interface inherits an interface that is not a sub-service",
            messageFormat: "Interface '{0}' inherits '{1}', which is not marked [SubService]; the "
                + "runtime proxy will be used for this contract. Mark the base interface [SubService] "
                + "to have it bound as part of this contract",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ModelMustBePartial = new(
            id: "PBN4005",
            title: "A [ProtoGrpc] type must be partial",
            messageFormat: "'{0}' is marked [ProtoGrpc] but is not declared partial, so there is "
                + "nowhere to generate the proxies; add the 'partial' modifier",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>
        /// The declaration's own shape has to be nameable at namespace level.
        /// </summary>
        /// <remarks>
        /// The generated half is emitted as <c>partial class X</c> directly inside the namespace, so a
        /// nested declaration produced a *stray top-level type* while the consumer's real one was left
        /// without the two <c>ClientFactory</c> members - four compile errors, none of them pointing
        /// here. A generic declaration fails the same way, having nowhere to put the type parameter.
        /// Supporting either means emitting the enclosing chain, which the serializer generator has open
        /// as a TODO for the same reason; refusing clearly comes first.
        /// </remarks>
        internal static readonly DiagnosticDescriptor ModelShapeNotSupported = new(
            id: "PBN4014",
            title: "A [ProtoGrpc] type must be top-level and non-generic",
            messageFormat: "'{0}' is marked [ProtoGrpc] but {1}, and the generated half is emitted as a "
                + "top-level non-generic partial - so it would not compile. Move the declaration to its "
                + "own top-level type",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ModelMustDeriveClientFactory = new(
            id: "PBN4006",
            title: "A [ProtoGrpc] type must derive from ClientFactory",
            messageFormat: "'{0}' is marked [ProtoGrpc] but does not derive from '{1}'; deriving from "
                + "it is what lets 'channel.CreateGrpcService<T>(...)' accept this type",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor NotAServiceContract = new(
            id: "PBN4007",
            title: "Named type is not a service contract",
            messageFormat: "'{0}' was named by [ProtoService] but is not an interface marked [Service] "
                + "or [ServiceContract], so protobuf-net.Grpc would not bind it either",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>
        /// A contract with nothing on it that we recognise as an operation.
        /// </summary>
        /// <remarks>
        /// This was silent until it was fixtured: the contract was dropped as a presumed marker
        /// interface and nothing said so, leaving the consumer with a run-time throw from
        /// <c>CreateClient&lt;T&gt;</c> and no build-time hint. Every other drop reason reports, and
        /// <c>PBN3004</c> exists on the serializer side purely so that a silent cascade is impossible;
        /// this is the same rule. Note it fires only where <em>no</em> operation was recognised and none
        /// was refused - a contract with a bad member reports <c>PBN4002</c> instead, which is more
        /// specific.
        /// </remarks>
        internal static readonly DiagnosticDescriptor NoOperationsFound = new(
            id: "PBN4009",
            title: "Service contract declares no recognised operations",
            messageFormat: "'{0}' was named by [ProtoService] but declares no methods recognised as gRPC "
                + "operations, so no proxy was generated for it and 'CreateClient<{1}>' will throw. "
                + "Properties, events and static members are not operations",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ImplementationDoesNotImplement = new(
            id: "PBN4008",
            title: "Named implementation does not implement the contract",
            messageFormat: "'{0}' was named as the implementation of '{1}' but does not implement it; "
                + "no server bindings were generated for this contract",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>
        /// The one that matters most, and the reason this generator exists in this shape.
        /// </summary>
        /// <remarks>
        /// Generated proxies with a reflective marshaller source is the failure mode that looks fine
        /// until publish: the proxies are static, the payloads are not, so the build succeeds and ILC
        /// is where it falls over. Naming a <c>[ProtoModel]</c> is the whole fix.
        /// </remarks>
        internal static readonly DiagnosticDescriptor NoModelNamed = new(
            id: "PBN4010",
            title: "No AOT serializer model named for these proxies",
            messageFormat: "'{0}' generates gRPC proxies but names no Model, so payloads will be "
                + "marshalled through RuntimeTypeModel.Default, which reflects; the proxies will be "
                + "AOT-safe and the serializers will not. Set [ProtoGrpc(Model = typeof(YourModel))] "
                + "naming a [ProtoModel] type",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>
        /// The named model is in this compilation but is not a compile-time model.
        /// </summary>
        /// <remarks>
        /// Today this fails as a bare <c>CS0117</c> on the generated <c>Model.Instance</c> reference,
        /// which points at generated code and says nothing about the cause. It also means seeding cannot
        /// happen, so every payload would be marshalled reflectively even though the proxies are static.
        /// </remarks>
        internal static readonly DiagnosticDescriptor ModelIsNotAProtoModel = new(
            id: "PBN4012",
            title: "The named serializer model is not marked [ProtoModel]",
            messageFormat: "'{0}' names '{1}' as its serializer model, but that type is declared in this "
                + "project without [ProtoModel], so it has no compile-time serializers and no 'Instance'. "
                + "Add [ProtoModel] to it",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>
        /// The named model lives in another assembly and cannot serialize a payload we need.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Nothing can be added to a model in a referenced assembly, so this is the only direction
        /// available there: check, and say so at build time. Otherwise the mismatch surfaces as a
        /// run-time marshalling failure, or - worse - as a silent fall back to the reflective model that
        /// only fails once published.
        /// </para>
        /// <para>
        /// Decided from the emitted <c>ISerializer&lt;T&gt;</c>/<c>ISerializerProxy&lt;T&gt;</c> set
        /// rather than from the model's <c>[ProtoSerializable]</c> attributes, deliberately: the
        /// attributes are what it was <em>asked</em> to serialize, and the two differ both ways - a
        /// dropped seed keeps its attribute, and a payload reached as a member of another seed never had
        /// one.
        /// </para>
        /// </remarks>
        internal static readonly DiagnosticDescriptor ModelCannotSerializePayload = new(
            id: "PBN4013",
            title: "The named serializer model has no serializer for a payload type",
            messageFormat: "'{0}' needs to marshal '{1}', but the model '{2}' - which is in another "
                + "assembly, so nothing can be added to it here - has no serializer for that type. Add "
                + "[ProtoSerializable(typeof({1}))] to it, or name a model that covers this contract",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor UnresolvedContract = new(
            id: "PBN4011",
            title: "Service contract could not be resolved",
            messageFormat: "The type named '{0}' could not be resolved. If it is generated by another "
                + "source generator in this same project, move it to a referenced project: generators "
                + "all run against the same input compilation and never see each other's output",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
    }
}
