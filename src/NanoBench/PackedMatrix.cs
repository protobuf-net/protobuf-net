using ProtoBuf;
using System.Collections.Generic;

namespace ProtoBuf.Nano.Bench.Packed;

// The packed permutation matrix - the payload every SIMD item in notes/gaps.md (B19-B21) has been
// blocked on, and the thing notes/packed-writes.md says the descriptor set cannot provide (it is
// 71.5% string/bytes and carries almost no packed content).
//
// ONE CONTRACT PER CATEGORY, rather than one contract with everything, so a benchmark arm can
// isolate a single encoding. The categories are the ones that behave differently on the wire, not
// the ones that look different in C#:
//
//   varint unsigned   1-5 / 1-10 bytes, no sign extension
//   varint signed     the same, EXCEPT a negative is always 10 bytes (protobuf's sign-extension
//                     quirk) - which is why int cannot share uint's ladder
//   zigzag            transformed first, then unsigned: sint32 is always 1-5, never 10
//   fixed integer     4 or 8 bytes flat; block-copyable
//   floating point    likewise, and always fixed - there is no varint form
//   bool              looks like a varint, behaves like a fixed width: always exactly one byte
//   enum              varint over the underlying type - and NEVER actually packed today, because
//                     EnumSerializer is not an IMeasuringSerializer (gaps.md B1)
//
// Each category carries the same shape as both T[] and List<T>: the array has a span natively on
// every TFM, the list only via CollectionsMarshal on net5+, so they take different paths.

public enum Level { None = 0, Low = 1, Mid = 2, High = 3 }

[ProtoContract]
public class PackedVarintUnsigned
{
    [ProtoMember(1, IsPacked = true)] public uint[] U32Array { get; set; }
    [ProtoMember(2, IsPacked = true)] public List<uint> U32List { get; set; }
    [ProtoMember(3, IsPacked = true)] public ulong[] U64Array { get; set; }
    [ProtoMember(4, IsPacked = true)] public List<ulong> U64List { get; set; }
}

[ProtoContract]
public class PackedVarintSigned
{
    [ProtoMember(1, IsPacked = true)] public int[] I32Array { get; set; }
    [ProtoMember(2, IsPacked = true)] public List<int> I32List { get; set; }
    [ProtoMember(3, IsPacked = true)] public long[] I64Array { get; set; }
    [ProtoMember(4, IsPacked = true)] public List<long> I64List { get; set; }
}

[ProtoContract]
public class PackedZigZag
{
    [ProtoMember(1, IsPacked = true, DataFormat = DataFormat.ZigZag)] public int[] S32Array { get; set; }
    [ProtoMember(2, IsPacked = true, DataFormat = DataFormat.ZigZag)] public List<int> S32List { get; set; }
    [ProtoMember(3, IsPacked = true, DataFormat = DataFormat.ZigZag)] public long[] S64Array { get; set; }
    [ProtoMember(4, IsPacked = true, DataFormat = DataFormat.ZigZag)] public List<long> S64List { get; set; }
}

[ProtoContract]
public class PackedFixedInt
{
    [ProtoMember(1, IsPacked = true, DataFormat = DataFormat.FixedSize)] public int[] F32Array { get; set; }
    [ProtoMember(2, IsPacked = true, DataFormat = DataFormat.FixedSize)] public List<int> F32List { get; set; }
    [ProtoMember(3, IsPacked = true, DataFormat = DataFormat.FixedSize)] public long[] F64Array { get; set; }
    [ProtoMember(4, IsPacked = true, DataFormat = DataFormat.FixedSize)] public List<long> F64List { get; set; }
}

[ProtoContract]
public class PackedFloatingPoint
{
    [ProtoMember(1, IsPacked = true)] public float[] SingleArray { get; set; }
    [ProtoMember(2, IsPacked = true)] public List<float> SingleList { get; set; }
    [ProtoMember(3, IsPacked = true)] public double[] DoubleArray { get; set; }
    [ProtoMember(4, IsPacked = true)] public List<double> DoubleList { get; set; }
}

[ProtoContract]
public class PackedBools
{
    [ProtoMember(1, IsPacked = true)] public bool[] BoolArray { get; set; }
    [ProtoMember(2, IsPacked = true)] public List<bool> BoolList { get; set; }
}

[ProtoContract]
public class PackedEnums
{
    [ProtoMember(1, IsPacked = true)] public Level[] EnumArray { get; set; }
    [ProtoMember(2, IsPacked = true)] public List<Level> EnumList { get; set; }
}

/// <summary>The raw-writer model: measure-first plus the raw write surface.</summary>
[ProtoModel]
[ProtoSerializable(typeof(PackedVarintUnsigned))]
[ProtoSerializable(typeof(PackedVarintSigned))]
[ProtoSerializable(typeof(PackedZigZag))]
[ProtoSerializable(typeof(PackedFixedInt))]
[ProtoSerializable(typeof(PackedFloatingPoint))]
[ProtoSerializable(typeof(PackedBools))]
[ProtoSerializable(typeof(PackedEnums))]
public partial class PackedRawModel : ProtoBuf.Meta.TypeModel { }

/// <summary>
/// The same domain emitted the classic way — the escape hatch, and the control.
/// </summary>
/// <remarks>
/// Two models over one domain in a single build is what makes this comparable without a second
/// process; the equivalence itself is gated by <c>ClassicVsRawTests</c> in AotConformanceTests.
/// </remarks>
[ProtoModel(ClassicEmit = true)]
[ProtoSerializable(typeof(PackedVarintUnsigned))]
[ProtoSerializable(typeof(PackedVarintSigned))]
[ProtoSerializable(typeof(PackedZigZag))]
[ProtoSerializable(typeof(PackedFixedInt))]
[ProtoSerializable(typeof(PackedFloatingPoint))]
[ProtoSerializable(typeof(PackedBools))]
[ProtoSerializable(typeof(PackedEnums))]
public partial class PackedClassicModel : ProtoBuf.Meta.TypeModel { }
