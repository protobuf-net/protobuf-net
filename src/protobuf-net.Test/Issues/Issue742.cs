using System;
using System.IO;
using Xunit;

namespace ProtoBuf.Test.Issues
{
    public class Issue742
    {
        [Fact]
        public void TestNullBasicTypeSerialization()
        {
            long? nullableLong = 11;
            var (serialized, hex) = Serialize(nullableLong);
            Assert.Equal("02-08-0B", hex);
            var deserialized = Deserialize<long?>(serialized);
            Assert.True(deserialized == nullableLong);
        }

        private T Deserialize<T>(byte[] toDeserialize)
        {
            using (var stream = new MemoryStream(toDeserialize))
            {
                return Serializer.DeserializeWithLengthPrefix<T>(stream, PrefixStyle.Base128);
            }
        }

        private (byte[], string) Serialize<T>(T toSerialize)
        {
            using (var stream = new MemoryStream())
            {
                Serializer.SerializeWithLengthPrefix(stream, toSerialize, PrefixStyle.Base128);
                var hex = BitConverter.ToString(stream.GetBuffer(), 0, (int)stream.Length);
                return (stream.ToArray(), hex);
            }
        }
    }
}
