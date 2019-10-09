#if !NO_INTERNAL_CONTEXT
using ProtoBuf.Internal.Serializers;
using Xunit;

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