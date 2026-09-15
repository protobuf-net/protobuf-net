using System.Collections.Generic;
using ProtoBuf;

namespace ProtoBuf.ConnectJsonDifferential;

/// <summary>
/// Every scalar kind the JSON mapping has a form for, in every container that can hold it.
/// </summary>
/// <remarks>
/// <b>Breadth here beats a real-world corpus</b>, and the reason is worth stating because the binary
/// path concluded the opposite. Binary's risk is in what people actually write - odd attribute
/// combinations, generated DTOs, shapes nobody would choose - so a 3090-contract sweep of real code
/// is the right instrument. The JSON mapping's risk is different in kind: it is a <em>specified
/// transformation</em>, so its surface is the cross-product of scalar kind and container, and real
/// code covers that cross-product sparsely and by accident. A fixture that walks it deliberately
/// covers more of the thing that can be wrong, with far less machinery.
/// <para>
/// <c>Coverage</c> in the differential asserts this is actually complete, rather than leaving it to
/// whoever edits the file next to notice a gap.
/// </para>
/// </remarks>
[ProtoContract]
public class Wide
{
    // --- singular scalars, one per protobuf type the mapping names -----------------------------
    [ProtoMember(1)] public bool Bool { get; set; }
    [ProtoMember(2)] public int Int32 { get; set; }
    [ProtoMember(3)] public long Int64 { get; set; }
    [ProtoMember(4)] public uint UInt32 { get; set; }
    [ProtoMember(5)] public ulong UInt64 { get; set; }
    [ProtoMember(6)] public float Float { get; set; }
    [ProtoMember(7)] public double Double { get; set; }
    [ProtoMember(8)] public string String { get; set; }
    [ProtoMember(9)] public byte[] Bytes { get; set; }
    [ProtoMember(10)] public Shade Enum { get; set; }
    [ProtoMember(11)] public Leaf Message { get; set; }

    // the narrow integers, which protobuf has no separate type for - they are int32/uint32 on the
    // wire and so plain JSON numbers, which is only interesting because C# makes them look distinct
    [ProtoMember(12)] public sbyte SByte { get; set; }
    [ProtoMember(13)] public byte Byte { get; set; }
    [ProtoMember(14)] public short Int16 { get; set; }
    [ProtoMember(15)] public ushort UInt16 { get; set; }

    // a char is a uint16 varint, so its schema type is uint32 and its JSON is the NUMBER - not the
    // character, which is what every POCO serializer would write
    [ProtoMember(16)] public char Char { get; set; }

    // fixed-width at 64 regardless of platform, so int64/uint64 - and therefore JSON STRINGS
    [ProtoMember(17)] public nint IntPtr { get; set; }
    [ProtoMember(18)] public nuint UIntPtr { get; set; }

    // inbuilt, and a plain string on the wire - not a surrogate case
    [ProtoMember(19)] public Uri Uri { get; set; }

    // --- explicit presence ---------------------------------------------------------------------
    [ProtoMember(20)] public int? NullableInt32 { get; set; }
    [ProtoMember(21)] public bool? NullableBool { get; set; }
    [ProtoMember(22)] public Shade? NullableEnum { get; set; }
    [ProtoMember(23)] public double? NullableDouble { get; set; }

    // --- repeated, one per kind that can be an element -----------------------------------------
    [ProtoMember(30)] public List<bool> RepeatedBool { get; set; } = new();
    [ProtoMember(31)] public List<int> RepeatedInt32 { get; set; } = new();
    [ProtoMember(32)] public List<long> RepeatedInt64 { get; set; } = new();
    [ProtoMember(33)] public List<uint> RepeatedUInt32 { get; set; } = new();
    [ProtoMember(34)] public List<ulong> RepeatedUInt64 { get; set; } = new();
    [ProtoMember(35)] public List<float> RepeatedFloat { get; set; } = new();
    [ProtoMember(36)] public List<double> RepeatedDouble { get; set; } = new();
    [ProtoMember(37)] public List<string> RepeatedString { get; set; } = new();
    [ProtoMember(38)] public List<byte[]> RepeatedBytes { get; set; } = new();
    [ProtoMember(39)] public List<Shade> RepeatedEnum { get; set; } = new();
    [ProtoMember(40)] public List<Leaf> RepeatedMessage { get; set; } = new();

    // --- map keys: every type protobuf allows as one, and no others ----------------------------
    // Only the keys protobuf-net actually models as a map. Its set is NARROWER than protobuf's -
    // bool, char, nint and nuint are all legal protobuf map keys that protobuf-net emits as a
    // `repeated KeyValuePair_...` instead - and also WRONGLY WIDER, since it accepts an enum key that
    // protoc rejects. Probe.cs pins both edges. Measured against plain protoc, not inferred.
    [ProtoMember(50)] public Dictionary<string, int> KeyString { get; set; } = new();
    [ProtoMember(51)] public Dictionary<int, int> KeyInt32 { get; set; } = new();
    [ProtoMember(52)] public Dictionary<long, int> KeyInt64 { get; set; } = new();
    [ProtoMember(53)] public Dictionary<uint, int> KeyUInt32 { get; set; } = new();
    [ProtoMember(54)] public Dictionary<ulong, int> KeyUInt64 { get; set; } = new();


    // --- map values: every kind that can be one ------------------------------------------------
    [ProtoMember(60)] public Dictionary<string, bool> ValueBool { get; set; } = new();
    [ProtoMember(61)] public Dictionary<string, long> ValueInt64 { get; set; } = new();
    [ProtoMember(62)] public Dictionary<string, ulong> ValueUInt64 { get; set; } = new();
    [ProtoMember(63)] public Dictionary<string, double> ValueDouble { get; set; } = new();
    [ProtoMember(64)] public Dictionary<string, string> ValueString { get; set; } = new();
    [ProtoMember(65)] public Dictionary<string, byte[]> ValueBytes { get; set; } = new();
    [ProtoMember(66)] public Dictionary<string, Shade> ValueEnum { get; set; } = new();
    [ProtoMember(67)] public Dictionary<string, Leaf> ValueMessage { get; set; } = new();

    // --- the compatibility-level group, at a level where a JSON form exists --------------------
    [ProtoMember(70)] public WideTemporal Temporal { get; set; }
}

/// <summary>
/// The four types whose encoding the compatibility level chooses, at level 300 where each is either
/// a google.protobuf well-known type or a string.
/// </summary>
/// <remarks>
/// A separate message because the level is a type-level decision. Below 240 (<c>DateTime</c>,
/// <c>TimeSpan</c>) or 300 (<c>Guid</c>, <c>decimal</c>) these are protobuf-net's own messages with
/// no canonical JSON at all, which is what <c>Awkward</c> in Probe.cs pins.
/// </remarks>
[ProtoContract]
[CompatibilityLevel(CompatibilityLevel.Level300)]
public class WideTemporal
{
    [ProtoMember(1)] public DateTime Timestamp { get; set; }
    [ProtoMember(2)] public TimeSpan Duration { get; set; }
    [ProtoMember(3)] public Guid Guid { get; set; }
    [ProtoMember(4)] public decimal Decimal { get; set; }

    [ProtoMember(5)] public List<DateTime> RepeatedTimestamp { get; set; } = new();
    [ProtoMember(6)] public List<Guid> RepeatedGuid { get; set; } = new();
    [ProtoMember(7)] public Dictionary<string, TimeSpan> ValueDuration { get; set; } = new();
    [ProtoMember(8)] public DateTime? NullableTimestamp { get; set; }
}
