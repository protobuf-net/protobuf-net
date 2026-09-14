using Google.Protobuf;

namespace ProtoBuf.ConnectJsonDifferential;

/// <summary>
/// Paired values: one code-first instance and one protoc instance holding exactly the same data.
/// </summary>
/// <remarks>
/// Written as pairs rather than converted from one to the other on purpose - a converter would be a
/// third implementation of the same mapping, and a bug in it would look like a bug in the generator.
/// </remarks>
internal static class Cases
{
    public static IEnumerable<(string Name, Shapes Ours, Oracle.Shapes Theirs)> All()
    {
        // an empty message. Canonical JSON omits every default-valued field, so the correct answer
        // is "{}" - which is also the case that catches a writer emitting explicit zeros
        yield return ("empty", new Shapes(), new Oracle.Shapes());

        yield return ("scalars",
            new Shapes
            {
                Count = 42,
                BigNumber = 9007199254740993L, // above 2^53: survives only because int64 is a STRING
                Unsigned = 4294967295,
                BigUnsigned = ulong.MaxValue,
                Flag = true,
                Ratio = 1.5f,
                Precise = -2.25,
                UserName = "Marc",
                Blob = new byte[] { 0, 1, 250, 255 },
                Colour = Shade.Blue,
                Pinned = "pinned!",
            },
            new Oracle.Shapes
            {
                Count = 42,
                BigNumber = 9007199254740993L,
                Unsigned = 4294967295,
                BigUnsigned = ulong.MaxValue,
                Flag = true,
                Ratio = 1.5f,
                Precise = -2.25,
                UserName = "Marc",
                Blob = ByteString.CopyFrom(0, 1, 250, 255),
                Colour = Oracle.Shade.Blue,
                PinnedName = "pinned!",
            });

        // NaN and the infinities have no JSON number form and travel as strings
        yield return ("float specials",
            new Shapes { Ratio = float.NaN, Precise = double.PositiveInfinity },
            new Oracle.Shapes { Ratio = float.NaN, Precise = double.PositiveInfinity });

        yield return ("negative infinity",
            new Shapes { Precise = double.NegativeInfinity },
            new Oracle.Shapes { Precise = double.NegativeInfinity });

        // an explicit zero enum is a default, and so omitted; a named one is written by NAME
        yield return ("enum zero", new Shapes { Colour = Shade.Unknown }, new Oracle.Shapes { Colour = Oracle.Shade.Unknown });
        yield return ("enum named", new Shapes { Colour = Shade.Green }, new Oracle.Shapes { Colour = Oracle.Shade.Green });

        // a value with no name falls back to the number, which is what lets a value added by a newer
        // peer survive a trip through an older one
        yield return ("enum unnamed", new Shapes { Colour = (Shade)77 }, new Oracle.Shapes { Colour = (Oracle.Shade)77 });

        yield return ("nested message",
            new Shapes { Child = new Leaf { Label = "root", Weight = 3 } },
            new Oracle.Shapes { Child = new Oracle.Leaf { Label = "root", Weight = 3 } });

        // an EMPTY nested message is present but has no fields: "{}" rather than omitted
        yield return ("nested empty",
            new Shapes { Child = new Leaf() },
            new Oracle.Shapes { Child = new Oracle.Leaf() });

        yield return ("repeated scalars",
            new Shapes { Many = { 1, 2, 3 }, Words = { "a", "", "c" } },
            new Oracle.Shapes { Many = { 1, 2, 3 }, Words = { "a", "", "c" } });

        yield return ("repeated messages and enums",
            new Shapes
            {
                Leaves = { new Leaf { Label = "x", Weight = 1 }, new Leaf() },
                Shades = { Shade.Green, Shade.Unknown, Shade.Blue },
            },
            new Oracle.Shapes
            {
                Leaves = { new Oracle.Leaf { Label = "x", Weight = 1 }, new Oracle.Leaf() },
                Shades = { Oracle.Shade.Green, Oracle.Shade.Unknown, Oracle.Shade.Blue },
            });

        // an empty collection is omitted, exactly as a default scalar is
        yield return ("empty collections", new Shapes { Many = { }, Words = { } }, new Oracle.Shapes());

        // map keys are ALWAYS strings in canonical JSON, whatever the schema says they are
        yield return ("maps",
            new Shapes
            {
                Tally = { ["one"] = 1, ["two"] = 2 },
                Names = { [7] = "seven", [-1] = "minus one" },
                Nested = { ["leaf"] = new Leaf { Label = "n", Weight = 9 } },
            },
            new Oracle.Shapes
            {
                Tally = { ["one"] = 1, ["two"] = 2 },
                Names = { [7] = "seven", [-1] = "minus one" },
                Nested = { ["leaf"] = new Oracle.Leaf { Label = "n", Weight = 9 } },
            });

        // a map entry holding a default value is still an entry - presence is the key's, not the
        // value's - so this must NOT be pruned the way a default field is
        yield return ("map with default values",
            new Shapes { Tally = { ["zero"] = 0 }, Nested = { ["empty"] = new Leaf() } },
            new Oracle.Shapes { Tally = { ["zero"] = 0 }, Nested = { ["empty"] = new Oracle.Leaf() } });

        yield return ("unicode and escaping",
            new Shapes { UserName = Tricky, Words = { Japanese, Control } },
            new Oracle.Shapes { UserName = Tricky, Words = { Japanese, Control } });

        yield return ("empty string is a default",
            new Shapes { UserName = "" },
            new Oracle.Shapes { UserName = "" });

        yield return ("extreme integers",
            new Shapes { Count = int.MinValue, BigNumber = long.MinValue, Unsigned = 0, BigUnsigned = 0 },
            new Oracle.Shapes { Count = int.MinValue, BigNumber = long.MinValue });

        yield return ("array, getter-only collection, present nullable",
            new Shapes { Tags = new[] { "a", "b" }, Fixed = { 4, 5 }, Maybe = 7 },
            new Oracle.Shapes { Tags = { "a", "b" }, Fixed = { 4, 5 }, Maybe = 7 });

        // a null nullable is absent on both sides, so this one does agree
        yield return ("absent nullable", new Shapes { Maybe = null }, new Oracle.Shapes());

        // the well-known types. A Timestamp is RFC 3339 with 0, 3, 6 or 9 fractional digits - the
        // fewest that represent the value - and a Duration is seconds with an "s" suffix under the
        // same rule. Both are nothing like what any C# serializer writes for these types
        yield return ("timestamp, whole second",
            Times(new DateTime(2026, 9, 14, 12, 30, 15, DateTimeKind.Utc), TimeSpan.FromSeconds(90)),
            OracleTimes(new DateTime(2026, 9, 14, 12, 30, 15, DateTimeKind.Utc), TimeSpan.FromSeconds(90)));

        yield return ("timestamp, milliseconds",
            Times(new DateTime(2026, 9, 14, 12, 30, 15, 250, DateTimeKind.Utc), TimeSpan.FromMilliseconds(1500)),
            OracleTimes(new DateTime(2026, 9, 14, 12, 30, 15, 250, DateTimeKind.Utc), TimeSpan.FromMilliseconds(1500)));

        // a tick is 100ns, so this lands on the six-digit form rather than three or nine
        yield return ("timestamp, microseconds",
            Times(new DateTime(2026, 9, 14, 12, 30, 15, DateTimeKind.Utc).AddTicks(1234560), TimeSpan.FromTicks(1234560)),
            OracleTimes(new DateTime(2026, 9, 14, 12, 30, 15, DateTimeKind.Utc).AddTicks(1234560), TimeSpan.FromTicks(1234560)));

        // 100ns is .NET's tick, and needs SEVEN fractional digits - so it can only be carried by the
        // nine-digit form. This is the case a six-digit formatter truncates, silently
        yield return ("timestamp, tick precision",
            Times(new DateTime(2026, 9, 14, 12, 30, 15, DateTimeKind.Utc).AddTicks(1234567), TimeSpan.FromTicks(1234567)),
            OracleTimes(new DateTime(2026, 9, 14, 12, 30, 15, DateTimeKind.Utc).AddTicks(1234567), TimeSpan.FromTicks(1234567)));

        yield return ("negative and zero durations",
            Times(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), TimeSpan.FromSeconds(-2.5)),
            OracleTimes(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), TimeSpan.FromSeconds(-2.5)));

        // a sub-second negative duration is the case where the sign lives only on the fraction
        yield return ("sub-second negative duration",
            Times(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc), TimeSpan.FromMilliseconds(-250)),
            OracleTimes(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc), TimeSpan.FromMilliseconds(-250)));

        yield return ("empty bytes",
            new Shapes { Blob = Array.Empty<byte>() },
            new Oracle.Shapes { Blob = ByteString.Empty });
    }

    private static Shapes Times(DateTime at, TimeSpan took)
        => new() { Times = new Temporal { At = at, Took = took } };

    private static Oracle.Shapes OracleTimes(DateTime at, TimeSpan took)
        => new()
        {
            Times = new Oracle.Temporal
            {
                At = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(at),
                Took = Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(took),
            },
        };

    // escaping is a place two writers can legitimately differ in TEXT while agreeing as JSON, which
    // is why the comparison is made over parsed trees; these are here to prove that holds
    private const string Tricky = "héllo \"world\"\n\t<&>";
    private const string Japanese = "日本語";
    private const string Control = "";
}
