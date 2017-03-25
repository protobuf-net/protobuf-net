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
                ProtoWriter.WriteFieldHeader(1, WireType.String, pw);
                ProtoWriter.WriteString("abc", pw);
            }, "0A03616263");
        }
        [Test]
        public void TestVariantInt32()
        {
            for (int i = 0; i < 128; i++)
            {
                Util.Test(pw =>
                  {
                      ProtoWriter.WriteFieldHeader(1, WireType.Variant, pw);
                      ProtoWriter.WriteInt32(i, pw);
                  }, "08" // 1 * 8 + 0
                 + i.ToString("X2")
                );
            }
            Util.Test(pw => {
                ProtoWriter.WriteFieldHeader(1, WireType.Variant, pw);
                ProtoWriter.WriteInt32(128, pw);
            }, "088001");
        }
    }
}
