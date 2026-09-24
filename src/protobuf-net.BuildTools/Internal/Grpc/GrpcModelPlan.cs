#nullable enable
using System;
using System.Collections.Immutable;

namespace ProtoBuf.BuildTools.Internal.Grpc
{
    /// <summary>
    /// Everything needed to fill in one consumer-declared
    /// <c>[ProtoGrpc] partial class X : ClientFactory</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is where the approach differs from a registry-based one. The trigger is a type the
    /// consumer declares, and every generated artefact hangs off it <em>by name</em>: the proxies are
    /// nested inside it, the client dispatch is a <c>typeof</c> chain in its <c>CreateClient</c>
    /// override, and the server bindings are nested <c>IServiceMethodProvider&lt;TImpl&gt;</c>
    /// implementations that the generated registration extension adds by hand.
    /// </para>
    /// <para>
    /// Nothing resolves by <see cref="Type"/> at run time, which is the property that survives ILC -
    /// and it is the same shape <c>[ProtoModel]</c> already uses for serializers, so a consumer meets
    /// one idea rather than two.
    /// </para>
    /// </remarks>
    internal sealed class GrpcModelPlan : IEquatable<GrpcModelPlan>
    {
        public GrpcModelPlan(
            string? namespaceName,
            string typeName,
            string? modelTypeFullName,
            bool isSealed,
            bool emitInstance,
            bool emitConstructor,
            string registrationMethodName,
            ImmutableArray<GrpcInterfaceModel> contracts,
            bool downLevel)
        {
            NamespaceName = namespaceName;
            TypeName = typeName;
            ModelTypeFullName = modelTypeFullName;
            IsSealed = isSealed;
            EmitInstance = emitInstance;
            EmitConstructor = emitConstructor;
            RegistrationMethodName = registrationMethodName;
            Contracts = contracts;
            DownLevel = downLevel;
        }

        /// <summary>The namespace the consumer declared their partial in, or null for the global one.</summary>
        public string? NamespaceName { get; }

        public string TypeName { get; }

        /// <summary>
        /// The <c>[ProtoModel]</c>-generated <c>TypeModel</c> named by <c>Model = typeof(...)</c>.
        /// </summary>
        /// <remarks>
        /// This link is what makes end-to-end AOT possible at all. Without it the marshallers come
        /// from <c>BinderConfiguration.Default</c>, i.e. <c>RuntimeTypeModel.Default</c>, and the
        /// payloads are still built by reflection however static the proxies are - which is the gap
        /// that neither of the source PRs closes.
        /// </remarks>
        public string? ModelTypeFullName { get; }

        public bool IsSealed { get; }

        /// <summary>Whether to emit <c>Instance</c>; suppressed if the consumer declared their own.</summary>
        public bool EmitInstance { get; }

        /// <summary>Whether to emit a non-public parameterless constructor.</summary>
        public bool EmitConstructor { get; }

        /// <summary>The name of the generated <c>IServiceCollection</c> extension, e.g. <c>AddMyServices</c>.</summary>
        public string RegistrationMethodName { get; }

        public ImmutableArray<GrpcInterfaceModel> Contracts { get; }

        /// <summary>
        /// Whether the consumer's language version is below the floor, so only the down-level shape
        /// can be emitted: the two <c>ClientFactory</c> members, with the client half delegating to
        /// the reflective runtime factory. See <c>DownLevelPlan</c> for why this is not "emit nothing".
        /// </summary>
        public bool DownLevel { get; }

        public bool Equals(GrpcModelPlan? other)
        {
            if (other is null) return false;
            if (!string.Equals(NamespaceName, other.NamespaceName, StringComparison.Ordinal)
                || !string.Equals(TypeName, other.TypeName, StringComparison.Ordinal)
                || !string.Equals(ModelTypeFullName, other.ModelTypeFullName, StringComparison.Ordinal)
                || IsSealed != other.IsSealed
                || EmitInstance != other.EmitInstance
                || EmitConstructor != other.EmitConstructor
                || !string.Equals(RegistrationMethodName, other.RegistrationMethodName, StringComparison.Ordinal)
                || DownLevel != other.DownLevel
                || Contracts.Length != other.Contracts.Length)
            {
                return false;
            }
            for (int i = 0; i < Contracts.Length; i++)
            {
                if (!Contracts[i].Equals(other.Contracts[i])) return false;
            }
            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as GrpcModelPlan);

        public override int GetHashCode()
        {
            var hash = StringComparer.Ordinal.GetHashCode(TypeName);
            return (hash * -1521134295) + Contracts.Length;
        }
    }

    /// <summary>
    /// The result of inspecting one <c>[ProtoGrpc]</c> declaration: a plan to emit, diagnostics to
    /// report, or both.
    /// </summary>
    internal sealed class GrpcModelCandidate : IEquatable<GrpcModelCandidate>
    {
        public GrpcModelCandidate(GrpcModelPlan? plan, ImmutableArray<DiagnosticInfo> diagnostics,
            ImmutableArray<string> droppedContracts = default, Aot.PlanLocation declaration = default)
        {
            Plan = plan;
            Diagnostics = diagnostics;
            DroppedContracts = droppedContracts.IsDefault ? ImmutableArray<string>.Empty : droppedContracts;
            Declaration = declaration;
        }

        public GrpcModelPlan? Plan { get; }

        public ImmutableArray<DiagnosticInfo> Diagnostics { get; }

        /// <summary>
        /// Contracts named by <c>[ProtoService]</c> that got no proxy, whatever the reason.
        /// </summary>
        /// <remarks>
        /// Carried separately from the drop diagnostics because it answers a different question. Each drop
        /// already says *why* it happened, and at the default severity that is the right amount of noise -
        /// under JIT the runtime path really does take over. But when the project asks for AOT there is no
        /// runtime path, so the same drop stops being a degradation and becomes a contract that will throw;
        /// that needs saying separately, because it depends on a build property the parse cannot see.
        /// </remarks>
        public ImmutableArray<string> DroppedContracts { get; }

        /// <summary>
        /// Where the <c>[ProtoGrpc]</c> declaration is, for diagnostics that belong on it rather than on a
        /// contract. Plain span data, not a <c>Location</c>, for the reason the whole model follows.
        /// </summary>
        public Aot.PlanLocation Declaration { get; }

        public bool Equals(GrpcModelCandidate? other)
        {
            if (other is null) return false;
            if (Plan is null != other.Plan is null) return false;
            if (Plan is not null && !Plan.Equals(other.Plan)) return false;
            if (Diagnostics.Length != other.Diagnostics.Length) return false;
            if (DroppedContracts.Length != other.DroppedContracts.Length) return false;
            if (!Declaration.Equals(other.Declaration)) return false;
            for (int i = 0; i < DroppedContracts.Length; i++)
            {
                if (!string.Equals(DroppedContracts[i], other.DroppedContracts[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }
            for (int i = 0; i < Diagnostics.Length; i++)
            {
                if (!Diagnostics[i].Equals(other.Diagnostics[i])) return false;
            }
            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as GrpcModelCandidate);

        public override int GetHashCode() => (Plan?.GetHashCode() ?? 0) + Diagnostics.Length;
    }
}
