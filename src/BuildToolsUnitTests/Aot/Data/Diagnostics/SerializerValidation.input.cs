using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

// Per-use validation of [ProtoSerializer]: each declaration here is structurally well-formed but
// names a serializer the runtime twin (MetaType.SerializerType / SerializerCache.Get) would reject
// at run time - reported as a warning naming the defect instead.
namespace AotFixtures.SerializerValidation;

public readonly struct Alpha { }
public readonly struct Beta { }
public readonly struct Gamma { }
public readonly struct Delta { }

// not a class
public struct AlphaSerializer : ISerializer<Alpha>
{
    SerializerFeatures ISerializer<Alpha>.Features
        => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeVarint;
    Alpha ISerializer<Alpha>.Read(ref ProtoReader.State state, Alpha value) => default;
    void ISerializer<Alpha>.Write(ref ProtoWriter.State state, Alpha value) => state.WriteInt32(0);
}

// no parameterless constructor
public sealed class BetaSerializer : ISerializer<Beta>
{
    public BetaSerializer(int seed) { }
    SerializerFeatures ISerializer<Beta>.Features
        => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeVarint;
    Beta ISerializer<Beta>.Read(ref ProtoReader.State state, Beta value) => default;
    void ISerializer<Beta>.Write(ref ProtoWriter.State state, Beta value) => state.WriteInt32(0);
}

// does not implement ISerializer<Gamma>
public sealed class GammaSerializer
{
}

// states a category its Features contradicts
public sealed class DeltaSerializer : ISerializer<Delta>
{
    SerializerFeatures ISerializer<Delta>.Features
        => SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString;
    Delta ISerializer<Delta>.Read(ref ProtoReader.State state, Delta value) => default;
    void ISerializer<Delta>.Write(ref ProtoWriter.State state, Delta value) { }
}

[ProtoContract]
public class Carrier
{
    [ProtoMember(1)] public Alpha Alpha { get; set; }
    [ProtoMember(2)] public Beta Beta { get; set; }
    [ProtoMember(3)] public Gamma Gamma { get; set; }
    [ProtoMember(4)] public Delta Delta { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(Carrier))]
[ProtoSerializer(typeof(Alpha), typeof(AlphaSerializer))]
[ProtoSerializer(typeof(Beta), typeof(BetaSerializer))]
[ProtoSerializer(typeof(Gamma), typeof(GammaSerializer))]
[ProtoSerializer(typeof(Delta), typeof(DeltaSerializer), IsScalar = true)]
public partial class SerializerValidationModel : TypeModel
{
}
