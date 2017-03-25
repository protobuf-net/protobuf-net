#if !NO_INTERNAL_CONTEXT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf.Serializers;
namespace ProtoBuf.unittest.Serializers
{
    
    public class Tag
    {
        [Fact]
        public void TestBasicTags()
        {

            Util.Test("abc", nil => new TagDecorator(1, WireType.String, false, nil), "0A");
        }
    }
}
#endif