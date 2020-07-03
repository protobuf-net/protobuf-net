using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Meta;
using System;
using System.Globalization;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Schemas
{
    [Trait("kind", "well-known")]
    public class WellKnownTypes
    {

        static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void DateTime_NullStaysNull()
        {
            var orig = new HasDateTime();
            Assert.Null(orig.Value);

            var hd = ChangeType<HasDateTime, HasTimestamp>(runtime, orig);
            Assert.Null(hd.Value);
            var clone = ChangeType<HasTimestamp, HasDateTime>(runtime, hd);
            Assert.Null(clone.Value);

            hd = ChangeType<HasDateTime, HasTimestamp>(dynamicMethod, orig);
            Assert.Null(hd.Value);
            clone = ChangeType<HasTimestamp, HasDateTime>(dynamicMethod, hd);
            Assert.Null(clone.Value);

            hd = ChangeType<HasDateTime, HasTimestamp>(fullyCompiled, orig);
            Assert.Null(hd.Value);
            clone = ChangeType<HasTimestamp, HasDateTime>(fullyCompiled, hd);
            Assert.Null(clone.Value);
        }


        [Fact]
        public void TimeSpan_NullStaysNull()
        {
            var orig = new HasTimeSpan();
            Assert.Null(orig.Value);

            var hd = ChangeType<HasTimeSpan, HasDuration>(runtime, orig);
            Assert.Null(hd.Value);
            var clone = ChangeType<HasDuration, HasTimeSpan>(runtime, hd);
            Assert.Null(clone.Value);

            hd = ChangeType<HasTimeSpan, HasDuration>(dynamicMethod, orig);
            Assert.Null(hd.Value);
            clone = ChangeType<HasDuration, HasTimeSpan>(dynamicMethod, hd);
            Assert.Null(clone.Value);

            hd = ChangeType<HasTimeSpan, HasDuration>(fullyCompiled, orig);
            Assert.Null(hd.Value);
            clone = ChangeType<HasDuration, HasTimeSpan>(fullyCompiled, hd);
            Assert.Null(clone.Value);
        }

        [Theory]
        [InlineData("2017-01-15T01:30:15.01Z")]
        [InlineData("1970-01-15T00:00:00Z")]
        [InlineData("1930-01-15T00:00:00.00001Z")]
        public void DateTime_WellKnownEquiv_String(string s)
        {
            // parse
            var when = DateTime.Parse(s, CultureInfo.InvariantCulture);

            DateTime_WellKnownEquiv(runtime, when);
            DateTime_WellKnownEquiv(dynamicMethod, when);
            DateTime_WellKnownEquiv(fullyCompiled, when);
        }

        private void DateTime_WellKnownEquiv(TypeModel model, DateTime when)
        {
            var time = when - epoch;

            var seconds = time.TotalSeconds > 0 ? (long)Math.Floor(time.TotalSeconds) : Math.Ceiling(time.TotalSeconds);
            var nanos = (int)(((time.Ticks % TimeSpan.TicksPerSecond) * 1000000)
                / TimeSpan.TicksPerMillisecond);
            if(nanos < 0)
            {
                seconds--;
                nanos += 1000000000;
            }

            // convert forwards and compare
            var hazDt = new HasDateTime { Value = when };
            HasTimestamp hazTs;
            using (var ms = new MemoryStream())
            {
                model.Serialize(ms, hazDt);
                ms.Position = 0;
#pragma warning disable CS0618
                hazTs = (HasTimestamp)model.Deserialize(ms, null, typeof(HasTimestamp));
#pragma warning restore CS0618
            }
            Assert.Equal(seconds, hazTs.Value?.Seconds ?? 0);
            Assert.Equal(nanos, hazTs.Value?.Nanos ?? 0);

            // and back again
            hazDt = Serializer.ChangeType<HasTimestamp, HasDateTime>(hazTs);
            Assert.Equal(when, hazDt.Value);
        }

        private readonly TypeModel runtime, dynamicMethod, fullyCompiled;
        private WellKnownTypes()
        {
            static RuntimeTypeModel Create(bool autoCompile)
            {
                var model = RuntimeTypeModel.Create();
                model.AutoCompile = autoCompile;
                model.Add(typeof(HasDuration), true);
                model.Add(typeof(HasTimeSpan), true);
                model.Add(typeof(HasDateTime), true);
                model.Add(typeof(HasTimestamp), true);
                return model;
            }

            runtime = Create(false);

            var tmp = Create(true);
            tmp.CompileInPlace();
            dynamicMethod = tmp;
            fullyCompiled = tmp.Compile();
        }

        [Theory]
        [InlineData("00:12:13.00032")]
        [InlineData("-00:12:13.00032")]
        [InlineData("00:12:13.10032")]
        [InlineData("00:12:13")]
        [InlineData("-00:12:13")]
        [InlineData("00:00:00.00032")]
        [InlineData("-00:00:00.00032")]
        [InlineData("00:00:00")]
        public void TimeSpan_WellKnownEquiv_String(string s)
        {
            // parse
            var time = TimeSpan.Parse(s, CultureInfo.InvariantCulture);

            TimeSpan_WellKnownEquiv(runtime, time);
            TimeSpan_WellKnownEquiv(dynamicMethod, time);
            TimeSpan_WellKnownEquiv(fullyCompiled, time);
        }

        [Fact]
        public void Timestamp_ExpectedValues() // prove implementation matches official version
        {
            DateTime utcMin = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
            DateTime utcMax = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
            AssertKnownValue(new Timestamp { Seconds = -62135596800 }, utcMin);
            AssertKnownValue(new Timestamp { Seconds = 253402300799, Nanos = 999999900 }, utcMax);
            AssertKnownValue(new Timestamp(), new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            AssertKnownValue(new Timestamp { Nanos = 1000000 }, new DateTime(1970, 1, 1, 0, 0, 0, 1, DateTimeKind.Utc));
            AssertKnownValue(new Timestamp { Seconds = 3600 }, new DateTime(1970, 1, 1, 1, 0, 0, DateTimeKind.Utc));
            AssertKnownValue(new Timestamp { Seconds = -3600 }, new DateTime(1969, 12, 31, 23, 0, 0, DateTimeKind.Utc));
            AssertKnownValue(new Timestamp { Seconds = -1, Nanos = 999000000 }, new DateTime(1969, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc));
        }

        private void AssertKnownValue(Timestamp value, DateTime expected)
        {
            var obj = new HasTimestamp { Value = value };
            Assert.Equal(expected, ChangeType<HasTimestamp, HasDateTime>(runtime, obj).Value);
            Assert.Equal(expected, ChangeType<HasTimestamp, HasDateTime>(dynamicMethod, obj).Value);
            Assert.Equal(expected, ChangeType<HasTimestamp, HasDateTime>(fullyCompiled, obj).Value);

            var obj2 = new HasDateTime { Value = expected };
            var other = ChangeType<HasDateTime, HasTimestamp>(runtime, obj2).Value ?? new Timestamp();
            Assert.Equal(value.Seconds, other.Seconds);
            Assert.Equal(value.Nanos, other.Nanos);

            other = ChangeType<HasDateTime, HasTimestamp>(dynamicMethod, obj2).Value ?? new Timestamp();
            Assert.Equal(value.Seconds, other.Seconds);
            Assert.Equal(value.Nanos, other.Nanos);

            other = ChangeType<HasDateTime, HasTimestamp>(fullyCompiled, obj2).Value ?? new Timestamp();
            Assert.Equal(value.Seconds, other.Seconds);
            Assert.Equal(value.Nanos, other.Nanos);
        }

        private void AssertKnownValue(TimeSpan expected, Duration valueToSerialize, Duration valueToDeserialize = null)
        {
            if (valueToDeserialize == null)
                valueToDeserialize = valueToSerialize; // assume they are te same

            Log($"valueToSerialize: {valueToSerialize}, {valueToSerialize.Seconds} / {valueToSerialize.Nanos}");
            Log($"expected: {expected}");
            var obj = new HasDuration { Value = valueToSerialize };
            Assert.Equal(expected, ChangeType<HasDuration, HasTimeSpan>(runtime, obj).Value);
            Assert.Equal(expected, ChangeType<HasDuration, HasTimeSpan>(dynamicMethod, obj).Value);
            Assert.Equal(expected, ChangeType<HasDuration, HasTimeSpan>(fullyCompiled, obj).Value);

            var obj2 = new HasTimeSpan { Value = expected };
            Log($"obj2.Value: {obj2.Value}");
            var other = ChangeType<HasTimeSpan, HasDuration>(runtime, obj2).Value ?? new Duration();
            Log($"other: {other}, {other.Seconds} / {other.Nanos}");
            Assert.Equal(valueToDeserialize.Seconds, other.Seconds);
            Assert.Equal(valueToDeserialize.Nanos, other.Nanos);

            other = ChangeType<HasTimeSpan, HasDuration>(dynamicMethod, obj2).Value ?? new Duration();
            Assert.Equal(valueToDeserialize.Seconds, other.Seconds);
            Assert.Equal(valueToDeserialize.Nanos, other.Nanos);

            other = ChangeType<HasTimeSpan, HasDuration>(fullyCompiled, obj2).Value ?? new Duration();
            Assert.Equal(valueToDeserialize.Seconds, other.Seconds);
            Assert.Equal(valueToDeserialize.Nanos, other.Nanos);
        }
        private void Log(string message) => _log?.WriteLine(message);
        private readonly ITestOutputHelper _log;
        public WellKnownTypes(ITestOutputHelper log) : this() => _log = log;


        [Theory]
        [InlineData(0, 0, 0, "0A-00")] // nothing
        [InlineData(0.9, 0, 900000000, "0A-06-10-80-D2-93-AD-03")] // field 2 = 900000000
        [InlineData(1.0, 1, 0, "0A-02-08-01")] // field 1 = 1
        [InlineData(1.1, 1, 100000000, "0A-07-08-01-10-80-C2-D7-2F")] // field 1 = 1, field 2 = 100000000 
        [InlineData(-0.9, 0, -900000000, "0A-0B-10-80-AE-EC-D2-FC-FF-FF-FF-FF-01")] // field 2 = -900000000
        [InlineData(-1.0, -1, 0, "0A-0B-08-FF-FF-FF-FF-FF-FF-FF-FF-FF-01")] // field 1 = -1
        [InlineData(-1.1, -1, -100000000, "0A-16-08-FF-FF-FF-FF-FF-FF-FF-FF-FF-01-10-80-BE-A8-D0-FF-FF-FF-FF-FF-01")]
        // ^^ field 1 = -1, field 2 = -100000000
        public void TestDurationRepresentations(double valueSeconds, long seconds, int nanos, string expectedHex)
        {
            // from https://github.com/protocolbuffers/protobuf/blob/master/src/google/protobuf/duration.proto

            // Signed fractions of a second at nanosecond resolution of the span
            // of time. Durations less than one second are represented with a 0
            // `seconds` field and a positive or negative `nanos` field. For durations
            // of one second or more, a non-zero value for the `nanos` field must be
            // of the same sign as the `seconds` field. Must be from -999,999,999
            // to +999,999,999 inclusive.
            // int32 nanos = 2;
            using var ms = new MemoryStream();
            Serializer.Serialize<HasTimeSpan>(ms, new HasTimeSpan { Value = TimeSpan.FromSeconds(valueSeconds) });
            var actualHex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            ms.Position = 0;
            var dur = Serializer.Deserialize<HasDuration>(ms).Value;
            Assert.Equal(seconds, dur.Seconds);
            Assert.Equal(nanos, dur.Nanos);

            Assert.Equal(expectedHex, actualHex);

        }

        // prove implementation matches official version
        [Fact] public void Duration_ExpectedValues_1() => AssertKnownValue(TimeSpan.FromSeconds(1), new Duration { Seconds = 1 });
        [Fact] public void Duration_ExpectedValues_2() => AssertKnownValue(TimeSpan.FromSeconds(-1), new Duration { Seconds = -1 });
        [Fact] public void Duration_ExpectedValues_3() => AssertKnownValue(TimeSpan.FromMilliseconds(1), new Duration { Nanos = 1000000 });
        [Fact] public void Duration_ExpectedValues_4() => AssertKnownValue(TimeSpan.FromMilliseconds(-1), new Duration { Nanos = -1000000 });
        [Fact] public void Duration_ExpectedValues_5() => AssertKnownValue(TimeSpan.FromTicks(1), new Duration { Nanos = 100 });
        [Fact] public void Duration_ExpectedValues_6() => AssertKnownValue(TimeSpan.FromTicks(-1), new Duration { Nanos = -100 });
        [Fact] public void Duration_ExpectedValues_7() => AssertKnownValue(TimeSpan.FromTicks(2), new Duration { Nanos = 250 }, new Duration { Nanos = 200 });
        [Fact] public void Duration_ExpectedValues_8() => AssertKnownValue(TimeSpan.FromTicks(-2), new Duration { Nanos = -250 }, new Duration { Nanos = -200 });

        private void TimeSpan_WellKnownEquiv(TypeModel model, TimeSpan time)
        {

            var seconds = time.TotalSeconds > 0 ? (long)Math.Floor(time.TotalSeconds) : Math.Ceiling(time.TotalSeconds);
            var nanos = (int)(((time.Ticks % TimeSpan.TicksPerSecond) * 1000000)
               / TimeSpan.TicksPerMillisecond);

            // convert forwards and compare
            var hazTs = new HasTimeSpan { Value = time };
            var hazD = ChangeType<HasTimeSpan, HasDuration>(model, hazTs);
            Assert.Equal(seconds, hazD.Value?.Seconds ?? 0);
            Assert.Equal(nanos, hazD.Value?.Nanos ?? 0);

            // and back again
            hazTs = ChangeType<HasDuration, HasTimeSpan>(model, hazD);
            Assert.Equal(time, hazTs.Value);
        }
        static TTo ChangeType<TFrom, TTo>(TypeModel model, TFrom val)
        {
            using var ms = new MemoryStream();
            model.Serialize(ms, val);
            ms.Position = 0;
#pragma warning disable CS0618
            return (TTo)model.Deserialize(ms, null, typeof(TTo));
#pragma warning restore CS0618
        }
    }

    [ProtoContract]
    public class HasTimestamp
    {
        [ProtoMember(1)]
        public Google.Protobuf.WellKnownTypes.Timestamp Value { get; set; }
    }

    [ProtoContract]
    public class HasDateTime
    {
#pragma warning disable CS0618
        [ProtoMember(1, DataFormat = DataFormat.WellKnown)]
#pragma warning restore CS0618
        public DateTime? Value { get; set; }
    }

    [ProtoContract]
    public class HasDuration
    {
        [ProtoMember(1)]
        public Google.Protobuf.WellKnownTypes.Duration Value { get; set; }
    }

    [ProtoContract]
    public class HasTimeSpan
    {
#pragma warning disable CS0618
        [ProtoMember(1, DataFormat = DataFormat.WellKnown)]
#pragma warning restore CS0618
        public TimeSpan? Value { get; set; }
    }
}

// following is protogen's output of:
// https://raw.githubusercontent.com/google/protobuf/master/src/google/protobuf/timestamp.proto
// https://raw.githubusercontent.com/google/protobuf/master/src/google/protobuf/duration.proto

// This file was generated by a tool; you should avoid making direct changes.
// Consider using 'partial classes' to extend these types
// Input: my.proto

#pragma warning disable CS1591, CS0612

namespace Google.Protobuf.WellKnownTypes
{

    [global::ProtoBuf.ProtoContract(Name = @"Timestamp")]
    public partial class Timestamp
    {
        [global::ProtoBuf.ProtoMember(1, Name = @"seconds")]
        public long Seconds { get; set; }

        [global::ProtoBuf.ProtoMember(2, Name = @"nanos")]
        public int Nanos { get; set; }

    }

}

#pragma warning restore CS1591, CS0612

// This file was generated by a tool; you should avoid making direct changes.
// Consider using 'partial classes' to extend these types
// Input: my.proto

#pragma warning disable CS1591, CS0612

namespace Google.Protobuf.WellKnownTypes
{

    [global::ProtoBuf.ProtoContract(Name = @"Duration")]
    public partial class Duration
    {
        [global::ProtoBuf.ProtoMember(1, Name = @"seconds")]
        public long Seconds { get; set; }

        [global::ProtoBuf.ProtoMember(2, Name = @"nanos")]
        public int Nanos { get; set; }

    }

}

#pragma warning restore CS1591, CS0612
