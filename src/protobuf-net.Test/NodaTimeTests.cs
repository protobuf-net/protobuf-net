using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using ProtoBuf.unittest;
using Xunit;

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

        [ProtoContract]
        public class HazWellKnownDuration
        {
            [ProtoMember(1)]
            public WellKnownTypes.Duration Value { get; set; }
        }

        [Fact]
        public void CanRoundTripValueWithDuration()
        {
            var model = CreateModel();

            TestRoundTrip(model); // runtime only
            model.CompileInPlace();
            TestRoundTrip(model); // locally compiled

            TestRoundTrip(model.Compile()); // fully compiled in-proc

#if !PLAT_NO_EMITDLL
            TestRoundTrip(model.CompileAndVerify()); // fully compiled on disk
#endif
            static void TestRoundTrip(TypeModel model)
            {
                var duration = NodaTime.Duration.FromTimeSpan(new System.TimeSpan(42, 1, 10, 12, 451));
                var obj = new HazNodaTimeDuration { Id = 42, Name = "abc", Time = duration };
                var clone = model.DeepClone(obj);
                Assert.NotSame(obj, clone);
                Assert.Equal(obj.Id, clone.Id);
                Assert.Equal(obj.Name, clone.Name);
                Assert.Equal(obj.Time, clone.Time);
            }
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
