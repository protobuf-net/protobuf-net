using AotFixtures.DepthBoundary;
using ProtoBuf.Meta;
using System;
using System.IO;
using Xunit;

namespace ProtoBuf.AotConformance
{
    /// <summary>
    /// The raw and stateful nesting caps must ADD across a hand-back (gap B15).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>RawWrite_</c> carries its own remaining-depth budget and deliberately never touches
    /// <c>writer.Depth</c> — the "raw API does not maintain all the members" convention. The gap is
    /// the reverse transition: where a raw body falls back to the engine, the engine counted from
    /// whatever <c>writer.Depth</c> was at the <i>outer</i> boundary, so a deep raw chain that then
    /// went stateful under-counted and the effective limit was larger than <c>MaxDepth</c>.
    /// </para>
    /// <para>
    /// The fixture is built so neither cap fires on its own: each raw run is shorter than
    /// <c>MaxDepth</c>, and so is the number of stateful hops. Only the SUM exceeds it, which is
    /// exactly the property under test — so this fails if the sync is removed, rather than passing
    /// for an unrelated reason.
    /// </para>
    /// </remarks>
    public class RawDepthBoundaryTests
    {
        private const int MaxDepth = 12, RawRun = 8, Hops = 2;

        /// <summary>
        /// A ladder: <paramref name="hops"/> segments of <see cref="RawRun"/> raw links each,
        /// joined by a nullable-struct member that crosses into the stateful engine.
        /// </summary>
        private static Step Ladder(int hops)
        {
            Step head = new() { Id = hops };
            var tail = head;
            for (var i = 0; i < RawRun - 1; i++)
            {
                tail.Deep = new Step { Id = i };
                tail = tail.Deep;
            }
            if (hops > 1) tail.Side = new Hop { Target = Ladder(hops - 1) };
            return head;
        }

        private static TypeModel Model(int maxDepth)
        {
            var model = Assert.IsAssignableFrom<TypeModel>(
                Activator.CreateInstance(typeof(DepthBoundaryModel), nonPublic: true));
            model.MaxDepth = maxDepth;
            return model;
        }

        [Fact]
        public void ShallowLadderStillSerializes()
        {
            // one segment: RawRun levels, comfortably inside MaxDepth. If this throws, the sync is
            // over-counting and the test below would pass for the wrong reason
            using var ms = new MemoryStream();
            Model(MaxDepth).Serialize(ms, Ladder(1));
            Assert.True(ms.Length > 0);
        }

        [Fact]
        public void NeitherCapFiresAloneAtThisShape()
        {
            // the raw runs and the hop count are each inside MaxDepth; it is only their sum that is
            // not. Stated as a test so the constants cannot drift into proving nothing.
            Assert.True(RawRun < MaxDepth);
            Assert.True(Hops < MaxDepth);
            Assert.True(RawRun * Hops > MaxDepth);
        }

        [Fact]
        public void DeepLadderAcrossTheBoundaryThrows()
        {
            using var ms = new MemoryStream();
            var ex = Assert.ThrowsAny<Exception>(() => Model(MaxDepth).Serialize(ms, Ladder(Hops)));
            // either cap is a pass: the point is that the total is caught, not which guard caught it
            Assert.True(ex is InvalidOperationException or ProtoException, "expected a depth failure, got " + ex);
        }
    }
}
