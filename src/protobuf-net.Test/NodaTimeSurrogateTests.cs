using NodaTime;
using ProtoBuf.Meta;
using System;
using System.IO;
using Xunit;

namespace ProtoBuf.Test
{
    /// <summary>
    /// LocalDate and LocalTime reach the wire two ways: NodaTimeSerializers, which AddNodaTime
    /// installs on a RuntimeTypeModel, and NodaTimeSurrogates, which is what a compile-time model
    /// can use. They must agree byte-for-byte, or data written by one is misread by the other.
    /// </summary>
    public class NodaTimeSurrogateTests
    {
        [ProtoContract]
        public class HazLocalDate
        {
            [ProtoMember(1)] public LocalDate Value { get; set; }
        }

        [ProtoContract]
        public class HazLocalTime
        {
            [ProtoMember(1)] public LocalTime Value { get; set; }
        }

        [ProtoContract]
        public class HazNullableLocalDate
        {
            [ProtoMember(1)] public LocalDate? Value { get; set; }
        }

        [ProtoContract]
        public class HazNullableLocalTime
        {
            [ProtoMember(1)] public LocalTime? Value { get; set; }
        }

        [ProtoContract]
        public class HazDateSurrogate
        {
            [ProtoMember(1)] public NodaTimeSurrogates.Date Value { get; set; }
        }

        [ProtoContract]
        public class HazTimeSurrogate
        {
            [ProtoMember(1)] public NodaTimeSurrogates.TimeOfDay Value { get; set; }
        }

        private static RuntimeTypeModel NodaModel()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            return model.AddNodaTime();
        }

        private static RuntimeTypeModel PlainModel()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            return model;
        }

        private static string Hex<T>(RuntimeTypeModel model, T value)
        {
            using var ms = new MemoryStream();
            model.Serialize(ms, value);
            return BitConverter.ToString(ms.ToArray());
        }

        [Theory]
        [InlineData(2020, 1, 2)]
        [InlineData(1, 1, 1)]
        [InlineData(9999, 12, 31)]
        public void DateSurrogateMatchesTheCustomSerializer(int year, int month, int day)
        {
            var value = new LocalDate(year, month, day);
            var viaSerializer = Hex(NodaModel(), new HazLocalDate { Value = value });
            var viaSurrogate = Hex(PlainModel(), new HazDateSurrogate { Value = value });
            Assert.Equal(viaSerializer, viaSurrogate);
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(3, 4, 5, 0)]
        [InlineData(23, 59, 59, 123456789)]
        public void TimeSurrogateMatchesTheCustomSerializer(int hour, int minute, int second, int nanos)
        {
            var value = new LocalTime(hour, minute, second).PlusNanoseconds(nanos);
            var viaSerializer = Hex(NodaModel(), new HazLocalTime { Value = value });
            var viaSurrogate = Hex(PlainModel(), new HazTimeSurrogate { Value = value });
            Assert.Equal(viaSerializer, viaSurrogate);
        }

        [Theory]
        [InlineData(2020, 1, 2)]
        [InlineData(1, 1, 1)]
        public void DateSurrogateRoundTrips(int year, int month, int day)
        {
            var value = new LocalDate(year, month, day);
            NodaTimeSurrogates.Date surrogate = value;
            LocalDate back = surrogate;
            Assert.Equal(value, back);
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(23, 59, 59, 123456789)]
        public void TimeSurrogateRoundTrips(int hour, int minute, int second, int nanos)
        {
            var value = new LocalTime(hour, minute, second).PlusNanoseconds(nanos);
            NodaTimeSurrogates.TimeOfDay surrogate = value;
            LocalTime back = surrogate;
            Assert.Equal(value, back);
        }

        [Fact]
        public void AnAbsentDateReadsAsTheDefault()
        {
            // every field omitted: the custom serializer starts from a default LocalDate's
            // components (1, 1, 1), and the surrogate has to land in the same place
            LocalDate viaSurrogate = new NodaTimeSurrogates.Date();
            Assert.Equal(default(LocalDate), viaSurrogate);

            using var ms = new MemoryStream();
            var clone = NodaModel().Deserialize<HazLocalDate>(ms);
            Assert.Equal(default(LocalDate), clone.Value);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void NullableDateRoundTrips(bool hasValue)
        {
            LocalDate? value = hasValue ? new LocalDate(2020, 1, 2) : null;
            var model = NodaModel();
            using var ms = new MemoryStream();
            model.Serialize(ms, new HazNullableLocalDate { Value = value });
            ms.Position = 0;
            Assert.Equal(value, model.Deserialize<HazNullableLocalDate>(ms).Value);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void NullableTimeRoundTrips(bool hasValue)
        {
            LocalTime? value = hasValue ? new LocalTime(3, 4, 5) : null;
            var model = NodaModel();
            using var ms = new MemoryStream();
            model.Serialize(ms, new HazNullableLocalTime { Value = value });
            ms.Position = 0;
            Assert.Equal(value, model.Deserialize<HazNullableLocalTime>(ms).Value);
        }

        [Fact]
        public void NonIsoDatesAreRefusedTheSameWay()
        {
            var julian = new LocalDate(2020, 1, 2, CalendarSystem.Julian);
            var viaSerializer = Assert.Throws<ArgumentOutOfRangeException>(() => Hex(NodaModel(), new HazLocalDate { Value = julian }));
            var viaSurrogate = Assert.Throws<ArgumentOutOfRangeException>(() => { NodaTimeSurrogates.Date _ = julian; });
            Assert.Equal(viaSerializer.Message, viaSurrogate.Message);
        }
    }
}
