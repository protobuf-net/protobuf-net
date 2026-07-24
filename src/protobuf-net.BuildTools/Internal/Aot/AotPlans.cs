#nullable enable
using System;

namespace ProtoBuf.BuildTools.Internal.Aot
{
    /// <summary>
    /// The kinds of member the AOT generator can currently emit. Anything not listed here causes
    /// the owning contract to be omitted from the model rather than guessed at.
    /// </summary>
    internal enum ProtoMemberKind
    {
        Int32,
        String,

        /// <summary>A nested contract, served by another serializer on the same services type.</summary>
        Message,
    }

    /// <summary>
    /// One serialized member of a contract.
    /// </summary>
    internal readonly struct ProtoMemberPlan : IEquatable<ProtoMemberPlan>
    {
        public ProtoMemberPlan(int fieldNumber, string name, ProtoMemberKind kind, string? typeName = null)
        {
            FieldNumber = fieldNumber;
            Name = name;
            Kind = kind;
            TypeName = typeName;
        }

        public int FieldNumber { get; }

        /// <summary>The C# member name on the contract type.</summary>
        public string Name { get; }

        public ProtoMemberKind Kind { get; }

        /// <summary>
        /// For <see cref="ProtoMemberKind.Message"/>, the fully-qualified type of the nested
        /// contract; null otherwise.
        /// </summary>
        public string? TypeName { get; }

        public bool Equals(ProtoMemberPlan other)
            => FieldNumber == other.FieldNumber && Kind == other.Kind
                && Name == other.Name && TypeName == other.TypeName;

        public override bool Equals(object? obj) => obj is ProtoMemberPlan other && Equals(other);

        public override int GetHashCode()
            => (FieldNumber * 397) ^ ((int)Kind * 31) ^ Name.GetHashCode() ^ (TypeName?.GetHashCode() ?? 0);
    }

    /// <summary>
    /// One contract type that the model can serialize.
    /// </summary>
    internal sealed class ProtoContractPlan : IEquatable<ProtoContractPlan>
    {
        public ProtoContractPlan(string typeName, EquatableArray<ProtoMemberPlan> members)
        {
            TypeName = typeName;
            Members = members;
        }

        /// <summary>Fully-qualified, <c>global::</c>-prefixed type name.</summary>
        public string TypeName { get; }

        public EquatableArray<ProtoMemberPlan> Members { get; }

        public bool Equals(ProtoContractPlan? other)
            => other is not null && TypeName == other.TypeName && Members.Equals(other.Members);

        public override bool Equals(object? obj) => Equals(obj as ProtoContractPlan);

        public override int GetHashCode() => (TypeName.GetHashCode() * 397) ^ Members.GetHashCode();
    }

    /// <summary>
    /// A user-declared <c>[ProtoModel]</c> type and everything it serializes.
    /// </summary>
    internal sealed class ProtoModelPlan : IEquatable<ProtoModelPlan>
    {
        public ProtoModelPlan(string? nameSpace, string typeName, EquatableArray<ProtoContractPlan> contracts)
        {
            Namespace = nameSpace;
            TypeName = typeName;
            Contracts = contracts;
        }

        /// <summary>Null for the global namespace.</summary>
        public string? Namespace { get; }

        /// <summary>The simple name of the user's partial model class.</summary>
        public string TypeName { get; }

        public EquatableArray<ProtoContractPlan> Contracts { get; }

        public string HintName
            => (Namespace is null ? TypeName : Namespace + "." + TypeName) + ".ProtoModel.g.cs";

        public bool Equals(ProtoModelPlan? other)
            => other is not null && Namespace == other.Namespace && TypeName == other.TypeName
                && Contracts.Equals(other.Contracts);

        public override bool Equals(object? obj) => Equals(obj as ProtoModelPlan);

        public override int GetHashCode()
            => ((Namespace?.GetHashCode() ?? 0) * 397) ^ (TypeName.GetHashCode() * 31) ^ Contracts.GetHashCode();
    }
}
