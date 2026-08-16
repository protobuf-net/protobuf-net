using BenchmarkDotNet.Attributes;
using System;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Nano.Bench;

/// <summary>
/// The dispatch-shape race (Marc, from IL inspection): a raw read dispatches on the tag, and
/// there are two ways to spell the switch. Full-tag labels (<c>case (2 &lt;&lt; 3) | 2:</c>)
/// are what the generator emits today - but even DENSE field numbers make SPARSE tags (fields
/// spread by 8, wire bits vary), so Roslyn/RyuJIT lower it to compare chains or a bucketed
/// search. Field-first (<c>switch (tag &gt;&gt; 3)</c> with a <c>when</c> guard per case)
/// makes the switch operand dense, so consecutive fields become a real jump table, at the
/// cost of one guard test inside each arm.
///
/// Two field sets: Dense9 is Marc's exact snippet shape (fields 1..9); Wide21 is a
/// FileDescriptorProto-ish width (1..12, 16..21 with a gap) plus one high outlier (536),
/// because real schemas have those and one outlier is what breaks naive table density.
/// Two orders: "ordered" is how a writer emits fields (runs, ascending - the branch
/// predictor's best case); "shuffled" is the dispatch-stress case where the lowering
/// strategy dominates.
///
/// The tolerant variants carry the wire-type tolerance the generator really emits for
/// scalars - three labels per field in the full-tag shape, three stacked when-guards per
/// field in the field-first shape (Marc's point: the guard approach RETAINS the
/// branch-per-wire-type design; every wire form keeps its own dedicated arm and read body).
/// The stream itself is 100% natural wires, so this measures what the COLD extra forms
/// cost, which is the question the tolerance design deferred pending measurement.
///
/// Bodies are distinct per arm (sum += a field-specific constant) so arms cannot merge; the
/// GlobalSetup asserts every variant agrees on the sum, so a mis-wired case is a setup
/// failure rather than a fast lie.
/// </summary>
[MemoryDiagnoser]
public class DispatchBenchmarks
{
    [Params("dense9", "wide21")]
    public string FieldSet = "dense9";

    [Params("ordered", "shuffled")]
    public string Order = "ordered";

    private const int TagCount = 65536;
    private uint[] _tags = [];
    private long _expected;

    // (field, natural wire) pairs; wires vary so the full-tag labels are genuinely scattered.
    // Marc's snippet had a wire-4 (end-group) case; data fields cannot arrive on it, so the
    // benchmark uses wire 5 there to keep the stream valid-shaped.
    private static readonly (int Field, int Wire)[] s_dense9 =
    [
        (1, 1), (2, 2), (3, 0), (4, 1), (5, 3), (6, 5), (7, 1), (8, 2), (9, 1),
    ];

    private static readonly (int Field, int Wire)[] s_wide21 =
    [
        (1, 2), (2, 0), (3, 0), (4, 2), (5, 2), (6, 2), (7, 2), (8, 0), (9, 1),
        (10, 5), (11, 0), (12, 2), (16, 0), (17, 0), (18, 2), (19, 0), (20, 2), (21, 0),
        (536, 2),
    ];

    [GlobalSetup]
    public void Setup()
    {
        var pairs = FieldSet == "dense9" ? s_dense9 : s_wide21;
        _tags = new uint[TagCount];
        // runs of 4 per field, fields ascending - the writer's natural order
        int i = 0;
        while (i < TagCount)
        {
            foreach (var (field, wire) in pairs)
            {
                for (int r = 0; r < 4 && i < TagCount; r++)
                {
                    _tags[i++] = (uint)(field << 3 | wire);
                }
            }
        }
        if (Order == "shuffled")
        {
            var rng = new Random(12345);
            for (int j = _tags.Length - 1; j > 0; j--)
            {
                int k = rng.Next(j + 1);
                (_tags[j], _tags[k]) = (_tags[k], _tags[j]);
            }
        }

        // every variant must agree before any number is believed
        _expected = FieldSet == "dense9" ? TagFull9(_tags) : TagFull21(_tags);
        Check(FieldSet == "dense9" ? FieldWhen9(_tags) : FieldWhen21(_tags));
        Check(FieldSet == "dense9" ? TagTolerant9(_tags) : TagTolerant21(_tags));
        Check(FieldSet == "dense9" ? FieldTolerant9(_tags) : FieldTolerant21(_tags));

        void Check(long actual)
        {
            if (actual != _expected) throw new InvalidOperationException(
                $"dispatch variants disagree: {actual} vs {_expected}");
        }
    }

    [Benchmark(Baseline = true)]
    public long TagSwitch() => FieldSet == "dense9" ? TagFull9(_tags) : TagFull21(_tags);

