#nullable enable
// Portions of this file - the operation/parameter shape models and DiagnosticInfo - are adapted from
// https://github.com/protobuf-net/protobuf-net/pull/1255 by Victor Irzak (@virzak), which in turn
// moved them from https://github.com/protobuf-net/protobuf-net.Grpc/pull/364. The shape
// classification there is good and is kept close to verbatim; what changed is how the generator is
// *triggered* and what it emits - see GrpcProxyGenerator for that reasoning.
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;

namespace ProtoBuf.BuildTools.Internal.Grpc
{
    /// <summary>
    /// The gRPC method shape implied by the combination of an operation's request and response.
    /// </summary>
    internal enum GrpcMethodKind
    {
        Unary,
        ServerStreaming,
        ClientStreaming,
        DuplexStreaming,
    }

    /// <summary>
    /// How (if at all) the operation accepts per-call context.
    /// </summary>
    internal enum GrpcContextKind
    {
        None,
        CallContext,
        CancellationToken,
    }

    /// <summary>
    /// The wrapper (if any) around an operation's response payload.
    /// </summary>
    internal enum GrpcResultShape
    {
        Sync,        // T or void
        Task,        // Task or Task<T>
        ValueTask,   // ValueTask or ValueTask<T>
        AsyncEnumerable,
    }

    /// <summary>
    /// The wrapper (if any) around an operation's request payload.
    /// </summary>
    internal enum GrpcArgShape
    {
        Data,            // a single data parameter
        Void,            // no data parameter
        AsyncEnumerable, // streaming input
    }

    /// <summary>
    /// A single parameter on a service-contract method.
    /// </summary>
    /// <remarks>
    /// Equality is by value throughout this model, so that the incremental generator's caching
    /// actually holds between unrelated edits; this is why these are hand-written classes rather
    /// than records (records compare <see cref="ImmutableArray{T}"/> members by reference, and
    /// positional records need an <c>IsExternalInit</c> polyfill on netstandard2.0).
    /// </remarks>
    internal sealed class GrpcParameterModel : IEquatable<GrpcParameterModel>
    {
        public GrpcParameterModel(string name, string typeDisplay)
        {
            Name = name;
            TypeDisplay = typeDisplay;
        }

        public string Name { get; }

        public string TypeDisplay { get; }

        public bool Equals(GrpcParameterModel? other)
            => other is not null
            && string.Equals(Name, other.Name, StringComparison.Ordinal)
            && string.Equals(TypeDisplay, other.TypeDisplay, StringComparison.Ordinal);

        public override bool Equals(object? obj) => Equals(obj as GrpcParameterModel);

        public override int GetHashCode()
        {
            var hash = StringComparer.Ordinal.GetHashCode(Name);
            return (hash * -1521134295) + StringComparer.Ordinal.GetHashCode(TypeDisplay);
        }
    }

    /// <summary>
    /// One operation (method) on a service contract, reduced to the facts the emitter needs.
    /// </summary>
    internal sealed class GrpcOperationModel : IEquatable<GrpcOperationModel>
    {
        public GrpcOperationModel(
            string operationName,
            string methodName,
            string declaringInterfaceFullName,
            GrpcMethodKind kind,
            GrpcContextKind context,
            GrpcArgShape requestShape,
            GrpcResultShape responseShape,
            string requestTypeFullName,
            string responseTypeFullName,
            bool voidRequest,
            bool voidResponse,
            string returnTypeDisplay,
            ImmutableArray<GrpcParameterModel> parameters)
        {
            OperationName = operationName;
            MethodName = methodName;
            DeclaringInterfaceFullName = declaringInterfaceFullName;
            Kind = kind;
            Context = context;
            RequestShape = requestShape;
            ResponseShape = responseShape;
            RequestTypeFullName = requestTypeFullName;
            ResponseTypeFullName = responseTypeFullName;
            VoidRequest = voidRequest;
            VoidResponse = voidResponse;
            ReturnTypeDisplay = returnTypeDisplay;
            Parameters = parameters;
        }

