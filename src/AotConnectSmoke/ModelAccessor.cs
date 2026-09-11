using ProtoBuf.Serializers;

namespace ProtoBuf.AotConnectSmoke;

// Hoists serializer resolution out of the per-message path.
//
// ProtoModelGenerator emits `ProtoBufGeneratedServices` as a PRIVATE NESTED class of the model, and
// `SerializerCache.Get<TProvider, T>()` is public - so another part of the same partial class can name
// it and hand the resolved ISerializer<T> out. Nothing in Core has to change for this to work.
//
// If it survives review this becomes a generator emit rather than a hand-written partial.
public partial class SmokeModel
{
    /// <summary>
    /// The model's serializer for <typeparamref name="T"/>, resolved once rather than per message.
    /// </summary>
    /// <remarks>
    /// The annotation is restated because <c>SerializerCache.Get</c> demands it, and it terminates
    /// immediately: every caller is a static initialiser naming a concrete contract type.
    /// <c>DynamicAccess</c> is internal to protobuf-net, so the flags are spelled out - the same
    /// reason <c>ProtoModelGenerator</c> spells them out in what it emits.
    /// </remarks>
    public static ISerializer<T> Serializer<
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
            System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors
            | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicConstructors
            | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor
            | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties
            | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties
            | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields
            | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields
            | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods
            | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods
            | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicNestedTypes
            | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicNestedTypes)] T>()
        => SerializerCache.Get<ProtoBufGeneratedServices, T>();
}
