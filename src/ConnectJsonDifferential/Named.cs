using System.Runtime.Serialization;
using System.Xml.Serialization;
using ProtoBuf;

namespace ProtoBuf.ConnectJsonDifferential;

/// <summary>
/// Every place protobuf-net takes a schema name from, and the precedence between them.
/// </summary>
/// <remarks>
/// <c>MetaType.GetFieldName</c> is <b>first-wins</b> across four sources, in this order:
/// <c>[ProtoMember(Name)]</c>, the type's <c>[ProtoPartialMember(Name)]</c>, <c>[DataMember(Name)]</c>
/// and <c>[XmlElement/XmlArray(ElementName)]</c> - and the last two are read inside blocks that only
/// run when no ProtoBuf-family attribute pinned a tag.
/// <para>
/// That gating is the part worth a fixture: a member with both <c>[ProtoMember(5)]</c> and
/// <c>[DataMember(Name = "...")]</c> keeps its <b>C# name</b>, because the block carrying that Name is
/// never reached. It reads like a rename that does nothing, and it is - in protobuf-net as much as
/// here, which is the only reason it is right.
/// </para>
/// <para>
/// The differential is what checks this: <c>GetProto</c> is protobuf-net's own answer, the oracle's
/// JSON keys are derived from it by protoc, and ours are derived from the plan. If the precedence
/// here drifted from <c>MetaType</c>'s, the two sets of keys would stop agreeing.
/// </para>
/// </remarks>
[ProtoContract]
[ProtoPartialMember(4, nameof(ByPartial), Name = "by_partial")]
public class Named
{
    /// <summary>The direct spelling, and the one that beats everything else.</summary>
    [ProtoMember(1, Name = "by_proto_member")]
    public string ByProtoMember { get; set; }

    /// <summary>
    /// <b>Not renamed</b>: the ProtoBuf family pinned the tag, so the <c>[DataMember]</c> block that
    /// would have supplied the name is skipped entirely.
    /// </summary>
    [ProtoMember(2)]
    [DataMember(Name = "ignored_because_tag_was_pinned")]
    public string TagPinnedSoNameIgnored { get; set; }

    /// <summary>
    /// <c>[ProtoMember(Name)]</c> wins over a <c>[DataMember(Name)]</c> on the same member, and here
    /// the DataMember one is unreachable anyway.
    /// </summary>
    [ProtoMember(3, Name = "proto_member_wins")]
    [DataMember(Name = "data_member_loses")]
    public string BothSpellings { get; set; }

    /// <summary>Renamed from the type, by name, for a member that pins no tag of its own.</summary>
    public string ByPartial { get; set; }
}

/// <summary>
/// The same question for a <c>[DataContract]</c> type, where <c>[DataMember]</c> supplies the tag and
/// so its <c>Name</c> is actually reached.
/// </summary>
[DataContract]
public class NamedByContract
{
    [DataMember(Order = 1, Name = "renamed_by_data_member")]
    public string ByDataMember { get; set; }

    /// <summary>No <c>Name</c>, so the schema keeps the C# spelling.</summary>
    [DataMember(Order = 2)]
    public string Plain { get; set; }
}

/// <summary>And the Xml family, whose argument is <c>ElementName</c> rather than <c>Name</c>.</summary>
[XmlType]
public class NamedByXml
{
    [XmlElement(Order = 1, ElementName = "renamed_by_xml")]
    public string ByXmlElement { get; set; }

    [XmlElement(Order = 2)]
    public string Plain { get; set; }
}
