using ProtoBuf.Meta;
using ProtoBuf.unittest;
using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test
{
    public class NodaTimeTests
    {
        static RuntimeTypeModel CreateModel()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            return model.AddNodaTimeSurrogates();
        }
        [Theory]
        [InlineData(typeof(NodaTime.Duration), typeof(WellKnownTypes.Duration))]
        [InlineData(typeof(NodaTime.Instant), typeof(WellKnownTypes.Timestamp))]
        public void CanRegisterTypes(Type modelType, Type surrogateType)
        {
            var model = CreateModel();
            Assert.True(model.IsDefined(modelType));
            var metaType = model[modelType];
            Assert.True(metaType.HasSurrogate);
            Assert.Equal(surrogateType, metaType.GetSurrogateOrBaseOrSelf(false).Type);
        }

        private readonly ITestOutputHelper _log;
        public NodaTimeTests(ITestOutputHelper log) => _log = log;
        private string Log(string message)
        {
            _log?.WriteLine(message);
            return message;
        }

        [Fact]
        public void SchemaWorksThroughSurrogateDuration()
        {
            var model = CreateModel();
            var schema = model.GetSchema(typeof(HazNodaTimeDuration), ProtoSyntax.Proto3);
            Assert.Equal(@"syntax = ""proto3"";
import ""google/protobuf/duration.proto"";

message HazNodaTimeDuration {
   int32 Id = 1;
   .google.protobuf.Duration Time = 2;
   string Name = 3;
}
", Log(schema), ignoreLineEndingDifferences: true);
        }


        [Fact]
        public void SchemaWorksThroughSurrogateTimestamp()
        {
            var model = CreateModel();
            var schema = model.GetSchema(typeof(HazNodaTimeInstant), ProtoSyntax.Proto3);
            Assert.Equal(@"syntax = ""proto3"";
import ""google/protobuf/timestamp.proto"";

message HazNodaTimeInstant {
   int32 Id = 1;
   .google.protobuf.Timestamp Time = 2;
   string Name = 3;
}
", Log(schema), ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void CanRoundTripValueWithDuration()
        {
            var model = CreateModel();

            TestRoundTrip(this, model); // runtime only
            model.CompileInPlace();
            TestRoundTrip(this, model); // locally compiled

            TestRoundTrip(this, model.Compile()); // fully compiled in-proc

#if !PLAT_NO_EMITDLL
            TestRoundTrip(this, model.CompileAndVerify()); // fully compiled on disk
#endif
            static void TestRoundTrip(NodaTimeTests tests, TypeModel model)
            {
                var duration = NodaTime.Duration.FromTimeSpan(new TimeSpan(42, 1, 10, 12, 451));
                var obj = new HazNodaTimeDuration { Id = 42, Name = "abc", Time = duration };
                using var ms = new MemoryStream();
                model.Serialize(ms, obj);
                var hex = tests.Log(BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length));
                ms.Position = 0;
                var clone = model.Deserialize<HazNodaTimeDuration>(ms);
                Assert.NotSame(obj, clone);
                Assert.Equal(obj.Id, clone.Id);
                Assert.Equal(obj.Name, clone.Name);
                Assert.Equal(obj.Time, clone.Time);
            }
        }

        [Fact]
        public void CanRoundTripValueWithTimestamp()
        {
            var model = CreateModel();

            TestRoundTrip(this, model); // runtime only
            model.CompileInPlace();
            TestRoundTrip(this, model); // locally compiled

            TestRoundTrip(this, model.Compile()); // fully compiled in-proc

#if !PLAT_NO_EMITDLL
            TestRoundTrip(this, model.CompileAndVerify()); // fully compiled on disk
#endif
            static void TestRoundTrip(NodaTimeTests tests, TypeModel model)
            {
                var when = NodaTime.Instant.FromDateTimeUtc(new DateTime(2020, 8, 23, 8, 51, 12, 451, DateTimeKind.Utc));
                var obj = new HazNodaTimeInstant { Id = 42, Name = "abc", Time = when };
                using var ms = new MemoryStream();
                model.Serialize(ms, obj);
                var hex = tests.Log(BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length));
                ms.Position = 0;
                var clone = model.Deserialize<HazNodaTimeInstant>(ms);
                Assert.NotSame(obj, clone);
                Assert.Equal(obj.Id, clone.Id);
                Assert.Equal(obj.Name, clone.Name);
                Assert.Equal(obj.Time, clone.Time);
            }
        }
        
        [Fact]
        public void AssertBytesFromTimeSpanModel()
        {
            var duration = new TimeSpan(42, 1, 10, 12, 451);
            var obj = new HazTimeSpanDuration { Id = 42, Name = "abc", Time = duration };
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, obj);

            // this is the same output as noted in TestExpectedBinaryOutput
            Assert.Equal("08-2A-12-0B-08-F4-DE-DD-01-10-C0-ED-86-D7-01-1A-03-61-62-63", Log(BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length)));
        }
        
        [Fact]
        public void AssertBytesFromDateTimeModel()
        {
            var value = new DateTime(2020, 8, 23, 8, 51, 12, 451, DateTimeKind.Utc);
            var obj = new HazDateTimeTimestamp { Id = 42, Name = "abc", Time = value };
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, obj);

            // this is the same output as noted in TestExpectedBinaryOutput
            Assert.Equal("08-2A-12-0C-08-80-DC-88-FA-05-10-C0-ED-86-D7-01-1A-03-61-62-63", Log(BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length)));
        }

        [ProtoContract]
        [CompatibilityLevel(CompatibilityLevel.Level300)] // uses Duration format
        public class HazTimeSpanDuration
        {
            [ProtoMember(1)]
            public int Id { get; set; }

            [ProtoMember(2)]
            public TimeSpan Time { get; set; }

            [ProtoMember(3)]
            public string Name { get; set; }
        }
        
        [ProtoContract]
        [CompatibilityLevel(CompatibilityLevel.Level300)] // uses Timestamp format
        public class HazDateTimeTimestamp
        {
            [ProtoMember(1)]
            public int Id { get; set; }

            [ProtoMember(2)]
            public DateTime Time { get; set; }

            [ProtoMember(3)]
            public string Name { get; set; }
        }

        [ProtoContract]
        public class HazNodaTimeDuration
        {
            [ProtoMember(1)]
            public int Id { get; set; }

            [ProtoMember(2)]
            public NodaTime.Duration Time { get; set; }

            [ProtoMember(3)]
            public string Name { get; set; }
        }
        
        [ProtoContract]
        public class HazNodaTimeInstant
        {
            [ProtoMember(1)]
            public int Id { get; set; }

            [ProtoMember(2)]
            public NodaTime.Instant Time { get; set; }

            [ProtoMember(3)]
            public string Name { get; set; }
        }
    }
}
