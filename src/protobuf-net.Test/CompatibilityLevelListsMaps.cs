using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test
{
    public class CompatibilityLevelListsMaps
    {
        private readonly ITestOutputHelper _log;
        public CompatibilityLevelListsMaps(ITestOutputHelper log) => _log = log;
        private string Log(string message)
        {
            _log?.WriteLine(message);
            return message;
        }

        [Fact]
        public void AssertVanillaListsSchema()
            => Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Test;
import ""google/protobuf/duration.proto"";
import ""google/protobuf/timestamp.proto"";

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

            AssertPayload(obj, "0A-06-08-80-E7-CB-F6-05-12-03-08-90-1C");
        }

        // note: v2.4 actually gets this schema very wrong; it claims bcl.proto throughout
        // note: I think this schema is *also* wrong, will check when have binary
        [Fact]
        public void AssertVanillaMapsSchema()
            => Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Test;
import ""google/protobuf/duration.proto"";
import ""google/protobuf/timestamp.proto"";
import ""protobuf-net/bcl.proto""; // schema for protobuf-net's handling of core .NET types

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
", Log(Serializer.GetProto<HazMaps>(ProtoSyntax.Proto3)), ignoreLineEndingDifferences: true);

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
        public void AssertVanillaMapsPayload_DateTimeMapMarkedPropertyLevel()
        {
            var date = new DateTime(2020, 05, 31, 0, 0, 0, DateTimeKind.Utc);
            var obj = new HazMaps
            {
                DateTimeMapMarkedPropertyLevel = { { 1, date } },
            };
            AssertPayload(obj, "0A-08-08-01-12-04-08-DA-9F-02");
        }

        [Fact]
        public void AssertVanillaMapsPayload_DateTimeMapMarkedViaMap()
        {
            var date = new DateTime(2020, 05, 31, 0, 0, 0, DateTimeKind.Utc);
            var obj = new HazMaps
            {
                DateTimeMapMarkedViaMap = { { 2, date } },
            };
            AssertPayload(obj, "12-0A-08-02-12-06-08-80-E7-CB-F6-05");
        }

        [Fact]
        public void AssertVanillaMapsPayload_DateTimeNotValidMapButMarked()
        {
            var date = new DateTime(2020, 05, 31, 0, 0, 0, DateTimeKind.Utc);
            var obj = new HazMaps
            {
                DateTimeNotValidMapButMarked = { { date, date } },
            };
            AssertPayload(obj, "1A-0C-0A-04-08-DA-9F-02-12-04-08-DA-9F-02");
        }

        [Fact]
        public void AssertVanillaMapsPayload_TimeSpanMapMarkedPropertyLevel()
        {
            var time = TimeSpan.FromMinutes(60);
            var obj = new HazMaps
            {
                TimeSpanMapMarkedPropertyLevel = { { "a", time } },
            };
            AssertPayload(obj, "22-09-0A-01-61-12-04-08-02-10-01");
        }

        [Fact]
        public void AssertVanillaMapsPayload_TimeSpanMapMarkedViaMap()
        {
            var time = TimeSpan.FromMinutes(60);
            var obj = new HazMaps
            {
                TimeSpanMapMarkedViaMap = { { "b", time } },
            };
            AssertPayload(obj, "2A-08-0A-01-62-12-03-08-90-1C");
        }

        [Fact]
        public void AssertVanillaMapsPayload_TimeSpanNotValidMapButMarked()
        {
            var time = TimeSpan.FromMinutes(60);
            var obj = new HazMaps
            {
                TimeSpanNotValidMapButMarked = { { time, time } },
            };
            AssertPayload(obj, "32-0C-0A-04-08-02-10-01-12-04-08-02-10-01");
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

            AssertPayload(obj, "0A-08-08-01-12-04-08-DA-9F-02-12-0A-08-02-12-06-08-80-E7-CB-F6-05-1A-0C-0A-04-08-DA-9F-02-12-04-08-DA-9F-02-22-09-0A-01-61-12-04-08-02-10-01-2A-08-0A-01-62-12-03-08-90-1C-32-0C-0A-04-08-02-10-01-12-04-08-02-10-01");
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



        [ProtoContract]
        public class HazGuids
        {
            [ProtoMember(1)]
            public List<Guid> Guids { get; } = new List<Guid>();
        }

        [ProtoContract]
        [CompatibilityLevel(CompatibilityLevel.Level300)]
        public class HazGuids300
        {
            [ProtoMember(1)]
            public List<Guid> Guids { get; } = new List<Guid>();
        }

        [ProtoContract]
        [CompatibilityLevel(CompatibilityLevel.Level300)]
        public class HazGuids300Fixed
        {
            [ProtoMember(1, DataFormat = DataFormat.FixedSize)]
            public List<Guid> Guids { get; } = new List<Guid>();
        }

        [Fact]
        public void VanillaHazGuidsSchema()
            => Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Test;
import ""protobuf-net/bcl.proto""; // schema for protobuf-net's handling of core .NET types

message HazGuids {
   repeated .bcl.Guid Guids = 1;
}
", Serializer.GetProto<HazGuids>(ProtoSyntax.Proto3), ignoreLineEndingDifferences: true);

        [Fact]
        public void Level300HazGuidsSchema()
            => Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Test;

message HazGuids300 {
   repeated string Guids = 1;
}
", Serializer.GetProto<HazGuids300>(ProtoSyntax.Proto3), ignoreLineEndingDifferences: true);

        [Fact]
        public void Level300FixedHazGuidsSchema()
            => Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Test;

message HazGuids300Fixed {
   repeated bytes Guids = 1;
}
", Serializer.GetProto<HazGuids300Fixed>(ProtoSyntax.Proto3), ignoreLineEndingDifferences: true);


        private static readonly Guid s_KnownGuid = Guid.Parse("c416e4af-455e-414c-948c-f27873263547");

        [Fact]
        public void VanillaHazGuidsPayload() => AssertPayload(new HazGuids { Guids = { s_KnownGuid } }, "0A-12-09-AF-E4-16-C4-5E-45-4C-41-11-94-8C-F2-78-73-26-35-47");
        /*
0A = field 1, type String
12 = length 18
payload = 09-AF-E4-16-C4-5E-45-4C-41-11-94-8C-F2-78-73-26-35-47
  09 = field 1, type Fixed64
  payload = AF-E4-16-C4-5E-45-4C-41
  11 = field 2, type Fixed64
  payload = -8C-F2-78-73-26-35-47
        */

        [Fact]
        public void Level300HazGuidsPayload() => AssertPayload(new HazGuids300 { Guids = { s_KnownGuid } }, "0A-24-63-34-31-36-65-34-61-66-2D-34-35-35-65-2D-34-31-34-63-2D-39-34-38-63-2D-66-32-37-38-37-33-32-36-33-35-34-37");
        /*
0A = field 1, type String
24 = length 36
payload = 63-34-31-36-65-34-61-66-2D-34-35-35-65-2D-34-31-34-63-2D-39-34-38-63-2D-66-32-37-38-37-33-32-36-33-35-34-37
UTF8: c416e4af-455e-414c-948c-f27873263547
        */

        [Fact]
        public void Level300FixedHazGuidsPayload() => AssertPayload(new HazGuids300Fixed { Guids = { s_KnownGuid } }, "0A-10-C4-16-E4-AF-45-5E-41-4C-94-8C-F2-78-73-26-35-47");
        /*
0A = field 1, type String
10 = length 16
payload = C4-16-E4-AF-45-5E-41-4C-94-8C-F2-78-73-26-35-47
         */

        void AssertPayload<T>(T message, string expectedHex)
        {
            var model = RuntimeTypeModel.Create(typeof(T).Name);
            model.AutoCompile = false;
            AssertImpl(model);
            model.CompileInPlace();
            AssertImpl(model);
            AssertImpl(model.Compile());

            void AssertImpl(TypeModel serializer)
            {
                using var ms = new MemoryStream();
                serializer.Serialize(ms, message);
                var actualHex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
                Log(actualHex);
                Assert.Equal(expectedHex, actualHex, ignoreCase: true);
            }
        }


        [ProtoContract]
        public class HazDecimals
        {
            [ProtoMember(1)]
            public List<decimal> Values { get; } = new List<decimal>();
        }

        [ProtoContract]
        [CompatibilityLevel(CompatibilityLevel.Level300)]
        public class HazDecimals300
        {
            [ProtoMember(1)]
            public List<decimal> Values { get; } = new List<decimal>();
        }

        [Fact]
        public void VanillaHazDecimalsSchema()
            => Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Test;
import ""protobuf-net/bcl.proto""; // schema for protobuf-net's handling of core .NET types

message HazDecimals {
   repeated .bcl.Decimal Values = 1;
}
", Serializer.GetProto<HazDecimals>(ProtoSyntax.Proto3), ignoreLineEndingDifferences: true);

        [Fact]
        public void Level300HazDecimalsSchema()
            => Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Test;

message HazDecimals300 {
   repeated string Values = 1;
}
", Serializer.GetProto<HazDecimals300>(ProtoSyntax.Proto3), ignoreLineEndingDifferences: true);

        [Fact]
        public void VanillaHazDecimalsPayload() => AssertPayload(new HazDecimals { Values = { 12354.1451M } }, "0A-07-08-CB-AF-F4-3A-18-08");
        /*
0A = field 1, type String
07 = length 7
payload = 08-CB-AF-F4-3A-18-08
  08 = field 1, type Variant
  CB-AF-F4-3A = 123541451 (raw) or -61770726 (zigzag)
  18 = field 3, type Variant
  08 = 8 (raw) or 4 (zigzag)
        */

        [Fact]
        public void Level300HazDecimalsPayload() => AssertPayload(new HazDecimals300 { Values = { 12354.1451M } }, "0A-0A-31-32-33-35-34-2E-31-34-35-31");
        /*
0A = field 1, type String
0A = length 10
payload = 31-32-33-35-34-2E-31-34-35-31
UTF8: 12354.1451
        */

        [Fact]
        public void HazSingleSignedPayload()
            => AssertPayload(new HazSingleSigned { Value = -42 }, "08-53");

        [Fact]
        public void HazSingleSignedSchema()
            => Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Test;

message HazSingleSigned {
   sint32 Value = 1;
}
", Log(Serializer.GetProto<HazSingleSigned>(ProtoSyntax.Proto3)), ignoreLineEndingDifferences: true);

        [ProtoContract]
        public class HazSingleSigned
        {
            [ProtoMember(1, DataFormat = DataFormat.ZigZag)]
            public int Value { get; set; }
        }

        [Fact]
        public void HazMultipleSignedPayload()
            => AssertPayload(new HazMultipleSigned { Values = { -42 } }, "08-53");

        [Fact]
        public void HazMultipleSignedSchema()
            => Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Test;

message HazMultipleSigned {
   repeated sint32 Values = 1;
}
", Log(Serializer.GetProto<HazMultipleSigned>(ProtoSyntax.Proto3)), ignoreLineEndingDifferences: true);

        [ProtoContract]
        public class HazMultipleSigned
        {
            [ProtoMember(1, DataFormat = DataFormat.ZigZag, IsPacked = true)]
            public List<int> Values { get; } = new List<int>();
        }
    }


}