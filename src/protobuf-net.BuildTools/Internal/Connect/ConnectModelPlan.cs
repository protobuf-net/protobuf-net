#nullable enable
using ProtoBuf.BuildTools.Internal.Aot;
using ProtoBuf.BuildTools.Internal.Grpc;
// DiagnosticInfo lives in Internal/Grpc alongside the model it describes
using System;

namespace ProtoBuf.BuildTools.Internal.Connect
{
    /// <summary>
    /// Everything the Connect emitter needs about one <c>[ProtoConnect]</c> container.
    /// </summary>
    /// <remarks>
    /// Subject to the same rule as <c>Internal/Grpc</c> and <c>Internal/Aot</c>: <b>no Roslyn
    /// references</b>. Holding an <c>ISymbol</c> here would make equality reference-based, so the
    /// driver's cache would never hit, and would pin the whole compilation alive for as long as the
    /// driver holds the plan.
    /// <para>
    /// The services are <see cref="GrpcInterfaceModel"/> unchanged, which is the point: that model is
    /// transport-neutral - <c>GrpcMethodKind</c> maps straight onto <c>ConnectMethodType</c> - so the
    /// Connect generator reuses <c>GrpcProxyGenerator.ParseContract</c> rather than forking the shape
    /// analysis. Everything that classification knows about - the context kinds, void/<c>Empty</c>,
    /// <c>[SubService]</c>, overloads, closed generics - arrives here for free.
    /// </para>
    /// </remarks>
    internal sealed class ConnectContainerPlan : IEquatable<ConnectContainerPlan>
    {
        public ConnectContainerPlan(
            string? containerNamespace,
            string containerName,
            string accessibility,
            bool isStatic,
            bool declaresConstructor,
            string? modelTypeFullName,
            EquatableArray<GrpcInterfaceModel> services)
        {
            ContainerNamespace = containerNamespace;
            ContainerName = containerName;
            Accessibility = accessibility;
            IsStatic = isStatic;
            DeclaresConstructor = declaresConstructor;
            ModelTypeFullName = modelTypeFullName;
            Services = services;
        }

        /// <summary>The consumer's namespace, or <c>null</c> for the global one.</summary>
        public string? ContainerNamespace { get; }

        /// <summary>The consumer's type name, which every generated entry point is named after.</summary>
        public string ContainerName { get; }

        /// <summary>
        /// The consumer's declared accessibility, mirrored onto the companion extensions type.
        /// </summary>
        /// <remarks>
        /// The generated <em>partial</em> restates nothing - a partial part may omit the modifier and
        /// the consumer's wins - but the companion <c>{Name}Extensions</c> is a separate type and has
        /// to be told.
        /// </remarks>
        public string Accessibility { get; }

        /// <summary>
        /// Whether the consumer declared the container <c>static</c>.
        /// </summary>
        /// <remarks>
        /// The only thing it changes is the constructor: a static class cannot have one. It does
        /// <em>not</em> decide where the fluent methods live - those are always on the companion
        /// extensions type, so that the fluent form does not depend on how the consumer declared their
        /// half.
        /// </remarks>
        public bool IsStatic { get; }

        /// <summary>
        /// Whether the consumer declared any constructor, in which case none is emitted.
        /// </summary>
        public bool DeclaresConstructor { get; }

        /// <summary>The <c>[ProtoModel]</c>-generated model named by <c>[ProtoConnect(Model = ...)]</c>.</summary>
        public string? ModelTypeFullName { get; }

        /// <summary>The contracts this container declares, in declaration order.</summary>
        public EquatableArray<GrpcInterfaceModel> Services { get; }

        public bool Equals(ConnectContainerPlan? other)
            => other is not null
            && string.Equals(ContainerNamespace, other.ContainerNamespace, StringComparison.Ordinal)
            && string.Equals(ContainerName, other.ContainerName, StringComparison.Ordinal)
            && string.Equals(Accessibility, other.Accessibility, StringComparison.Ordinal)
            && IsStatic == other.IsStatic
            && DeclaresConstructor == other.DeclaresConstructor
            && string.Equals(ModelTypeFullName, other.ModelTypeFullName, StringComparison.Ordinal)
            && Services.Equals(other.Services);

        public override bool Equals(object? obj) => Equals(obj as ConnectContainerPlan);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = ContainerName.GetHashCode();
                hash = (hash * 31) + (ContainerNamespace?.GetHashCode() ?? 0);
                hash = (hash * 31) + (ModelTypeFullName?.GetHashCode() ?? 0);
                hash = (hash * 31) + Services.Count;
                return hash;
            }
        }
    }

    /// <summary>A plan plus whatever the parse wanted to say about it.</summary>
    internal sealed class ConnectCandidate : IEquatable<ConnectCandidate>
    {
        public ConnectCandidate(ConnectContainerPlan? plan, EquatableArray<DiagnosticInfo> diagnostics)
        {
            Plan = plan;
            Diagnostics = diagnostics;
        }

        public ConnectContainerPlan? Plan { get; }

        public EquatableArray<DiagnosticInfo> Diagnostics { get; }

        public bool Equals(ConnectCandidate? other)
            => other is not null
            && Equals(Plan, other.Plan)
            && Diagnostics.Equals(other.Diagnostics);

        public override bool Equals(object? obj) => Equals(obj as ConnectCandidate);

        public override int GetHashCode() => (Plan?.GetHashCode() ?? 0) + Diagnostics.Count;
    }
}
