using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;
using ProtoBuf.Meta;

namespace ProtoBuf.unittest.Serializers
{
    [TestFixture]
    public class SubItems
    {
        [Test]
        public void TestWriteSubItemWithShortBlob() {
            Util.Test(pw =>
            {
                pw.WriteFieldHeader(5, WireType.String);
                int token = ProtoWriter.StartSubItem(new object(), pw);
                pw.WriteFieldHeader(6, WireType.String);
                pw.WriteBytes(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 });
                ProtoWriter.EndSubItem(token, pw);
            }, "2A" // 5 * 8 + 2 = 42
             + "0A" // sub-item length = 10
             + "32" // 6 * 8 + 2 = 50 = 0x32
             + "08" // BLOB length
             + "0001020304050607"); // BLOB
        }
    }
}
