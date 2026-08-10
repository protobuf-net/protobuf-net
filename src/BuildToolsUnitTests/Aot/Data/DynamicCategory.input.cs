using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

namespace AotFixtures.DynamicCategory;

// A hand-written serializer whose *category* cannot be established at compile time. `Features` is a
// property, so a generator can only read it when the declaration folds to a constant; here it
// deliberately does not, which is the same position the generator is in for any serializer that
// arrives through a compiled reference.
//
// It used to drop the contract. It no longer needs to: the only thing the category decided was how a
// *member* of this type is framed, and WriteAny/ReadAny make exactly that choice at run time from
// the serializer's own Features. The contract itself emits no body either way - the services type
// just hands the hand-written serializer out.
public sealed class MeasureSerializer : ISerializer<Measure>
{
    private static SerializerFeatures Category() => SerializerFeatures.CategoryScalar;

    SerializerFeatures ISerializer<Measure>.Features => Category() | SerializerFeatures.WireTypeVarint;

    Measure ISerializer<Measure>.Read(ref ProtoReader.State state, Measure value)
        => new Measure(state.ReadInt32());

    void ISerializer<Measure>.Write(ref ProtoWriter.State state, Measure value)
        => state.WriteInt32(value.Value);
}

[ProtoContract(Serializer = typeof(MeasureSerializer))]
public readonly struct Measure
{
    public Measure(int value) => Value = value;
    public int Value { get; }
}

// the same, but a message-category one - the point being that a single emitted shape serves both,
// which is what makes deferring the decision viable at all
public sealed class LabelSerializer : ISerializer<Label>
{
    private static SerializerFeatures Category() => SerializerFeatures.CategoryMessage;

    SerializerFeatures ISerializer<Label>.Features => Category() | SerializerFeatures.WireTypeString;

    Label ISerializer<Label>.Read(ref ProtoReader.State state, Label value)
    {
        value ??= new Label();
        int field;
        while ((field = state.ReadFieldHeader()) > 0)
        {
            if (field == 1) value.Text = state.ReadString();
            else state.SkipField();
        }
        return value;
    }

    void ISerializer<Label>.Write(ref ProtoWriter.State state, Label value)
        => state.WriteString(1, value.Text);
}

[ProtoContract(Serializer = typeof(LabelSerializer))]
public class Label
{
    public string Text { get; set; }
}

[ProtoContract]
public class Reading
{
    [ProtoMember(1)] public Measure Scalar { get; set; }
    [ProtoMember(2)] public Label Message { get; set; }
    [ProtoMember(3)] public int Other { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(Reading))]
public partial class DynamicCategoryModel : TypeModel
{
}

public static class DynamicCategorySamples
{
    public static object[] Values =>
    [
        new Reading(),
        new Reading { Scalar = new Measure(42), Other = 7 },
        new Reading { Message = new Label { Text = "hi" } },
        new Reading { Scalar = new Measure(-1), Message = new Label { Text = "" }, Other = -2 },
    ];
}
