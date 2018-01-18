using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Meta;
using System;
using System.Globalization;
using System.IO;
using Xunit;

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
            var hazTs = Serializer.ChangeType<HasDateTime, HasTimestamp>(hazDt);
            Assert.Equal(seconds, hazTs.Value?.Seconds ?? 0);
            Assert.Equal(nanos, hazTs.Value?.Nanos ?? 0);

            // and back again
            hazDt = Serializer.ChangeType<HasTimestamp, HasDateTime>(hazTs);
            Assert.Equal(when, hazDt.Value);
        }

        private TypeModel runtime, dynamicMethod, fullyCompiled;
        public WellKnownTypes()
        {
            RuntimeTypeModel Create(bool autoCompile)
            {
                var model = TypeModel.Create();
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

            var obj = new HasDuration { Value = valueToSerialize };
            Assert.Equal(expected, ChangeType<HasDuration, HasTimeSpan>(runtime, obj).Value);
            Assert.Equal(expected, ChangeType<HasDuration, HasTimeSpan>(dynamicMethod, obj).Value);
            Assert.Equal(expected, ChangeType<HasDuration, HasTimeSpan>(fullyCompiled, obj).Value);

            var obj2 = new HasTimeSpan { Value = expected };
            var other = ChangeType<HasTimeSpan, HasDuration>(runtime, obj2).Value ?? new Duration();
            Assert.Equal(valueToDeserialize.Seconds, other.Seconds);
            Assert.Equal(valueToDeserialize.Nanos, other.Nanos);

            other = ChangeType<HasTimeSpan, HasDuration>(dynamicMethod, obj2).Value ?? new Duration();
            Assert.Equal(valueToDeserialize.Seconds, other.Seconds);
            Assert.Equal(valueToDeserialize.Nanos, other.Nanos);

            other = ChangeType<HasTimeSpan, HasDuration>(fullyCompiled, obj2).Value ?? new Duration();
            Assert.Equal(valueToDeserialize.Seconds, other.Seconds);
            Assert.Equal(valueToDeserialize.Nanos, other.Nanos);
        }

        [Fact]
        public void Duration_ExpectedValues() // prove implementation matches official version
        {
            AssertKnownValue(TimeSpan.FromSeconds(1), new Duration { Seconds = 1 });
            AssertKnownValue(TimeSpan.FromSeconds(-1), new Duration { Seconds = -1 });
            AssertKnownValue(TimeSpan.FromMilliseconds(1), new Duration { Nanos = 1000000 });
            AssertKnownValue(TimeSpan.FromMilliseconds(-1), new Duration { Nanos = -1000000 });
            AssertKnownValue(TimeSpan.FromTicks(1), new Duration { Nanos = 100 });
            AssertKnownValue(TimeSpan.FromTicks(-1), new Duration { Nanos = -100 });
            AssertKnownValue(TimeSpan.FromTicks(2), new Duration { Nanos = 250 }, new Duration { Nanos = 200 });
            AssertKnownValue(TimeSpan.FromTicks(-2), new Duration { Nanos = -250 }, new Duration { Nanos = -200 });
        }

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
            using (var ms = new MemoryStream())
            {
                model.Serialize(ms, val);
                ms.Position = 0;
                return (TTo)model.Deserialize(ms, null, typeof(TTo));
            }
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
        [ProtoMember(1, DataFormat = DataFormat.WellKnown)]
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
        [ProtoMember(1, DataFormat = DataFormat.WellKnown)]
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
