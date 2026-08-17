// NOTE: no .reference.cs yet - added on Linux, and AotRefGen is net472 so it could not be run.
// Nothing here is refused by ref-emit, so this fixture *should* have one.
// Differentially covered in the meantime by AotConformanceTests, which is net8.0 and compares
// these samples against RuntimeTypeModel in both directions; what is missing is the decompiled
// ref-emit output, which is a reviewing aid. Run AotRefGen on Windows and commit the result.
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System.Collections.Generic;

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

    // ...and as collection elements. These state *no* wire type in the element features and pass the
    // element serializer instead, so WriteRepeated/ReadRepeated inherit the category and wire type
    // from it - the same deferral the unary members above get from WriteAny/ReadAny.
    [ProtoMember(4)] public List<Measure> Scalars { get; set; }
    [ProtoMember(5)] public List<Label> Messages { get; set; }

    // Nullable<TStruct> where TStruct's serializer's category is undetermined at compile time - the
    // one shape DynamicCategory otherwise had no member for. On read, member.IsNullable steers the
    // GetValueOrDefault() unwrap before ReadAny; on write, HasValue decides presence (the struct
    // itself is never null) and then WriteAny takes the framing off the serializer's real Features.
    [ProtoMember(6)] public Measure? NullableScalar { get; set; }
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
        new Reading { Scalars = [new Measure(1), new Measure(0), new Measure(-3)] },
        new Reading { Messages = [new Label { Text = "a" }, new Label { Text = "b" }] },
        // NullableScalar present - the HasValue-guarded WriteAny/ReadAny path
        new Reading { NullableScalar = new Measure(17) },
        new Reading
        {
            Scalar = new Measure(5),
            Scalars = [new Measure(9)],
            Message = new Label { Text = "m" },
            Messages = [new Label { Text = "n" }],
            // NullableScalar deliberately left null here, so the "everything else set" sample also
            // pins the absent case alongside a fully populated instance
            Other = 11,
        },
    ];
}