        /// <summary>The logical gRPC operation name (after <c>[Operation]</c> / trailing-Async handling).</summary>
        public string OperationName { get; }

        /// <summary>The CLR method name to implement / dispatch to.</summary>
        public string MethodName { get; }

        /// <summary>
        /// The interface that <em>declares</em> this operation, which is not the contract itself when
        /// the contract inherits it.
        /// </summary>
        /// <remarks>
        /// An explicit interface implementation must name the declaring interface, and the server-side
        /// metadata lookup does <c>contractType.GetMethod(name)</c>, which does not search base
        /// interfaces - so inherited operations need this on both sides.
        /// </remarks>
        public string DeclaringInterfaceFullName { get; }

        public GrpcMethodKind Kind { get; }

        public GrpcContextKind Context { get; }

        public GrpcArgShape RequestShape { get; }

        public GrpcResultShape ResponseShape { get; }

        /// <summary>Fully-qualified request payload type (<c>Empty</c> when <see cref="VoidRequest"/>).</summary>
        public string RequestTypeFullName { get; }

        /// <summary>Fully-qualified response payload type (<c>Empty</c> when <see cref="VoidResponse"/>).</summary>
        public string ResponseTypeFullName { get; }

        public bool VoidRequest { get; }

        public bool VoidResponse { get; }

        /// <summary>The declared return type, as written, for the explicit interface implementation.</summary>
        public string ReturnTypeDisplay { get; }

        public ImmutableArray<GrpcParameterModel> Parameters { get; }

        public bool Equals(GrpcOperationModel? other)
        {
            if (other is null) return false;
            if (!string.Equals(OperationName, other.OperationName, StringComparison.Ordinal)
                || !string.Equals(MethodName, other.MethodName, StringComparison.Ordinal)
                || !string.Equals(DeclaringInterfaceFullName, other.DeclaringInterfaceFullName, StringComparison.Ordinal)
                || Kind != other.Kind
                || Context != other.Context
                || RequestShape != other.RequestShape
                || ResponseShape != other.ResponseShape
                || !string.Equals(RequestTypeFullName, other.RequestTypeFullName, StringComparison.Ordinal)
                || !string.Equals(ResponseTypeFullName, other.ResponseTypeFullName, StringComparison.Ordinal)
                || VoidRequest != other.VoidRequest
                || VoidResponse != other.VoidResponse
                || !string.Equals(ReturnTypeDisplay, other.ReturnTypeDisplay, StringComparison.Ordinal)
                || Parameters.Length != other.Parameters.Length)
            {
                return false;
            }
            for (int i = 0; i < Parameters.Length; i++)
            {
                if (!Parameters[i].Equals(other.Parameters[i])) return false;
            }
            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as GrpcOperationModel);

        public override int GetHashCode()
        {
            var hash = StringComparer.Ordinal.GetHashCode(MethodName);
            hash = (hash * -1521134295) + StringComparer.Ordinal.GetHashCode(OperationName);
            hash = (hash * -1521134295) + StringComparer.Ordinal.GetHashCode(DeclaringInterfaceFullName);
            hash = (hash * -1521134295) + (int)Kind;
            hash = (hash * -1521134295) + (int)Context;
            hash = (hash * -1521134295) + (int)RequestShape;
            hash = (hash * -1521134295) + (int)ResponseShape;
            hash = (hash * -1521134295) + StringComparer.Ordinal.GetHashCode(RequestTypeFullName);
            hash = (hash * -1521134295) + StringComparer.Ordinal.GetHashCode(ResponseTypeFullName);
            hash = (hash * -1521134295) + Parameters.Length;
            return hash;
        }
    }


