using System.Text;
using System.Text.Json;
using Google.Protobuf;
using ProtoBuf.Connect;

namespace ProtoBuf.ConnectJsonDifferential;

/// <summary>
/// The breadth sweep: every scalar kind in every container, paired through the <b>binary</b> codec.
/// </summary>
/// <remarks>
/// The pairing is the interesting part. <c>Cases</c> builds two instances by hand, which is honest
/// but does not scale and cannot check anything about a value protobuf-net <em>derives</em> - a
/// level-300 <c>Guid</c> is a <c>string</c> in the schema, so a hand-written oracle would hold
/// whatever the author typed, and would confirm our JSON against a guess.
/// <para>
/// So here the protobuf-net instance is serialized to <b>binary protobuf</b> and parsed into the
/// protoc message. That makes the oracle hold exactly what protobuf-net thinks the message contains,
/// including every derived string form, and it costs one filler instead of two. It leans on the
/// binary codec - which is fair: that one is differentially verified against ref-emit across 3090
/// contracts, and a fault in it would show as a mismatch here rather than hide one.
/// </para>
/// </remarks>
internal static class Sweep
{
    public static void Run(JsonModel model, List<string> failures)
    {
        var serializer = ((IJsonModel)model).GetJsonSerializer<Wide>()
            ?? throw new InvalidOperationException(
                "no JSON serializer for Wide - the breadth fixture is refused, so nothing below is measured");

        foreach (var (name, value) in Values())
        {
            // the bridge: our binary bytes, read by protoc's parser
            var binary = new MemoryStream();
            model.Serialize(binary, value);
            WideOracle.Wide oracle;
            try
            {
                oracle = WideOracle.Wide.Parser.ParseFrom(binary.ToArray());
            }
            catch (Exception ex)
            {
                failures.Add($"[sweep/{name}] protoc could not parse our BINARY bytes: {ex.Message}");
                continue;
            }

            var expected = JsonFormatter.Default.Format(oracle);
            if (Environment.GetEnvironmentVariable("SWEEP_TRACE") == "1") Console.WriteLine($"  {name}: {expected}");
            var actual = Write(serializer, value);

            if (Json.Canonical(actual) != Json.Canonical(expected))
            {
                failures.Add($"[sweep/{name}] WRITE\n  ours:   {Json.Canonical(actual)}\n  google: {Json.Canonical(expected)}");
            }

            // and back: our reader over Google's JSON, re-emitted
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(expected));
            var round = serializer.Read(ref reader, null);
            var reEmitted = Write(serializer, round);
            if (Json.Canonical(reEmitted) != Json.Canonical(expected))
            {
                failures.Add($"[sweep/{name}] READ of Google's JSON lost something"
                    + $"\n  in:  {Json.Canonical(expected)}\n  out: {Json.Canonical(reEmitted)}");
            }

            // and Google parsing ours, which is the direction that proves our output is not merely
            // equivalent but actually accepted
            try
            {
                var parsed = WideOracle.Wide.Parser.ParseJson(actual);
                if (!parsed.Equals(oracle))
                {
                    failures.Add($"[sweep/{name}] Google parsed our JSON to a different message"
                        + $"\n  {parsed}\n  {oracle}");
                }
            }
            catch (Exception ex)
            {
                failures.Add($"[sweep/{name}] Google could not parse our JSON: {ex.Message}\n  {actual}");
            }
        }
    }

    public static int Count => Values().Count();

    private static string Write(IJsonSerializer<Wide> serializer, Wide value)
    {
        var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer)) serializer.Write(writer, value);
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>
    /// One case per cell, plus the values that are interesting <em>within</em> a cell.
    /// </summary>
    /// <remarks>
    /// Deliberately one member at a time rather than one fully-populated instance: a mismatch then
    /// names the member, where a single fat case would report one enormous diff for any of fifty
    /// causes. The last case populates everything at once anyway, to catch anything order-dependent.
    /// </remarks>
    private static IEnumerable<(string Name, Wide Value)> Values()
    {
        yield return ("empty", new Wide());

        // --- singular scalars, at a non-default value and at a boundary ------------------------
        yield return ("bool", new Wide { Bool = true });
        yield return ("int32", new Wide { Int32 = -12345 });
        yield return ("int32 min", new Wide { Int32 = int.MinValue });
        yield return ("int64", new Wide { Int64 = 9007199254740993L });
        yield return ("int64 min", new Wide { Int64 = long.MinValue });
        yield return ("uint32 max", new Wide { UInt32 = uint.MaxValue });
        yield return ("uint64 max", new Wide { UInt64 = ulong.MaxValue });
        yield return ("float", new Wide { Float = 1.5f });
        yield return ("float nan", new Wide { Float = float.NaN });
        yield return ("float +inf", new Wide { Float = float.PositiveInfinity });
        yield return ("float -inf", new Wide { Float = float.NegativeInfinity });
        yield return ("double", new Wide { Double = -2.25 });
        yield return ("double epsilon", new Wide { Double = double.Epsilon });
        yield return ("string", new Wide { String = "plain" });
        yield return ("string empty", new Wide { String = "" });
        yield return ("string tricky", new Wide { String = "héllo \"q\"\n\t<&>日本語" });
        yield return ("bytes", new Wide { Bytes = new byte[] { 0, 1, 250, 255 } });
        yield return ("bytes empty", new Wide { Bytes = Array.Empty<byte>() });
        yield return ("enum named", new Wide { Enum = Shade.Green });
        yield return ("enum zero", new Wide { Enum = Shade.Unknown });
        yield return ("enum unnamed", new Wide { Enum = (Shade)99 });
        yield return ("message", new Wide { Message = new Leaf { Label = "m", Weight = 2 } });
        yield return ("message empty", new Wide { Message = new Leaf() });

        // the narrow integers, which are int32/uint32 on the wire whatever C# calls them
        yield return ("sbyte", new Wide { SByte = -128 });
        yield return ("byte", new Wide { Byte = 255 });
        yield return ("int16", new Wide { Int16 = short.MinValue });
        yield return ("uint16", new Wide { UInt16 = ushort.MaxValue });

        // a char's JSON is the NUMBER, not the character
        yield return ("char", new Wide { Char = 'A' });
        yield return ("char high", new Wide { Char = '￿' });

        // int64/uint64 on the wire, so JSON strings
        yield return ("nint", new Wide { IntPtr = -42 });
        yield return ("nuint", new Wide { UIntPtr = 42 });

        yield return ("uri absolute", new Wide { Uri = new Uri("https://example.org/a?b=c#d") });
        yield return ("uri relative", new Wide { Uri = new Uri("/relative/path", UriKind.Relative) });

        // --- explicit presence ------------------------------------------------------------------
        yield return ("nullable int32 set", new Wide { NullableInt32 = 5 });
        yield return ("nullable bool set", new Wide { NullableBool = true });
        yield return ("nullable enum set", new Wide { NullableEnum = Shade.Blue });
        yield return ("nullable double set", new Wide { NullableDouble = 0.5 });
        yield return ("nullables absent", new Wide { NullableInt32 = null, NullableBool = null });

        // --- repeated ----------------------------------------------------------------------------
        yield return ("repeated bool", new Wide { RepeatedBool = { true, false, true } });
        yield return ("repeated int32", new Wide { RepeatedInt32 = { 1, 0, -1 } });
        yield return ("repeated int64", new Wide { RepeatedInt64 = { long.MaxValue, 0 } });
        yield return ("repeated uint32", new Wide { RepeatedUInt32 = { 0, uint.MaxValue } });
        yield return ("repeated uint64", new Wide { RepeatedUInt64 = { ulong.MaxValue } });
        yield return ("repeated float", new Wide { RepeatedFloat = { 1f, float.NaN } });
        yield return ("repeated double", new Wide { RepeatedDouble = { 0d, -1.5 } });
        yield return ("repeated string", new Wide { RepeatedString = { "a", "", "c" } });
        yield return ("repeated bytes", new Wide { RepeatedBytes = { new byte[] { 1 }, Array.Empty<byte>() } });
        yield return ("repeated enum", new Wide { RepeatedEnum = { Shade.Green, Shade.Unknown, (Shade)99 } });
        yield return ("repeated message", new Wide { RepeatedMessage = { new Leaf { Label = "x" }, new Leaf() } });

        // --- map keys, one per legal key type -----------------------------------------------------
        yield return ("key string", new Wide { KeyString = { ["a"] = 1, [""] = 2 } });
        yield return ("key int32", new Wide { KeyInt32 = { [0] = 1, [-7] = 2 } });
        yield return ("key int64", new Wide { KeyInt64 = { [long.MinValue] = 1 } });
        yield return ("key uint32", new Wide { KeyUInt32 = { [uint.MaxValue] = 1 } });
        yield return ("key uint64", new Wide { KeyUInt64 = { [ulong.MaxValue] = 1 } });

        // --- map values ---------------------------------------------------------------------------
        yield return ("value bool", new Wide { ValueBool = { ["k"] = true, ["f"] = false } });
        yield return ("value int64", new Wide { ValueInt64 = { ["k"] = long.MaxValue } });
        yield return ("value uint64", new Wide { ValueUInt64 = { ["k"] = ulong.MaxValue } });
        yield return ("value double", new Wide { ValueDouble = { ["k"] = double.NegativeInfinity } });
        yield return ("value string", new Wide { ValueString = { ["k"] = "", ["j"] = "v" } });
        yield return ("value bytes", new Wide { ValueBytes = { ["k"] = new byte[] { 9 } } });
        yield return ("value enum", new Wide { ValueEnum = { ["k"] = Shade.Blue, ["z"] = Shade.Unknown } });
        yield return ("value message", new Wide { ValueMessage = { ["k"] = new Leaf { Weight = 1 }, ["e"] = new Leaf() } });

        // --- the compatibility-level group at 300 --------------------------------------------------
        yield return ("timestamp", Temporal(t => t.Timestamp = new DateTime(2026, 9, 15, 8, 30, 0, DateTimeKind.Utc)));
        yield return ("timestamp millis", Temporal(t => t.Timestamp = new DateTime(2026, 9, 15, 8, 30, 0, 250, DateTimeKind.Utc)));
        yield return ("timestamp micros", Temporal(t => t.Timestamp = new DateTime(2026, 9, 15, 8, 30, 0, DateTimeKind.Utc).AddTicks(1234560)));
        yield return ("timestamp ticks", Temporal(t => t.Timestamp = new DateTime(2026, 9, 15, 8, 30, 0, DateTimeKind.Utc).AddTicks(1234567)));
        yield return ("timestamp epoch", Temporal(t => t.Timestamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        yield return ("duration whole", Temporal(t => t.Duration = TimeSpan.FromSeconds(90)));
        yield return ("duration millis", Temporal(t => t.Duration = TimeSpan.FromMilliseconds(1500)));
        yield return ("duration negative", Temporal(t => t.Duration = TimeSpan.FromSeconds(-2.5)));
        yield return ("duration sub-second negative", Temporal(t => t.Duration = TimeSpan.FromMilliseconds(-250)));
        yield return ("guid", Temporal(t => t.Guid = new Guid("0f8fad5b-d9cb-469f-a165-70867728950e")));
        yield return ("decimal", Temporal(t => t.Decimal = -12345.6789m));
        yield return ("decimal zero", Temporal(t => t.Decimal = 0m));
        yield return ("repeated timestamp", Temporal(t => t.RepeatedTimestamp.Add(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc))));
        yield return ("repeated guid", Temporal(t => t.RepeatedGuid.Add(Guid.Empty)));
        yield return ("value duration", Temporal(t => t.ValueDuration["k"] = TimeSpan.FromSeconds(3)));
        yield return ("nullable timestamp", Temporal(t => t.NullableTimestamp = new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc)));

        // everything at once, in case anything is order-dependent
        var all = new Wide
        {
            Bool = true, Int32 = 1, Int64 = 2, UInt32 = 3, UInt64 = 4, Float = 5f, Double = 6d,
            String = "s", Bytes = new byte[] { 7 }, Enum = Shade.Blue, Message = new Leaf { Label = "m" },
            SByte = -1, Byte = 2, Int16 = -3, UInt16 = 4, Char = 'z', IntPtr = -5, UIntPtr = 6,
            // note the nullables hold NON-default values here on purpose: a nullable holding its
            // type's default is the one place we knowingly differ from a canonical writer, and that
            // is pinned once in KnownDivergences rather than re-reported by every case that happens
            // to contain one
            Uri = new Uri("https://example.org/"), NullableInt32 = 8, NullableBool = true,
            NullableEnum = Shade.Green, NullableDouble = 9.5,
            RepeatedInt32 = { 1, 2 }, RepeatedString = { "a" }, RepeatedEnum = { Shade.Green },
            RepeatedMessage = { new Leaf { Weight = 1 } },
            KeyString = { ["a"] = 1 }, KeyInt32 = { [2] = 3 },
            ValueEnum = { ["e"] = Shade.Blue }, ValueMessage = { ["m"] = new Leaf() },
        };
        all.Temporal = new WideTemporal
        {
            Timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Duration = TimeSpan.FromSeconds(1),
            Guid = new Guid("0f8fad5b-d9cb-469f-a165-70867728950e"),
            Decimal = 1.25m,
        };
        yield return ("everything", all);
    }

    private static Wide Temporal(Action<WideTemporal> configure)
    {
        var temporal = new WideTemporal();
        configure(temporal);
        return new Wide { Temporal = temporal };
    }
}