    [Benchmark]
    public long FieldSwitchWhen() => FieldSet == "dense9" ? FieldWhen9(_tags) : FieldWhen21(_tags);

    [Benchmark]
    public long TagSwitchTolerant() => FieldSet == "dense9" ? TagTolerant9(_tags) : TagTolerant21(_tags);

    [Benchmark]
    public long FieldSwitchTolerant() => FieldSet == "dense9" ? FieldTolerant9(_tags) : FieldTolerant21(_tags);

    // ---- dense9: Marc's snippet shape -------------------------------------------------

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long TagFull9(uint[] tags)
    {
        long sum = 0;
        foreach (var tag in tags)
        {
            switch (tag)
            {
                case 1 << 3 | 1: sum += 101; break;
                case 2 << 3 | 2: sum += 202; break;
                case 3 << 3 | 0: sum += 303; break;
                case 4 << 3 | 1: sum += 404; break;
                case 5 << 3 | 3: sum += 505; break;
                case 6 << 3 | 5: sum += 606; break;
                case 7 << 3 | 1: sum += 707; break;
                case 8 << 3 | 2: sum += 808; break;
                case 9 << 3 | 1: sum += 909; break;
                default: sum -= 1; break;
            }
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long FieldWhen9(uint[] tags)
    {
        long sum = 0;
        foreach (var tag in tags)
        {
            switch (tag >> 3)
            {
                case 1 when tag is (1 << 3 | 1): sum += 101; break;
                case 2 when tag is (2 << 3 | 2): sum += 202; break;
                case 3 when tag is (3 << 3 | 0): sum += 303; break;
                case 4 when tag is (4 << 3 | 1): sum += 404; break;
                case 5 when tag is (5 << 3 | 3): sum += 505; break;
                case 6 when tag is (6 << 3 | 5): sum += 606; break;
                case 7 when tag is (7 << 3 | 1): sum += 707; break;
                case 8 when tag is (8 << 3 | 2): sum += 808; break;
                case 9 when tag is (9 << 3 | 1): sum += 909; break;
                default: sum -= 1; break;
            }
        }
        return sum;
    }

    // the generator's scalar tolerance: each field also accepts the two fixed forms (or the
    // varint form, for a fixed-natural field), each wire form with its OWN arm and body -
    // the branch-per-wire-type design, spelled in both switch shapes. The stream never
    // exercises the cold forms; the cost being measured is carrying the labels at all.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long TagTolerant9(uint[] tags)
    {
        long sum = 0;
        foreach (var tag in tags)
        {
            switch (tag)
            {
                case 1 << 3 | 1: sum += 101; break;
                case 1 << 3 | 0: sum += 100; break;
                case 1 << 3 | 5: sum += 105; break;
                case 2 << 3 | 2: sum += 202; break;
                case 3 << 3 | 0: sum += 303; break;
                case 3 << 3 | 5: sum += 305; break;
                case 3 << 3 | 1: sum += 301; break;
                case 4 << 3 | 1: sum += 404; break;
                case 4 << 3 | 0: sum += 400; break;
                case 4 << 3 | 5: sum += 405; break;
                case 5 << 3 | 3: sum += 505; break;
                case 6 << 3 | 5: sum += 606; break;
                case 6 << 3 | 0: sum += 600; break;
                case 6 << 3 | 1: sum += 601; break;
                case 7 << 3 | 1: sum += 707; break;
                case 7 << 3 | 0: sum += 700; break;
                case 7 << 3 | 5: sum += 705; break;
                case 8 << 3 | 2: sum += 808; break;
                case 9 << 3 | 1: sum += 909; break;
                case 9 << 3 | 0: sum += 900; break;
                case 9 << 3 | 5: sum += 905; break;
                default: sum -= 1; break;
            }
        }
        return sum;
    }

    // stacked when-guards per wire form (Marc's shape): the field switch stays dense, and
    // every wire form keeps its dedicated arm exactly as the full-tag emission has
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long FieldTolerant9(uint[] tags)
    {
        long sum = 0;
        foreach (var tag in tags)
        {
            switch (tag >> 3)
            {
                case 1 when tag is (1 << 3 | 1): sum += 101; break;
                case 1 when tag is (1 << 3 | 0): sum += 100; break;
                case 1 when tag is (1 << 3 | 5): sum += 105; break;
                case 2 when tag is (2 << 3 | 2): sum += 202; break;
                case 3 when tag is (3 << 3 | 0): sum += 303; break;
                case 3 when tag is (3 << 3 | 5): sum += 305; break;
                case 3 when tag is (3 << 3 | 1): sum += 301; break;
                case 4 when tag is (4 << 3 | 1): sum += 404; break;
                case 4 when tag is (4 << 3 | 0): sum += 400; break;
                case 4 when tag is (4 << 3 | 5): sum += 405; break;
                case 5 when tag is (5 << 3 | 3): sum += 505; break;
                case 6 when tag is (6 << 3 | 5): sum += 606; break;
                case 6 when tag is (6 << 3 | 0): sum += 600; break;
                case 6 when tag is (6 << 3 | 1): sum += 601; break;
                case 7 when tag is (7 << 3 | 1): sum += 707; break;
                case 7 when tag is (7 << 3 | 0): sum += 700; break;
                case 7 when tag is (7 << 3 | 5): sum += 705; break;
                case 8 when tag is (8 << 3 | 2): sum += 808; break;
                case 9 when tag is (9 << 3 | 1): sum += 909; break;
                case 9 when tag is (9 << 3 | 0): sum += 900; break;
                case 9 when tag is (9 << 3 | 5): sum += 905; break;
                default: sum -= 1; break;
            }
        }
        return sum;
    }

    // ---- wide21: FileDescriptorProto-ish width plus one high outlier ------------------

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long TagFull21(uint[] tags)
    {
        long sum = 0;
        foreach (var tag in tags)
        {
            switch (tag)
            {
                case 1 << 3 | 2: sum += 1; break;
                case 2 << 3 | 0: sum += 2; break;
                case 3 << 3 | 0: sum += 3; break;
                case 4 << 3 | 2: sum += 4; break;
                case 5 << 3 | 2: sum += 5; break;
                case 6 << 3 | 2: sum += 6; break;
                case 7 << 3 | 2: sum += 7; break;
                case 8 << 3 | 0: sum += 8; break;
                case 9 << 3 | 1: sum += 9; break;
                case 10 << 3 | 5: sum += 10; break;
                case 11 << 3 | 0: sum += 11; break;
                case 12 << 3 | 2: sum += 12; break;
                case 16 << 3 | 0: sum += 16; break;
                case 17 << 3 | 0: sum += 17; break;
                case 18 << 3 | 2: sum += 18; break;
                case 19 << 3 | 0: sum += 19; break;
                case 20 << 3 | 2: sum += 20; break;
                case 21 << 3 | 0: sum += 21; break;
                case 536 << 3 | 2: sum += 536; break;
                default: sum -= 1; break;
            }
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long FieldWhen21(uint[] tags)
    {
        long sum = 0;
        foreach (var tag in tags)
        {
            switch (tag >> 3)
            {
                case 1 when tag is (1 << 3 | 2): sum += 1; break;
                case 2 when tag is (2 << 3 | 0): sum += 2; break;
                case 3 when tag is (3 << 3 | 0): sum += 3; break;
                case 4 when tag is (4 << 3 | 2): sum += 4; break;
                case 5 when tag is (5 << 3 | 2): sum += 5; break;
                case 6 when tag is (6 << 3 | 2): sum += 6; break;
                case 7 when tag is (7 << 3 | 2): sum += 7; break;
                case 8 when tag is (8 << 3 | 0): sum += 8; break;
                case 9 when tag is (9 << 3 | 1): sum += 9; break;
                case 10 when tag is (10 << 3 | 5): sum += 10; break;
                case 11 when tag is (11 << 3 | 0): sum += 11; break;
                case 12 when tag is (12 << 3 | 2): sum += 12; break;
                case 16 when tag is (16 << 3 | 0): sum += 16; break;
                case 17 when tag is (17 << 3 | 0): sum += 17; break;
                case 18 when tag is (18 << 3 | 2): sum += 18; break;
                case 19 when tag is (19 << 3 | 0): sum += 19; break;
                case 20 when tag is (20 << 3 | 2): sum += 20; break;
                case 21 when tag is (21 << 3 | 0): sum += 21; break;
                case 536 when tag is (536 << 3 | 2): sum += 536; break;
                default: sum -= 1; break;
            }
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long TagTolerant21(uint[] tags)
    {
        long sum = 0;
        foreach (var tag in tags)
        {
            switch (tag)
            {
                case 1 << 3 | 2: sum += 1; break;
                case 2 << 3 | 0: sum += 2; break;
                case 2 << 3 | 5: sum += 205; break;
                case 2 << 3 | 1: sum += 201; break;
                case 3 << 3 | 0: sum += 3; break;
                case 3 << 3 | 5: sum += 305; break;
                case 3 << 3 | 1: sum += 301; break;
                case 4 << 3 | 2: sum += 4; break;
                case 5 << 3 | 2: sum += 5; break;
                case 6 << 3 | 2: sum += 6; break;
                case 7 << 3 | 2: sum += 7; break;
                case 8 << 3 | 0: sum += 8; break;
                case 8 << 3 | 5: sum += 805; break;
                case 8 << 3 | 1: sum += 801; break;
                case 9 << 3 | 1: sum += 9; break;
                case 9 << 3 | 0: sum += 900; break;
                case 9 << 3 | 5: sum += 905; break;
                case 10 << 3 | 5: sum += 10; break;
                case 10 << 3 | 0: sum += 1000; break;
                case 10 << 3 | 1: sum += 1001; break;
                case 11 << 3 | 0: sum += 11; break;
                case 11 << 3 | 5: sum += 1105; break;
                case 11 << 3 | 1: sum += 1101; break;
                case 12 << 3 | 2: sum += 12; break;
                case 16 << 3 | 0: sum += 16; break;
                case 16 << 3 | 5: sum += 1605; break;
                case 16 << 3 | 1: sum += 1601; break;
                case 17 << 3 | 0: sum += 17; break;
                case 17 << 3 | 5: sum += 1705; break;
                case 17 << 3 | 1: sum += 1701; break;
                case 18 << 3 | 2: sum += 18; break;
                case 19 << 3 | 0: sum += 19; break;
                case 19 << 3 | 5: sum += 1905; break;
                case 19 << 3 | 1: sum += 1901; break;
                case 20 << 3 | 2: sum += 20; break;
                case 21 << 3 | 0: sum += 21; break;
                case 21 << 3 | 5: sum += 2105; break;
                case 21 << 3 | 1: sum += 2101; break;
                case 536 << 3 | 2: sum += 536; break;
                default: sum -= 1; break;
            }
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long FieldTolerant21(uint[] tags)
    {
        long sum = 0;
        foreach (var tag in tags)
        {
            switch (tag >> 3)
            {
                case 1 when tag is (1 << 3 | 2): sum += 1; break;
                case 2 when tag is (2 << 3 | 0): sum += 2; break;
                case 2 when tag is (2 << 3 | 5): sum += 205; break;
                case 2 when tag is (2 << 3 | 1): sum += 201; break;
                case 3 when tag is (3 << 3 | 0): sum += 3; break;
                case 3 when tag is (3 << 3 | 5): sum += 305; break;
                case 3 when tag is (3 << 3 | 1): sum += 301; break;
                case 4 when tag is (4 << 3 | 2): sum += 4; break;
                case 5 when tag is (5 << 3 | 2): sum += 5; break;
                case 6 when tag is (6 << 3 | 2): sum += 6; break;
                case 7 when tag is (7 << 3 | 2): sum += 7; break;
                case 8 when tag is (8 << 3 | 0): sum += 8; break;
                case 8 when tag is (8 << 3 | 5): sum += 805; break;
                case 8 when tag is (8 << 3 | 1): sum += 801; break;
                case 9 when tag is (9 << 3 | 1): sum += 9; break;
                case 9 when tag is (9 << 3 | 0): sum += 900; break;
                case 9 when tag is (9 << 3 | 5): sum += 905; break;
                case 10 when tag is (10 << 3 | 5): sum += 10; break;
                case 10 when tag is (10 << 3 | 0): sum += 1000; break;
                case 10 when tag is (10 << 3 | 1): sum += 1001; break;
                case 11 when tag is (11 << 3 | 0): sum += 11; break;
                case 11 when tag is (11 << 3 | 5): sum += 1105; break;
                case 11 when tag is (11 << 3 | 1): sum += 1101; break;
                case 12 when tag is (12 << 3 | 2): sum += 12; break;
                case 16 when tag is (16 << 3 | 0): sum += 16; break;
                case 16 when tag is (16 << 3 | 5): sum += 1605; break;
                case 16 when tag is (16 << 3 | 1): sum += 1601; break;
                case 17 when tag is (17 << 3 | 0): sum += 17; break;
                case 17 when tag is (17 << 3 | 5): sum += 1705; break;
                case 17 when tag is (17 << 3 | 1): sum += 1701; break;
                case 18 when tag is (18 << 3 | 2): sum += 18; break;
                case 19 when tag is (19 << 3 | 0): sum += 19; break;
                case 19 when tag is (19 << 3 | 5): sum += 1905; break;
                case 19 when tag is (19 << 3 | 1): sum += 1901; break;
                case 20 when tag is (20 << 3 | 2): sum += 20; break;
                case 21 when tag is (21 << 3 | 0): sum += 21; break;
                case 21 when tag is (21 << 3 | 5): sum += 2105; break;
                case 21 when tag is (21 << 3 | 1): sum += 2101; break;
                case 536 when tag is (536 << 3 | 2): sum += 536; break;
                default: sum -= 1; break;
            }
        }
        return sum;
    }
}
