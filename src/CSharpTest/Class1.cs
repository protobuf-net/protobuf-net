using ProtoBuf;

#pragma warning disable CS0649
[ProtoContract]
class Foo
{
    [ProtoMember(1)]
    public int SomeField;

    [ProtoMember(tag: 3, IsRequired = true)]
    public int SomeOtherField;

    [ProtoMember(2, DataFormat = global::ProtoBuf.DataFormat.FixedSize)]
    public string SomeProperty { get; }


}
[ProtoContract]
struct Bar
{
    [ProtoMember(1)]
    public int SomeField;

    [ProtoMember(2)]
    public string SomeProperty { get; }
}
#pragma warning restore CS0649