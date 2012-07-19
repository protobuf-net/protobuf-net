using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf;

namespace Examples.Issues
{
    [TestFixture]
    public class SO11564914
    {
        [Test, ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Are you mixing protobuf-net and protobuf-csharp-port? See http://stackoverflow.com/q/11564914; type: Examples.Issues.SO11564914+BlockHeader")]
        public void SerializeFromProtobufCSharpPortShouldGiveUsefulMessage()
        {
            var obj = new BlockHeader();
            Serializer.Serialize(Stream.Null, obj);
        }
        [Test, ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Are you mixing protobuf-net and protobuf-csharp-port? See http://stackoverflow.com/q/11564914; type: Examples.Issues.SO11564914+BlockHeader")]
        public void DeserializeFromProtobufCSharpPortShouldGiveUsefulMessage()
        {
            var obj = new BlockHeader();
            Serializer.Deserialize<BlockHeader>(Stream.Null);
        }

        public sealed partial class BlockHeader : GeneratedMessage<BlockHeader, BlockHeader.Builder>
        {
            // yada yada yada
            public class Builder
            {
                
            }
        }

        public class GeneratedMessage<TFoo, TBar>
        {
        }
        
    }
}
