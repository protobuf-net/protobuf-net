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
            var serialized = Serialize<long?>(nullableLong);
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

        private byte[] Serialize<T>(T toSerialize)
        {
            using (var stream = new MemoryStream())
            {
                Serializer.SerializeWithLengthPrefix(stream, toSerialize, PrefixStyle.Base128);
                return stream.ToArray();
            }
        }
    }
}
