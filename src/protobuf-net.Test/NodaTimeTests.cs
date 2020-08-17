using ProtoBuf.Meta;
using ProtoBuf.Serializers;
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
        [Fact]
        public void CanRegisterTypes()
        {
            var model = CreateModel();
            var metaType = model[typeof(NodaTime.Duration)];
            Assert.True(metaType.HasSurrogate);
            Assert.Equal(typeof(WellKnownTypes.Duration), metaType.GetSurrogateOrBaseOrSelf(false).Type);
        }

        private readonly ITestOutputHelper _log;
        public NodaTimeTests(ITestOutputHelper log) => _log = log;
        private string Log(string message)
        {
            _log?.WriteLine(message);
            return message;
        }

        [Fact]
        public void SchemaWorksThroughSurrogate()
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
        public void AssertBytesFromTimeSpanModel()
        {
            var duration = new TimeSpan(42, 1, 10, 12, 451);
            var obj = new HazTimeSpanDuration { Id = 42, Name = "abc", Time = duration };
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, obj);
            Assert.Equal("08-2A-12-0B-08-F4-DE-DD-01-10-C0-ED-86-D7-01-1A-03-61-62-63", Log(BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length)));
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
        public class HazNodaTimeDuration
        {
            [ProtoMember(1)]
            public int Id { get; set; }

            [ProtoMember(2)]
            public NodaTime.Duration Time { get; set; }

            [ProtoMember(3)]
            public string Name { get; set; }
        }
    }
}
