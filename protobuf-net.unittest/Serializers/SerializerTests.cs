using System;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ProtoBuf;

namespace protobuf_net.Tests
{
    [TestFixture]
    public class SerializerTests
    {
        [Test]
        public void WhenDateTimeIsSerializedAndThenDeserialized_DateTimeKindIsPreserved()
        {
            // Arrange
            var utcDate = new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Act
            DateTime deserializedDate;

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, utcDate);
                stream.Flush();
                stream.Position = 0;
                deserializedDate = Serializer.Deserialize<DateTime>(stream);
            }

            // Assert
            Assert.That(deserializedDate.Kind, Is.EqualTo(utcDate.Kind));
        }

        [Test]
        public void WhenTimespanIsSerializedAndDeserialized_TimeSpanIsPreserved()
        {
            // Arrange
            var originalTimeSpan = new TimeSpan(1, 2, 3, 4);

            // Act
            TimeSpan deserializedTimeSpan;

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, originalTimeSpan);
                stream.Flush();
                stream.Position = 0;
                deserializedTimeSpan = Serializer.Deserialize<TimeSpan>(stream);
            }

            // Assert
            Assert.That(deserializedTimeSpan, Is.EqualTo(originalTimeSpan));
        }
    }
}