    /// <summary>
    /// One service contract, reduced to the facts the emitter needs.
    /// </summary>
    /// <remarks>
    /// Unlike the equivalent in the source PR there is no <c>InitTypeName</c>: nothing is registered
    /// from a <c>[ModuleInitializer]</c>, because nothing is looked up at run time. The proxy is a
    /// nested type of the consumer's declared partial, reached by an ordinary <c>typeof</c> test.
    /// </remarks>
    internal sealed class GrpcInterfaceModel : IEquatable<GrpcInterfaceModel>
    {
        public GrpcInterfaceModel(
            string interfaceFullName,
            string serviceName,
            string proxyTypeName,
            string providerTypeName,
            string? implementationTypeFullName,
            ImmutableArray<GrpcOperationModel> operations,
            bool implementsDisposable,
            bool implementsAsyncDisposable)
        {
            InterfaceFullName = interfaceFullName;
            ServiceName = serviceName;
            ProxyTypeName = proxyTypeName;
            ProviderTypeName = providerTypeName;
            ImplementationTypeFullName = implementationTypeFullName;
            Operations = operations;
            ImplementsDisposable = implementsDisposable;
            ImplementsAsyncDisposable = implementsAsyncDisposable;
        }

        /// <summary>The contract interface, as <c>global::Ns.IFoo</c>.</summary>
        public string InterfaceFullName { get; }

        /// <summary>The logical gRPC service name.</summary>
        public string ServiceName { get; }

        /// <summary>Generated client proxy class name, nested inside the consumer's partial.</summary>
        public string ProxyTypeName { get; }

        /// <summary>Generated <c>IServiceMethodProvider&lt;TImpl&gt;</c> class name, nested likewise.</summary>
        public string ProviderTypeName { get; }

        /// <summary>
        /// The implementation type named by <c>[ProtoService(..., typeof(Impl))]</c>, if any.
        /// </summary>
        /// <remarks>
        /// This is what lets the server half close its generics at compile time.
        /// <c>IServiceMethodProvider&lt;TService&gt;</c> is generic in the <em>implementation</em>, so
        /// with no implementation named there is nothing to instantiate it with - which is why the
        /// source PR had to reach for <c>MakeGenericMethod(typeof(TService))</c>, suppressing IL3050
        /// and IL2060 to do it. Naming the implementation removes that reflective step; a client-only
        /// project omits it and simply gets no server bindings.
        /// </remarks>
        public string? ImplementationTypeFullName { get; }

        public ImmutableArray<GrpcOperationModel> Operations { get; }

        /// <summary>
        /// Whether the contract inherits <see cref="IDisposable"/>, which the proxy has to implement
        /// (as a no-op, matching the runtime) but must never bind as an operation.
        /// </summary>
        public bool ImplementsDisposable { get; }

        /// <summary>
        /// Whether the contract inherits <c>IAsyncDisposable</c>; as with <see cref="ImplementsDisposable"/>.
        /// </summary>
        public bool ImplementsAsyncDisposable { get; }

        public bool Equals(GrpcInterfaceModel? other)
        {
            if (other is null) return false;
            if (!string.Equals(InterfaceFullName, other.InterfaceFullName, StringComparison.Ordinal)
                || !string.Equals(ServiceName, other.ServiceName, StringComparison.Ordinal)
                || !string.Equals(ProxyTypeName, other.ProxyTypeName, StringComparison.Ordinal)
                || !string.Equals(ProviderTypeName, other.ProviderTypeName, StringComparison.Ordinal)
                || !string.Equals(ImplementationTypeFullName, other.ImplementationTypeFullName, StringComparison.Ordinal)
                || ImplementsDisposable != other.ImplementsDisposable
                || ImplementsAsyncDisposable != other.ImplementsAsyncDisposable
                || Operations.Length != other.Operations.Length)
            {
                return false;
            }
            for (int i = 0; i < Operations.Length; i++)
            {
                if (!Operations[i].Equals(other.Operations[i])) return false;
            }
            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as GrpcInterfaceModel);

        public override int GetHashCode()
        {
            var hash = StringComparer.Ordinal.GetHashCode(InterfaceFullName);
            hash = (hash * -1521134295) + StringComparer.Ordinal.GetHashCode(ServiceName);
            return (hash * -1521134295) + Operations.Length;
        }
    }

