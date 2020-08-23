using Google.Protobuf;
using NodaTime.Serialization.Protobuf;
using System;
using Xunit;
using Xunit.Abstractions;

namespace NativeGoogleTests
{
    public class NodaTimeTests
    {
        private readonly ITestOutputHelper _log;
        public NodaTimeTests(ITestOutputHelper log) => _log = log;
        private string Log(string message)
        {
            _log?.WriteLine(message);
            return message;
        }

        [Fact]
        public void TestExpectedBinaryOutputDuration()
        {
            var duration = NodaTime.Duration.FromTimeSpan(new TimeSpan(42, 1, 10, 12, 451));
            var obj = new HazNodaTimeDuration { Id = 42, Name = "abc", Time = duration.ToProtobufDuration() };
            var hex = Log(BitConverter.ToString(obj.ToByteArray()));

            // this is the same output as noted in AssertBytesFromTimeSpanModel
            Assert.Equal("08-2A-12-0B-08-F4-DE-DD-01-10-C0-ED-86-D7-01-1A-03-61-62-63", hex);
        }
        
        [Fact]
        public void TestExpectedBinaryOutputInterval()
        {
            var when = NodaTime.Instant.FromDateTimeUtc(new DateTime(2020, 8, 23, 8, 51, 12, 451, DateTimeKind.Utc));
            var obj = new HazNodaTimeInstant() { Id = 42, Name = "abc", Time = when.ToTimestamp() };
            var hex = Log(BitConverter.ToString(obj.ToByteArray()));

            // this is the same output as noted in AssertBytesFromDateTimeModel
            Assert.Equal("08-2A-12-0C-08-80-DC-88-FA-05-10-C0-ED-86-D7-01-1A-03-61-62-63", hex);
        }
        

        
        
    }
}
