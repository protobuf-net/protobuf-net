using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
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
import ""google/protobuf/timestamp.proto"";
import ""google/protobuf/duration.proto"";

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
import ""google/protobuf/timestamp.proto"";
import ""google/protobuf/duration.proto"";

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
