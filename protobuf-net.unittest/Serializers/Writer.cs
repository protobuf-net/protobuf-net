using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ProtoBuf.unittest.Serializers
{
    [TestFixture]
    public class Writer
    {
        [Test]
        public void TestString_abc()
        {
            Util.Test(pw =>
            {
                pw.WriteFieldHeader(1, WireType.String);
                pw.WriteString("abc");
            }, "0A03616263");
        }
        [Test]
        public void TestVariantInt32()
        {
            for (int i = 0; i < 128; i++)
            {
                Util.Test(pw =>
                  {
                      pw.WriteFieldHeader(1, WireType.Variant);
                      pw.WriteInt32(i);
                  }, "08" // 1 * 8 + 0
                 + i.ToString("X2")
                );
            }
            Util.Test(pw => {
                pw.WriteFieldHeader(1, WireType.Variant);
                pw.WriteInt32(128);
            }, "088001");
        }
    }
}
