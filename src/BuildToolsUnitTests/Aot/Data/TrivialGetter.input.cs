using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;

namespace AotFixtures.TrivialGetter;

// A getter-only property whose body is trivial enough to name the field it returns. All three
// spellings resolve to the same field, which is then reached exactly as an auto-property's backing
// field is.
//
// This is the one place the generator is strictly more capable than ref-emit rather than merely
// different: RuntimeTypeModel *throws* on these (PropertyDecorator.SanityCheck, "cannot apply
// changes to property"), on both its runtime and its persisted path, because reflection has no
// setter to call and no way to know which field the getter reads. We do know, because we can see
// the source. So this fixture deliberately has no samples and no .reference.cs - there is no
// reference behaviour to differ from. TrivialGetterTests covers it instead.
[ProtoContract]
public class Backed
{
    private int _value;
    private string _text;
    private readonly List<int> _numbers = new();

    public Backed() { }
    public Backed(int value, string text)
    {
        _value = value;
        _text = text;
    }

    [ProtoMember(1)] public int Value => _value;
    [ProtoMember(2)] public string Text { get => _text; }

    // a getter-only collection: already worked, since it is populated by mutation
    [ProtoMember(3)] public List<int> Numbers { get { return _numbers; } }
}

// anything less than trivial keeps the old behaviour - the value is read and discarded, because a
// guessed field name would silently write to the wrong place
[ProtoContract]
public class Computed
{
    private int _value;

    [ProtoMember(1)] public int Doubled => _value * 2;
}

[ProtoModel]
[ProtoSerializable(typeof(Backed))]
[ProtoSerializable(typeof(Computed))]
public partial class TrivialGetterModel : TypeModel
{
}
