#nullable enable
using System;

namespace ProtoBuf.BuildTools.Internal.Aot
{
    /// <summary>
    /// One member of an enum, as canonical JSON needs it.
    /// </summary>
    /// <remarks>
    /// Canonical protobuf JSON writes an enum as its <em>name</em>, which the binary path has no use
    /// for at all - <see cref="ProtoEnumPlan"/> carries only the underlying scalar, because that is
    /// the whole of what goes on the wire. So this is new information rather than a reshaping of
    /// what was already gathered.
    /// </remarks>
    internal readonly struct ProtoJsonEnumMember : IEquatable<ProtoJsonEnumMember>
    {
        public ProtoJsonEnumMember(string csharpName, string jsonName, long value)
        {
            CSharpName = csharpName;
            JsonName = jsonName;
            Value = value;
        }

        /// <summary>The member's name in C#, for the <c>case</c> label.</summary>
        public string CSharpName { get; }

        /// <summary>
        /// The name on the wire - <c>[ProtoEnum(Name = ...)]</c> where declared, else the C# name.
        /// </summary>
        /// <remarks>
        /// <c>[ProtoEnum]</c> is documented as renaming "for schema purposes", which was true while
        /// the schema was never an interop contract. JSON makes it one: this name is what a peer
        /// generated from our <c>.proto</c> will send and expect.
        /// </remarks>
        public string JsonName { get; }

        /// <summary>
        /// The constant, widened to <see cref="long"/> - <c>unchecked</c> for a <c>ulong</c>-backed
        /// enum, which is injective and so still serves its only purpose here: de-duplicating
        /// aliases, since two <c>case</c> labels of the same value do not compile.
        /// </summary>
        public long Value { get; }

        public bool Equals(ProtoJsonEnumMember other)
            => CSharpName == other.CSharpName && JsonName == other.JsonName && Value == other.Value;

        public override bool Equals(object? obj) => obj is ProtoJsonEnumMember other && Equals(other);

        public override int GetHashCode()
            => (CSharpName.GetHashCode() * 397) ^ (JsonName.GetHashCode() * 31) ^ Value.GetHashCode();
    }

    /// <summary>
    /// An enum reachable from the model, with the name table canonical JSON needs.
    /// </summary>
    internal readonly struct ProtoJsonEnumPlan : IEquatable<ProtoJsonEnumPlan>
    {
        public ProtoJsonEnumPlan(string typeName, EquatableArray<ProtoJsonEnumMember> members,
            bool isFlags, string underlyingTypeName)
        {
            TypeName = typeName;
            Members = members;
            IsFlags = isFlags;
            UnderlyingTypeName = underlyingTypeName;
        }

        public string TypeName { get; }

        /// <summary>
        /// The enum's underlying type in C#, for the numeric fallback's cast.
        /// </summary>
        /// <remarks>
        /// Not cosmetic: a <c>ulong</c>-backed enum whose value has the high bit set prints as a
        /// negative number through a <c>long</c> cast, which is a different value.
        /// </remarks>
        public string UnderlyingTypeName { get; }

        public EquatableArray<ProtoJsonEnumMember> Members { get; }

        /// <summary>
        /// Whether the enum carries <c>[Flags]</c>, in which case a combined value matches no single
        /// name and falls through to the numeric form.
        /// </summary>
        /// <remarks>
        /// Recorded rather than acted on: the numeric fallback is already what an unrecognised value
        /// gets, so a flags enum needs no separate treatment. It is here because "why does this one
        /// write numbers" is otherwise a puzzle for whoever reads the output next.
        /// </remarks>
        public bool IsFlags { get; }

        public bool Equals(ProtoJsonEnumPlan other)
            => TypeName == other.TypeName && Members.Equals(other.Members) && IsFlags == other.IsFlags
                && UnderlyingTypeName == other.UnderlyingTypeName;

        public override bool Equals(object? obj) => obj is ProtoJsonEnumPlan other && Equals(other);

        public override int GetHashCode()
            => (TypeName.GetHashCode() * 397) ^ Members.GetHashCode() ^ (IsFlags ? 8191 : 0);
    }
}
