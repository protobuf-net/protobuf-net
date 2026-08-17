using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

// Structural validation of [ProtoSerializer] declarations, reported at gathering time: an open/
// closed mismatch and an arity mismatch are both mistakes in the declaration itself, before any
// contract is parsed. Under Diagnostics/ because the point is the .txt, not working output.
namespace AotFixtures.SerializerDeclaration;

public readonly struct Pair<TKey, TValue> { }

public sealed class PairSerializer<TKey, TValue> : ISerializer<Pair<TKey, TValue>>
{
    SerializerFeatures ISerializer<Pair<TKey, TValue>>.Features
        => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeVarint;
    Pair<TKey, TValue> ISerializer<Pair<TKey, TValue>>.Read(ref ProtoReader.State state, Pair<TKey, TValue> value)
        => default;
    void ISerializer<Pair<TKey, TValue>>.Write(ref ProtoWriter.State state, Pair<TKey, TValue> value)
        => state.WriteInt32(0);
}

public sealed class OneArgSerializer<T> : ISerializer<Pair<T, int>>
{
    SerializerFeatures ISerializer<Pair<T, int>>.Features
        => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeVarint;
    Pair<T, int> ISerializer<Pair<T, int>>.Read(ref ProtoReader.State state, Pair<T, int> value) => default;
    void ISerializer<Pair<T, int>>.Write(ref ProtoWriter.State state, Pair<T, int> value) => state.WriteInt32(0);
}

[ProtoContract]
public class Untouched
{
    [ProtoMember(1)] public int Value { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(Untouched))]
// open type, closed serializer: mismatch
[ProtoSerializer(typeof(Pair<,>), typeof(PairSerializer<int, int>))]
// open both, arities differ
[ProtoSerializer(typeof(Pair<,>), typeof(OneArgSerializer<>))]
// declared twice at the same scope: no defined winner, so the duplicate is reported
[ProtoSerializer(typeof(Pair<int, int>), typeof(PairSerializer<int, int>))]
[ProtoSerializer(typeof(Pair<int, int>), typeof(PairSerializer<int, int>))]
public partial class SerializerDeclarationModel : TypeModel
{
}
