using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace ProtoBuf.unittest.Serializers
{
    public class Writer
    {
        [Fact]
        public void TestString_abc()
        {
            Util.Test((ref ProtoWriter.State st) =>
            {
                st.WriteFieldHeader(1, WireType.String);
                st.WriteString("abc");
            }, "0A03616263");
        }
        [Fact]
        public void TestVariantInt32()
        {
            for (int i = 0; i < 128; i++)
            {
                Util.Test((ref ProtoWriter.State st) =>
                  {
                      st.WriteFieldHeader(1, WireType.Varint);
                      st.WriteInt32(i);
                  }, "08" // 1 * 8 + 0
                 + i.ToString("X2")
                );
            }
            Util.Test((ref ProtoWriter.State st) => {
                st.WriteFieldHeader(1, WireType.Varint);
                st.WriteInt32(128);
            }, "088001");
        }
    }
}