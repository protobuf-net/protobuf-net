using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ProtoBuf.Test
{
    public class CompatibilityLevelListsMaps
    {
        [Fact]
        public void AssertVanillaListsSchema()
            => Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Test;
import ""google/protobuf/timestamp.proto"";
import ""google/protobuf/duration.proto"";

message HazLists {
   repeated .google.protobuf.Timestamp DateTimes = 1;
   repeated .google.protobuf.Duration TimeSpans = 2;
}
", Serializer.GetProto<HazLists>(ProtoSyntax.Proto3), ignoreLineEndingDifferences: true);

        [Fact]
        public void AssertVanillaListsPayload() // from 2.4 binary
        {
            var obj = new HazLists
            {
                DateTimes =
            {
                new DateTime(2020,05,31,0,0,0,DateTimeKind.Utc),
            },
                TimeSpans =
            {
                TimeSpan.FromMinutes(60)
            }
            };
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, obj);
            var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            Assert.Equal("0A-06-08-80-E7-CB-F6-05-12-03-08-90-1C", hex, ignoreCase: true);
        }

        // note: v2.4 actually gets this schema very wrong; it claims bcl.proto throughout
        // note: I think this schema is *also* wrong, will check when have binary
        [Fact]
        public void AssertVanillaMapsSchema()
            => Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Test;
import ""protobuf-net/bcl.proto""; // schema for protobuf-net's handling of core .NET types
import ""google/protobuf/timestamp.proto"";
import ""google/protobuf/duration.proto"";

message HazMaps {
   map<int32,.bcl.DateTime> DateTimeMapMarkedPropertyLevel = 1;
   map<int32,.google.protobuf.Timestamp> DateTimeMapMarkedViaMap = 2;
   repeated KeyValuePair_DateTime_DateTime DateTimeNotValidMapButMarked = 3;
   map<string,.bcl.TimeSpan> TimeSpanMapMarkedPropertyLevel = 4;
   map<string,.google.protobuf.Duration> TimeSpanMapMarkedViaMap = 5;
   repeated KeyValuePair_TimeSpan_TimeSpan TimeSpanNotValidMapButMarked = 6;
}
message KeyValuePair_DateTime_DateTime {
   .bcl.DateTime Key = 1;
   .bcl.DateTime Value = 2;
}
message KeyValuePair_TimeSpan_TimeSpan {
   .bcl.TimeSpan Key = 1;
   .bcl.TimeSpan Value = 2;
}
", Serializer.GetProto<HazMaps>(ProtoSyntax.Proto3), ignoreLineEndingDifferences: true);

        [Fact]
        public void AssertExpectedDateTimeValues()
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var date = new DateTime(2020, 05, 31, 0, 0, 0, DateTimeKind.Utc);
            var delta = date - epoch;

            Assert.Equal(18413, delta.TotalDays);
            Assert.Equal(1590883200, delta.TotalSeconds);
        }

        [Fact]
        public void AssertVanillaMapsPayload() // from 2.4 binary
        {
            var date = new DateTime(2020, 05, 31, 0, 0, 0, DateTimeKind.Utc);
            var time = TimeSpan.FromMinutes(60);
            var obj = new HazMaps
            {
                DateTimeMapMarkedPropertyLevel = { { 1, date } },
                DateTimeMapMarkedViaMap = { { 2, date } },
                DateTimeNotValidMapButMarked = { { date, date } },
                TimeSpanMapMarkedPropertyLevel = { { "a", time } },
                TimeSpanMapMarkedViaMap = { { "b", time } },
                TimeSpanNotValidMapButMarked = { { time, time } },
            };
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, obj);
            var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);

            Assert.Equal("0A-08-08-01-12-04-08-DA-9F-02-12-0A-08-02-12-06-08-80-E7-CB-F6-05-1A-0C-0A-04-08-DA-9F-02-12-04-08-DA-9F-02-22-09-0A-01-61-12-04-08-02-10-01-2A-08-0A-01-62-12-03-08-90-1C-32-0C-0A-04-08-02-10-01-12-04-08-02-10-01", hex, ignoreCase: true);
            /*
notes:
.bcl.DateTime is days by default, so: 18413
.google.protobuf.Timestamp is seconds, so: 1590883200
.bcl.TimeSpan allows hours, so: 1
.google.protobuf.Duration is seconds, so: 3600

// DateTimeMapMarkedPropertyLevel: map<int, .bcl.DateTime>
0A = field 1, type String
08 = length 8
payload = 08-01-12-04-08-DA-9F-02
  08 = field 1, type Variant
  01 = 1 (raw) or -1 (zigzag)
  12 = field 2, type String
  04 = length 4
  payload = 08-DA-9F-02
    08 = field 1, type Variant
    DA-9F-02 = 36826 (raw) or 18413 (zigzag)

// DateTimeMapMarkedViaMap: map<int, .google.protobuf.Timestamp>
12 = field 2, type String
0A = length 10
payload = 08-02-12-06-08-80-E7-CB-F6-05
  08 = field 1, type Variant
  02 = 2 (raw) or 1 (zigzag)
  12 = field 2, type String
  06 = length 6
  payload = 08-80-E7-CB-F6-05
    08 = field 1, type Variant
    80-E7-CB-F6-05 = 1590883200 (raw) or 795441600 (zigzag)

// DateTimeNotValidMapButMarked: map<.bcl.DateTime, .bcl.DateTime>
1A = field 3, type String
0C = length 12
payload = 0A-04-08-DA-9F-02-12-04-08-DA-9F-02
  0A = field 1, type String
  04 = length 4
  payload = 08-DA-9F-02
    08 = field 1, type Variant
    DA-9F-02 = 36826 (raw) or 18413 (zigzag)
  12 = field 2, type String
  04 = length 4
  payload = 08-DA-9F-02
    08 = field 1, type Variant
    DA-9F-02 = 36826 (raw) or 18413 (zigzag)

// TimeSpanMapMarkedPropertyLevel: map<string, .bcl.TimeSpan>
22 = field 4, type String
09 = length 9
payload = 0A-01-61-12-04-08-02-10-01
  0A = field 1, type String
  01 = length 1
  payload = 61
  UTF8: a
  12 = field 2, type String
  04 = length 4
  payload = 08-02-10-01
    08 = field 1, type Variant
    02 = 2 (raw) or 1 (zigzag)
    10 = field 2, type Variant
    01 = 1 (raw) or -1 (zigzag)

// TimeSpanMapMarkedViaMap: map<string, .google.protobuf.Duration>
2A = field 5, type String
08 = length 8
payload = 0A-01-62-12-03-08-90-1C
  0A = field 1, type String
  01 = length 1
  payload = 62
  UTF8: b
  12 = field 2, type String
  03 = length 3
  payload = 08-90-1C
    08 = field 1, type Variant
    90-1C = 3600 (raw) or 1800 (zigzag)

// TimeSpanNotValidMapButMarked: map<.bcl.TimeSpan, .bcl.TimeSpan>
32 = field 6, type String
0C = length 12
payload = 0A-04-08-02-10-01-12-04-08-02-10-01
  0A = field 1, type String
  04 = length 4
  payload = 08-02-10-01
    08 = field 1, type Variant
    02 = 2 (raw) or 1 (zigzag)
    10 = field 2, type Variant
    01 = 1 (raw) or -1 (zigzag)
  12 = field 2, type String
  04 = length 4
  payload = 08-02-10-01
    08 = field 1, type Variant
    02 = 2 (raw) or 1 (zigzag)
    10 = field 2, type Variant
    01 = 1 (raw) or -1 (zigzag)
*/
        }

