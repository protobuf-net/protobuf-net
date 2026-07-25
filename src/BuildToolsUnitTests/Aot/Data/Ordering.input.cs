using ProtoBuf;
using ProtoBuf.Meta;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace AotFixtures.Ordering;

// [DataContract] is a contract marker in its own right; field numbers come from DataMember.Order
[DataContract]
public class ViaDataMember
{
    [DataMember(Order = 1)] public int First { get; set; }
    [DataMember(Order = 2)] public string Second { get; set; }

    // Order defaults to -1, so this is not serialized at all
    [DataMember] public int NoOrder { get; set; }

    public int Undecorated { get; set; }
}

// the offset applies to DataMember orders, giving fields 11 and 12
[ProtoContract(DataMemberOffset = 10)]
[DataContract]
public class ViaDataMemberOffset
{
    [DataMember(Order = 1)] public int First { get; set; }
    [DataMember(Order = 2)] public string Second { get; set; }
}

[XmlType]
public class ViaXmlElement
{
    [XmlElement(Order = 1)] public int First { get; set; }
    [XmlElement(Order = 2)] public string Second { get; set; }

    [XmlElement(Order = 3), XmlIgnore] public int Ignored { get; set; }
}

// ... but the offset does *not* reach XmlElement, so this stays field 1
[ProtoContract(DataMemberOffset = 10)]
[XmlType]
public class OffsetIgnoredByXml
{
    [XmlElement(Order = 1)] public int First { get; set; }
}

// both families at once: [ProtoMember] wins for its own member, [DataMember] supplies the other
[ProtoContract]
[DataContract]
public class Mixed
{
    [ProtoMember(5), DataMember(Order = 1)] public int Both { get; set; }
    [DataMember(Order = 2)] public int OnlyDataMember { get; set; }
}

public static class OrderingSamples
{
    public static object[] Values =>
    [
        new ViaDataMember(),
        new ViaDataMember { First = 1, Second = "a", NoOrder = 9, Undecorated = 8 },
        new ViaDataMemberOffset(),
        new ViaDataMemberOffset { First = 2, Second = "b" },
        new ViaXmlElement(),
        new ViaXmlElement { First = 3, Second = "c", Ignored = 7 },
        new OffsetIgnoredByXml(),
        new OffsetIgnoredByXml { First = 4 },
        new Mixed(),
        new Mixed { Both = 5, OnlyDataMember = 6 },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(ViaDataMember))]
[ProtoSerializable(typeof(ViaDataMemberOffset))]
[ProtoSerializable(typeof(ViaXmlElement))]
[ProtoSerializable(typeof(OffsetIgnoredByXml))]
[ProtoSerializable(typeof(Mixed))]
public partial class OrderingModel : TypeModel
{
}
