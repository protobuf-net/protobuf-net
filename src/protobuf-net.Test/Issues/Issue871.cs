using System;
using System.IO;
using Xunit;

namespace ProtoBuf.Test.Issues
{
    public class Issue871
    {
        [Fact]
        public void CanRoundTripValues()
        {
            using var ms = new MemoryStream();
            int qtyExpected = 1;
            var whenExpected = new DateTime(2021, 1, 1);
            Serializer.SerializeWithLengthPrefix(ms, qtyExpected, PrefixStyle.Base128, 0); // 02-08-01
            Serializer.SerializeWithLengthPrefix(ms, whenExpected, PrefixStyle.Base128); // 06-0A-04-08-88-A3-02

            var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            Assert.Equal("02-08-01-06-0A-04-08-88-A3-02", hex);

            // Deserialize
            ms.Position = 0;
            int qtyActual = Serializer.DeserializeWithLengthPrefix<int>(ms, PrefixStyle.Base128);
            Assert.Equal(qtyExpected, qtyActual);
            var whenActual = Serializer.DeserializeWithLengthPrefix<DateTime>(ms, PrefixStyle.Base128);
            Assert.Equal(whenExpected, whenActual);
        }
    }
}
