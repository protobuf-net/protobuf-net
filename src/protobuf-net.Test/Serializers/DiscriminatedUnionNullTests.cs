using Xunit;

namespace ProtoBuf.Serializers
{
    /// <summary>
    /// Assigning null to a union deselects it, rather than selecting it with nothing. Pinned across
    /// all four unions because for a long time only three of them did it: the generated `oneof`
    /// property setter is `new DiscriminatedUnionNObject(n, value)`, so an all-reference oneof (which
    /// picks the plain <see cref="DiscriminatedUnionObject"/>) reported ShouldSerializeX() == true
    /// with X == null, while a mixed one did not.
    /// </summary>
    public class DiscriminatedUnionNullTests
    {
        [Fact]
        public void ObjectUnionDeselectsOnNull()
        {
            Assert.True(new DiscriminatedUnionObject(3, "x").Is(3));
            Assert.False(new DiscriminatedUnionObject(3, null).Is(3));
        }

        [Fact]
        public void Union32DeselectsOnNull()
        {
            Assert.True(new DiscriminatedUnion32Object(3, (object)"x").Is(3));
            Assert.False(new DiscriminatedUnion32Object(3, (object)null).Is(3));
        }

        [Fact]
        public void Union64DeselectsOnNull()
        {
            Assert.True(new DiscriminatedUnion64Object(3, (object)"x").Is(3));
            Assert.False(new DiscriminatedUnion64Object(3, (object)null).Is(3));
        }

        [Fact]
        public void Union128DeselectsOnNull()
        {
            Assert.True(new DiscriminatedUnion128Object(3, (object)"x").Is(3));
            Assert.False(new DiscriminatedUnion128Object(3, (object)null).Is(3));
        }
    }
}