    /// <summary>
    /// The result of inspecting one candidate contract: a model to emit, diagnostics to report, or both.
    /// </summary>
    internal sealed class GrpcContractCandidate : IEquatable<GrpcContractCandidate>
    {
        public GrpcContractCandidate(GrpcInterfaceModel? model, ImmutableArray<DiagnosticInfo> diagnostics)
        {
            Model = model;
            Diagnostics = diagnostics;
        }

        public GrpcInterfaceModel? Model { get; }

        public ImmutableArray<DiagnosticInfo> Diagnostics { get; }

        public bool Equals(GrpcContractCandidate? other)
        {
            if (other is null) return false;
            if (Model is null != other.Model is null) return false;
            if (Model is not null && !Model.Equals(other.Model)) return false;
            if (Diagnostics.Length != other.Diagnostics.Length) return false;
            for (int i = 0; i < Diagnostics.Length; i++)
            {
                if (!Diagnostics[i].Equals(other.Diagnostics[i])) return false;
            }
            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as GrpcContractCandidate);

        public override int GetHashCode() => (Model?.GetHashCode() ?? 0) + Diagnostics.Length;
    }

    /// <summary>
    /// Why a contract, or a whole <c>[ProtoGrpc]</c> declaration, was left to the runtime path.
    /// </summary>
    /// <remarks>
    /// A <em>kind</em> rather than a <see cref="DiagnosticDescriptor"/>, so that nothing in the cached
    /// model is a Roslyn object at all; the descriptor is looked up at report time. Same shape as the
    /// serializer generator's <c>ProtoDiagnosticKind</c>, and the names line up with the descriptor
    /// fields on the generator so the mapping needs no table to read.
    /// </remarks>
    internal enum GrpcDiagnosticKind
    {
        LanguageVersionTooLow,
        InterfaceMustNotBeNested,
        UnsupportedMethodShape,
        GenericInterfaceNotSupported,
        UnsupportedBaseInterface,
        ModelMustBePartial,
        ModelMustDeriveClientFactory,
        NotAServiceContract,
        NoOperationsFound,
        ImplementationDoesNotImplement,
        NoModelNamed,
        ModelIsNotAProtoModel,
        ModelCannotSerializePayload,
        UnresolvedContract,
    }

    /// <summary>
    /// A diagnostic reduced to value-comparable data.
    /// </summary>
    /// <remarks>
    /// Neither a <see cref="Diagnostic"/> nor a <see cref="Location"/> can live in an incremental
    /// model: a location roots a <see cref="SyntaxTree"/>, so it never compares equal across runs and
    /// keeps the whole compilation alive. <c>PlanLocation</c> is the plain-data stand-in, shared with
    /// the serializer generator's model rather than duplicated.
    /// </remarks>
    internal sealed class DiagnosticInfo : IEquatable<DiagnosticInfo>
    {
        public DiagnosticInfo(GrpcDiagnosticKind kind, Location? location, params string[] messageArgs)
        {
            Kind = kind;
            Location = Aot.PlanLocation.From(location);
            MessageArgs = messageArgs;
        }

        public GrpcDiagnosticKind Kind { get; }

        public Aot.PlanLocation Location { get; }

        public string[] MessageArgs { get; }

        public object[] ToMessageArgs()
        {
            var result = new object[MessageArgs.Length];
            for (int i = 0; i < result.Length; i++) result[i] = MessageArgs[i];
            return result;
        }

        public bool Equals(DiagnosticInfo? other)
        {
            if (other is null || Kind != other.Kind) return false;
            if (!Location.Equals(other.Location)) return false;
            if (MessageArgs.Length != other.MessageArgs.Length) return false;
            for (int i = 0; i < MessageArgs.Length; i++)
            {
                if (!string.Equals(MessageArgs[i], other.MessageArgs[i], StringComparison.Ordinal)) return false;
            }
            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as DiagnosticInfo);

        public override int GetHashCode() => (int)Kind + MessageArgs.Length;
    }
}
