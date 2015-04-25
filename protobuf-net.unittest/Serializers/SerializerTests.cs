using System;
using System.IO;
using System.Runtime.Serialization;
using NUnit.Framework;
using ProtoBuf;

namespace ProtoBuf.unittest.Serializers
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
            Assert.AreEqual(deserializedDate.Kind, utcDate.Kind);
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
            Assert.AreEqual(deserializedTimeSpan, originalTimeSpan);
        }

        [Test]
        public void WhenEmptyArrayIsSerializedAndDeserialized_EmptyArrayIsPreserved()
        {
            // Arrange
            var original = new Test
            {
                Array = new int[0]
            };

            // Act
            Test deserialized;

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, original);
                stream.Flush();
                stream.Position = 0;
                deserialized = Serializer.Deserialize<Test>(stream);
            }

            // Assert
            Assert.IsNotNull(deserialized.Array);
            Assert.AreEqual(deserialized.Array.Length, 0);
        }

        [Test]
        public void WhenNullArrayIsSerializedAndDeserialized_ArrayIsNull()
        {
            // Arrange
            var original = new Test
            {
                Array = null
            };

            // Act
            Test deserialized;

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, original);
                stream.Flush();
                stream.Position = 0;
                deserialized = Serializer.Deserialize<Test>(stream);
            }

            // Assert
            Assert.IsNull(deserialized.Array);
        }

        [DataContract]
        private class Test
        {
            [DataMember(Order = 1)]
            public int[] Array { get; set; }
        }
    }
}
