using ProtoBuf;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace AotFixtures.PackedAll;

// EVERY packed emission in one golden, for review.
//
// This exists because the raw packed surface has six write methods and only two of them
// (WriteRawPackedVarint, WriteRawPackedFixed64) appeared in any checked-in golden - Bool, Fixed32
// and ZigZag were reachable only through NanoBench, whose generated output is not committed, and
// through RawPackedWriteTests, which calls the API directly rather than through generated code. So
// there was no file anyone could read to see what the generator actually emits for them.
//
// One contract per encoding category would mirror NanoBench; one contract with everything is
// better here, because the point is a single readable diff of the whole surface.

public enum Level { None = 0, Low = 1, Mid = 2, High = 3 }

// A DERIVED list, which resolves to CreateList<TRoot, T>() rather than CreateList<T>() and so
// carries TakesCollectionType. It is packed-eligible where the UNPACKED path still refuses it:
// there, `foreach` binds to the DECLARED type's GetEnumerator, which a derived type may hide with
// a redeclaration; CollectionsMarshal.AsSpan takes the List<T> and has no such hazard. gap B23.
//
// The hiding member is deliberate rather than decoration - it is the exact shape the unpacked
// exclusion exists for, so without it this fixture would not distinguish "the hazard does not
// apply to AsSpan" from "there was no hazard here to begin with".
public class Readings : List<uint>
{
    public new List<uint>.Enumerator GetEnumerator() => throw new System.NotSupportedException(
        "if this runs, the packed write bound to the DECLARED type's enumerator rather than AsSpan");
}

[ProtoContract]
public class EveryPackedShape
{
    // --- varint: the three span types the surface takes, reached from all three collection shapes
    [ProtoMember(1, IsPacked = true)] public uint[] U32Array { get; set; }
    [ProtoMember(2, IsPacked = true)] public List<uint> U32List { get; set; }
    [ProtoMember(3, IsPacked = true)] public ImmutableArray<uint> U32Immutable { get; set; }

    // int32 is NOT the unsigned arm with a cast: a negative sign-extends to ten bytes
    [ProtoMember(4, IsPacked = true)] public int[] I32Array { get; set; }

    // long puns onto the unsigned 64-bit arm at the call site
    [ProtoMember(5, IsPacked = true)] public ulong[] U64Array { get; set; }
    [ProtoMember(6, IsPacked = true)] public long[] I64Array { get; set; }

    // --- zigzag: signed-only, and NOT punned to unsigned (the transform is signed by definition)
    [ProtoMember(7, IsPacked = true, DataFormat = DataFormat.ZigZag)] public int[] S32Array { get; set; }
    [ProtoMember(8, IsPacked = true, DataFormat = DataFormat.ZigZag)] public long[] S64Array { get; set; }

    // --- fixed width: FixedSize flattens an integer column; float/double are fixed by nature
    [ProtoMember(9, IsPacked = true, DataFormat = DataFormat.FixedSize)] public int[] F32Array { get; set; }
    [ProtoMember(10, IsPacked = true, DataFormat = DataFormat.FixedSize)] public long[] F64Array { get; set; }
    [ProtoMember(11, IsPacked = true)] public float[] Singles { get; set; }
    [ProtoMember(12, IsPacked = true)] public double[] Doubles { get; set; }

    // --- bool: the payload IS the span, behind a vectorised canonical scan
    [ProtoMember(13, IsPacked = true)] public bool[] Flags { get; set; }
    [ProtoMember(14, IsPacked = true)] public List<bool> FlagList { get; set; }

    // --- enum: the design's showcase. Punned at the CALL SITE via MemoryMarshal.Cast, so the
    // library has no enum-specific code and the column IS the int32 column from here on
    [ProtoMember(15, IsPacked = true)] public Level[] Levels { get; set; }
    [ProtoMember(16, IsPacked = true)] public List<Level> LevelList { get; set; }

    // --- a derived list: same CollectionsMarshal.AsSpan as List<uint>, despite TakesCollectionType
    [ProtoMember(17, IsPacked = true)] public Readings Derived { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(EveryPackedShape))]
public partial class PackedAllModel : ProtoBuf.Meta.TypeModel { }

public static class PackedAllSamples
{
    public static object[] Values =>
    [
        new EveryPackedShape(),
        new EveryPackedShape
        {
            // 40 elements where it matters, so the 32-element vector block engages AND leaves a
            // ragged tail; values straddle 128 so the uniform-block blit and the scalar fallback
            // both run. A shorter column would exercise only the fallback.
            U32Array = [.. Range40(i => (uint)(i % 5 == 0 ? i * 1000 : i))],
            U32List = [.. Range40(i => (uint)(i % 200))],
            U32Immutable = [.. Range40(i => (uint)i)],
            I32Array = [.. Range40(i => (i & 3) == 3 ? -(i + 1) : i)],
            U64Array = [.. Range40(i => (ulong)i | ((ulong)(i % 7) << 40))],
            I64Array = [.. Range40(i => (long)(i - 20) * 1_000_003L)],
            S32Array = [.. Range40(i => (i & 1) == 0 ? i : -i)],
            S64Array = [.. Range40(i => (long)((i & 1) == 0 ? i : -i) * 7)],
            F32Array = [.. Range40(i => i - 20)],
            F64Array = [.. Range40(i => (long)i * -3)],
            Singles = [.. Range40(i => i * 1.5f)],
            Doubles = [.. Range40(i => i * 1.25d)],
            Flags = [.. Range40(i => (i % 3) == 0)],
            FlagList = [.. Range40(i => (i % 2) == 0)],
            Levels = [.. Range40(i => (Level)(i & 3))],
            LevelList = [.. Range40(i => (Level)(i % 3))],
            Derived = Reading40(),
        },
        // a single element each: the framing rule that a lone value is written UNPACKED, with its
        // own per-element header, rather than as a one-element packed block
        new EveryPackedShape
        {
            U32Array = [7],
            Flags = [true],
            Levels = [Level.High],
            Singles = [1f],
        },
        // empty: still writes a zero-length header, which "skip if empty" would drop
        new EveryPackedShape
        {
            U32Array = [],
            Flags = [],
            Doubles = [],
        },
    ];

    private static IEnumerable<T> Range40<T>(System.Func<int, T> gen)
    {
        for (int i = 0; i < 40; i++) yield return gen(i);
    }

    // built with Add rather than a collection expression: the hiding GetEnumerator above makes
    // `[.. ]` bind to it, which would throw while BUILDING the sample and say nothing about the
    // generator
    private static Readings Reading40()
    {
        var value = new Readings();
        for (int i = 0; i < 40; i++) ((List<uint>)value).Add((uint)(i % 200));
        return value;
    }
}
