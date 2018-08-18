using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Examples.Issues
{
    public class Issue374
    {
        [ProtoContract]
        public class Issue374TestModel
        {
            [ProtoMember(1, IsRequired = true)]
            public byte[] TestBytes { get; set; }

            public Issue374TestModel()
            {

            }

            public Issue374TestModel(byte[] bytes)
            {
                TestBytes = bytes;
            }
        }

        [Fact]
        public void ReadLengthPrefixProducesCorrectLengthComparedToStreamPosition()
        {
            byte[] bytes = null;
            const int arraySize = 7000 * 100;

            using(MemoryStream stream = new MemoryStream(arraySize))
            {
                Serializer.SerializeWithLengthPrefix(stream, new Issue374TestModel(new byte[arraySize]), PrefixStyle.Base128);
                bytes = stream.ToArray();
            }

            using(MemoryStream stream = new MemoryStream(bytes))
            {
                ProtoReader.ReadLengthPrefix(stream, false, PrefixStyle.Base128, out int fieldNumber, out int bytesRead);

                //These should be the same. They don't appear to be with the fault raised in issue 374
                Assert.Equal(stream.Position, bytesRead);
            }
        }
    }
}
