using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using System.IO;
using ProtoBuf.Meta;

namespace ProtoBuf.unittest.Serializers
{
    public class SubItems
    {
        [Fact]
        public void TestWriteSubItemWithShortBlob() {
            Util.Test((ProtoWriter pw, ref ProtoWriter.State st) =>
            {
                ProtoWriter.WriteFieldHeader(5, WireType.String, pw, ref st);
                SubItemToken token = ProtoWriter.StartSubItem(new object(), pw, ref st);
                ProtoWriter.WriteFieldHeader(6, WireType.String, pw, ref st);
                ProtoWriter.WriteBytes(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }, pw, ref st);
                ProtoWriter.EndSubItem(token, pw, ref st);
            }, "2A" // 5 * 8 + 2 = 42
             + "0A" // sub-item length = 10
             + "32" // 6 * 8 + 2 = 50 = 0x32
             + "08" // BLOB length
             + "0001020304050607"); // BLOB
        }
    }
}
