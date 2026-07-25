#nullable enable
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
    internal sealed class GrpcInterfaceModel : IEquatable<GrpcInterfaceModel>
    {
        public GrpcInterfaceModel(
            string interfaceFullName,
            string serviceName,
            string proxyTypeName,
            string serverBindingsTypeName,
            string initTypeName,
            ImmutableArray<GrpcOperationModel> operations)
        {
            InterfaceFullName = interfaceFullName;
            ServiceName = serviceName;
            ProxyTypeName = proxyTypeName;
            ServerBindingsTypeName = serverBindingsTypeName;
            InitTypeName = initTypeName;
            Operations = operations;
        }

        /// <summary>The contract interface, as <c>global::Ns.IFoo</c>.</summary>
        public string InterfaceFullName { get; }

        /// <summary>The logical gRPC service name.</summary>
        public string ServiceName { get; }

        /// <summary>Generated client class name, in the <c>ProtoBuf.Grpc.Generated</c> namespace.</summary>
        public string ProxyTypeName { get; }

        /// <summary>Generated server-bindings class name, in the <c>ProtoBuf.Grpc.Generated</c> namespace.</summary>
        public string ServerBindingsTypeName { get; }

        /// <summary>Generated module-initializer class name, in the <c>ProtoBuf.Grpc.Generated</c> namespace.</summary>
        public string InitTypeName { get; }

        public ImmutableArray<GrpcOperationModel> Operations { get; }

        public bool Equals(GrpcInterfaceModel? other)
        {
            if (other is null) return false;
            if (!string.Equals(InterfaceFullName, other.InterfaceFullName, StringComparison.Ordinal)
                || !string.Equals(ServiceName, other.ServiceName, StringComparison.Ordinal)
                || !string.Equals(ProxyTypeName, other.ProxyTypeName, StringComparison.Ordinal)
                || !string.Equals(ServerBindingsTypeName, other.ServerBindingsTypeName, StringComparison.Ordinal)
                || !string.Equals(InitTypeName, other.InitTypeName, StringComparison.Ordinal)
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
    /// The result of inspecting one candidate interface: a model to emit, diagnostics to report, or both.
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
    /// A diagnostic reduced to value-comparable data.
    /// </summary>
    /// <remarks>
    /// <see cref="Diagnostic"/> holds a <see cref="Location"/>, which roots a <see cref="SyntaxTree"/>;
    /// keeping one in the incremental model would both defeat caching and retain compilations, so the
    /// location is flattened to a span and rehydrated at report time.
    /// </remarks>
    internal sealed class DiagnosticInfo : IEquatable<DiagnosticInfo>
    {
        public DiagnosticInfo(DiagnosticDescriptor descriptor, Location? location, params string[] messageArgs)
        {
            Descriptor = descriptor;
            Location = location is null || location.Kind != LocationKind.SourceFile
                ? null
                : Location.Create(location.SourceTree!.FilePath, location.SourceSpan, location.GetLineSpan().Span);
            MessageArgs = messageArgs;
        }

        public DiagnosticDescriptor Descriptor { get; }

        public Location? Location { get; }

        public string[] MessageArgs { get; }

        public Diagnostic ToDiagnostic() => Diagnostic.Create(Descriptor, Location, MessageArgs);

        public bool Equals(DiagnosticInfo? other)
        {
            if (other is null || !ReferenceEquals(Descriptor, other.Descriptor)) return false;
            if (Location != other.Location) return false;
            if (MessageArgs.Length != other.MessageArgs.Length) return false;
            for (int i = 0; i < MessageArgs.Length; i++)
            {
                if (!string.Equals(MessageArgs[i], other.MessageArgs[i], StringComparison.Ordinal)) return false;
            }
            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as DiagnosticInfo);

        public override int GetHashCode() => Descriptor.Id.GetHashCode() + MessageArgs.Length;
    }
}
