using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf;

namespace Examples.Issues
{
    
    public class SO11564914
    {
        [Fact]
        public void SerializeFromProtobufCSharpPortShouldGiveUsefulMessage()
        {
            Program.ExpectFailure<InvalidOperationException>(() =>
            {
                var obj = new BlockHeader();
                Serializer.Serialize(Stream.Null, obj);
            }, "Are you mixing protobuf-net and protobuf-csharp-port? See https://stackoverflow.com/q/11564914/23354; type: Examples.Issues.SO11564914+BlockHeader");
        }
        [Fact]
        public void DeserializeFromProtobufCSharpPortShouldGiveUsefulMessage()
        {
            Program.ExpectFailure<InvalidOperationException>(() =>
            {
                var obj = new BlockHeader();
                Serializer.Deserialize<BlockHeader>(Stream.Null);
            }, "Are you mixing protobuf-net and protobuf-csharp-port? See https://stackoverflow.com/q/11564914/23354; type: Examples.Issues.SO11564914+BlockHeader");
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