#pragma warning disable CS0618
        [ProtoContract]
        public class HazLists
        {
            [ProtoMember(1, DataFormat = DataFormat.WellKnown)]
            public List<DateTime> DateTimes { get; } = new List<DateTime>();

            [ProtoMember(2, DataFormat = DataFormat.WellKnown)]
            public List<TimeSpan> TimeSpans { get; } = new List<TimeSpan>();
        }

        [ProtoContract]
        public class HazMaps
        {
            [ProtoMember(1, DataFormat = DataFormat.WellKnown)]
            public Dictionary<int, DateTime> DateTimeMapMarkedPropertyLevel { get; }
                = new Dictionary<int, DateTime>();

            [ProtoMember(2)]
            [ProtoMap(ValueFormat = DataFormat.WellKnown)]
            public Dictionary<int, DateTime> DateTimeMapMarkedViaMap { get; }
                = new Dictionary<int, DateTime>();

            [ProtoMember(3, DataFormat = DataFormat.WellKnown)]
            [ProtoMap(KeyFormat = DataFormat.WellKnown, ValueFormat = DataFormat.WellKnown)]
            public Dictionary<DateTime, DateTime> DateTimeNotValidMapButMarked { get; }
                = new Dictionary<DateTime, DateTime>();

            [ProtoMember(4, DataFormat = DataFormat.WellKnown)]
            public Dictionary<string, TimeSpan> TimeSpanMapMarkedPropertyLevel { get; }
            = new Dictionary<string, TimeSpan>();

            [ProtoMember(5)]
            [ProtoMap(ValueFormat = DataFormat.WellKnown)]
            public Dictionary<string, TimeSpan> TimeSpanMapMarkedViaMap { get; }
                = new Dictionary<string, TimeSpan>();

            [ProtoMember(6, DataFormat = DataFormat.WellKnown)]
            [ProtoMap(KeyFormat = DataFormat.WellKnown, ValueFormat = DataFormat.WellKnown)]
            public Dictionary<TimeSpan, TimeSpan> TimeSpanNotValidMapButMarked { get; }
                = new Dictionary<TimeSpan, TimeSpan>();
        }
#pragma warning restore CS0618
    }


}
