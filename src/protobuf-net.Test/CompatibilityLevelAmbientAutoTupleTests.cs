using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test
{
    public class CompatibilityLevelAmbientAutoTupleTests
    {
        public CompatibilityLevelAmbientAutoTupleTests(ITestOutputHelper log)
            => _log = log;
        private readonly ITestOutputHelper _log;
        private void Log(string message) => _log?.WriteLine(message);

        [Fact]
        public void VanillaSchema() => AssertSchema<Vanilla>(@"
syntax = ""proto3"";
import ""protobuf-net/bcl.proto""; // schema for protobuf-net's handling of core .NET types

message KeyValuePair_Guid_ValueTuple_DateTime_Decimal {
   .bcl.Guid Key = 1;
   ValueTuple_DateTime_Decimal Value = 2;
}
message ValueTuple_DateTime_Decimal {
   .bcl.DateTime Item1 = 1;
   .bcl.Decimal Item2 = 2;
}
message ValueTuple_DateTime_TimeSpan {
   .bcl.DateTime Item1 = 1;
   .bcl.TimeSpan Item2 = 2;
}
message ValueTuple_Guid_Decimal {
   .bcl.Guid Item1 = 1;
   .bcl.Decimal Item2 = 2;
}
message Vanilla {
   ValueTuple_DateTime_TimeSpan Value = 1;
   repeated ValueTuple_Guid_Decimal List = 2;
   repeated KeyValuePair_Guid_ValueTuple_DateTime_Decimal Map = 3;
}");

        [ProtoContract]
        public class Vanilla
        {
            [ProtoMember(1)]
            public (DateTime, TimeSpan) Value { get; set; }

            [ProtoMember(2)]
            public List<(Guid, decimal)> List { get; } = new List<(Guid, decimal)>();

            [ProtoMember(3)]
            public Dictionary<Guid, (DateTime, decimal)> Map { get; } = new Dictionary<Guid, (DateTime, decimal)>();
        }

        [Fact]
        public void Level300Schema() => AssertSchema<Level300>(@"
syntax = ""proto3"";
import ""google/protobuf/duration.proto"";
import ""google/protobuf/timestamp.proto"";

message Level300 {
   ValueTuple_DateTime_TimeSpan Value = 1;
   repeated ValueTuple_Guid_Decimal List = 2;
   map<string,ValueTuple_DateTime_Decimal> Map = 3;
}
message ValueTuple_DateTime_Decimal {
   .google.protobuf.Timestamp Item1 = 1;
   string Item2 = 2;
}
message ValueTuple_DateTime_TimeSpan {
   .google.protobuf.Timestamp Item1 = 1;
   .google.protobuf.Duration Item2 = 2;
}
message ValueTuple_Guid_Decimal {
   string Item1 = 1;
   string Item2 = 2;
}");

        [CompatibilityLevel(CompatibilityLevel.Level300)]
        [ProtoContract]
        public class Level300
        {
            [ProtoMember(1)]
            public (DateTime, TimeSpan) Value { get; set; }

            [ProtoMember(2)]
            public List<(Guid, decimal)> List { get; } = new List<(Guid, decimal)>();

            [ProtoMember(3)]
            public Dictionary<Guid, (DateTime, decimal)> Map { get; } = new Dictionary<Guid, (DateTime, decimal)>();
        }

        [Fact]
        public void Level300Payload_CheckActual()
        {

            var date = new DateTime(2020, 6, 5, 21, 06, 31, DateTimeKind.Utc);
            var time = TimeSpan.FromSeconds(4156);
            var guid = Guid.Parse("5a0960eb-ab45-4d0b-98fd-aaebd9129430");
            var price = 123.45M;
            var equivObj = new Level300
            {
                Value = (date, time),
                List = { (guid, price) },
                Map = { { guid, (date, price) } },
            };
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, equivObj);
            var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            ms.SetLength(0);
            Log(hex);
            Assert.Equal("0A-0D-0A-06-08-D7-E7-EA-F6-05-12-03-08-BC-20-12-2E-0A-24-35-61-30-39-36-30-65-62-2D-"
                       + "61-62-34-35-2D-34-64-30-62-2D-39-38-66-64-2D-61-61-65-62-64-39-31-32-39-34-33-30-12-"
                       + "06-31-32-33-2E-34-35-1A-38-0A-24-35-61-30-39-36-30-65-62-2D-61-62-34-35-2D-34-64-30-"
                       + "62-2D-39-38-66-64-2D-61-61-65-62-64-39-31-32-39-34-33-30-12-10-0A-06-08-D7-E7-EA-F6-"
                       + "05-12-06-31-32-33-2E-34-35", hex);
        }

        [Fact]
        public void Level300Payload_CheckExpected()
        {

            WellKnownTypes.Timestamp date = new DateTime(2020, 6, 5, 21, 06, 31, DateTimeKind.Utc);
            WellKnownTypes.Duration time = TimeSpan.FromSeconds(4156);
            string guid = "5a0960eb-ab45-4d0b-98fd-aaebd9129430", price = "123.45";
            var equivObj = new Level300_Equivalent
            {
                Value = (date, time),
                List = { (guid, price)},
                Map = { { guid, (date, price) } },
            };
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, equivObj);
            var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            ms.SetLength(0);
            Log(hex);
            Assert.Equal("0A-0D-0A-06-08-D7-E7-EA-F6-05-12-03-08-BC-20-12-2E-0A-24-35-61-30-39-36-30-65-62-2D-"
                       + "61-62-34-35-2D-34-64-30-62-2D-39-38-66-64-2D-61-61-65-62-64-39-31-32-39-34-33-30-12-"
                       + "06-31-32-33-2E-34-35-1A-38-0A-24-35-61-30-39-36-30-65-62-2D-61-62-34-35-2D-34-64-30-"
                       + "62-2D-39-38-66-64-2D-61-61-65-62-64-39-31-32-39-34-33-30-12-10-0A-06-08-D7-E7-EA-F6-"
                       + "05-12-06-31-32-33-2E-34-35", hex);
        }

        [Fact]
        public void Level300Payload_EquivSchema() => AssertSchema<Level300_Equivalent>(@"
syntax = ""proto3"";
import ""google/protobuf/duration.proto"";
import ""google/protobuf/timestamp.proto"";

message Level300_Equivalent {
   ValueTuple_Timestamp_Duration Value = 1;
   repeated ValueTuple_String_String List = 2;
   map<string,ValueTuple_Timestamp_String> Map = 3;
}
message ValueTuple_String_String {
   string Item1 = 1;
   string Item2 = 2;
}
message ValueTuple_Timestamp_Duration {
   .google.protobuf.Timestamp Item1 = 1;
   .google.protobuf.Duration Item2 = 2;
}
message ValueTuple_Timestamp_String {
   .google.protobuf.Timestamp Item1 = 1;
   string Item2 = 2;
}");

        [CompatibilityLevel(CompatibilityLevel.Level300)]
        [ProtoContract]
        public class Level300_Equivalent
        {
            [ProtoMember(1)]
            public (WellKnownTypes.Timestamp, WellKnownTypes.Duration) Value { get; set; }

            [ProtoMember(2)]
            public List<(string, string)> List { get; } = new List<(string, string)>();

            [ProtoMember(3)]
            public Dictionary<string, (WellKnownTypes.Timestamp, string)> Map { get; } = new Dictionary<string, (WellKnownTypes.Timestamp, string)>();
        }

        private void AssertSchema<T>(string expected)
        {
            var model = RuntimeTypeModel.Create();
            model.Add<T>();
            var schema = model.GetSchema(typeof(T), ProtoSyntax.Proto3);
            Log(schema);
            Assert.Equal(expected.Trim(), schema.Trim(), ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void DetectInvalidModel()
        {
            var model = RuntimeTypeModel.Create();
            model.Add<HybridInvalidModel>();
            var ex = Assert.Throws<InvalidOperationException>(() => model.GetSchema(typeof(HybridInvalidModel)));
            Assert.Equal("The tuple-like type System.ValueTuple`2[System.DateTime,System.TimeSpan] must use a single compatiblity level, but 'Level200' and 'Level300' are both observed; this usually means it is being used in different contexts in the same model.", ex.Message);
        }

        [ProtoContract]
        public class HybridInvalidModel
        {
            [ProtoMember(1)]
            [CompatibilityLevel(CompatibilityLevel.Level300)]
            public (DateTime, TimeSpan) Value { get; set; }

            [ProtoMember(2)]
            public List<(DateTime, TimeSpan)> List { get; } = new List<(DateTime, TimeSpan)>();
        }

        [ProtoContract]
        public class HybridValidModel
        {
            [ProtoMember(1)]
            [CompatibilityLevel(CompatibilityLevel.Level300)]
            public (DateTime, TimeSpan) Value { get; set; }

            [ProtoMember(2)]
            [CompatibilityLevel(CompatibilityLevel.Level300)]
            public List<(DateTime, TimeSpan)> List { get; } = new List<(DateTime, TimeSpan)>();
        }

        [Fact]
        public void HybridValidModelSchema() => AssertSchema<HybridValidModel>(@"
syntax = ""proto3"";
import ""google/protobuf/duration.proto"";
import ""google/protobuf/timestamp.proto"";

message HybridValidModel {
   ValueTuple_DateTime_TimeSpan Value = 1;
   repeated ValueTuple_DateTime_TimeSpan List = 2;
}
message ValueTuple_DateTime_TimeSpan {
   .google.protobuf.Timestamp Item1 = 1;
   .google.protobuf.Duration Item2 = 2;
}");
    }
}
