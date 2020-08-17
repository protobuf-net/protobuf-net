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
        public void TestExpectedBinaryOutput()
        {
            var duration = NodaTime.Duration.FromTimeSpan(new TimeSpan(42, 1, 10, 12, 451));
            var obj = new HazNodaTimeDuration { Id = 42, Name = "abc", Time = duration.ToProtobufDuration() };
            var hex = Log(BitConverter.ToString(obj.ToByteArray()));

            // this is the same output as noted in AssertBytesFromTimeSpanModel
            Assert.Equal("08-2A-12-0B-08-F4-DE-DD-01-10-C0-ED-86-D7-01-1A-03-61-62-63", hex);
        }
    }
}
