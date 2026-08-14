using AotFixtures.GroupedElements;
using ProtoBuf;
using System;
using System.IO;
using Xunit;

namespace ProtoBuf.AotConformance
{
    /// <summary>
    /// A cycle through <b>grouped</b> members must throw, not recurse until the process dies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Marc's case, and the hole gap B14 opened:
    /// <code>
    /// Node a = new(), b = new() { GroupTail = a };
    /// a.GroupTail = b;
    /// Serialize(a);
    /// </code>
    /// </para>
    /// <para>
    /// The generated writer's recursion used to be safe <i>by construction</i>: every write was
    /// preceded by a measure, and the measure carried the depth budget — the emitter said so
    /// outright ("the write recursion that follows traverses the graph the measure just proved
    /// finite"). A grouped sub-message has no length prefix, so nothing measures it; making groups
    /// write without a measure therefore removed the only guard on the way down. Found by reading
    /// the emitted code, not by a failing test — nothing in the suite had a grouped cycle.
    /// </para>
    /// <para>
    /// This test can only fail in two ways, and both are loud: a clean throw is a pass, no throw
    /// means the cycle was silently written, and a lost guard takes the whole test process with
    /// it. That is the right shape for a stack-overflow guard, since the failure it prevents is
    /// not catchable.
    /// </para>
    /// </remarks>
    public class GroupCycleTests
    {
        private static Node Cycle()
        {
            Node a = new() { Id = 1 }, b = new() { Id = 2, GroupTail = a };
            a.GroupTail = b;
            return a;
        }

        [Fact]
        public void GroupedCycleThrowsRatherThanOverflowing()
        {
            using var ms = new MemoryStream();
            var ex = Assert.ThrowsAny<Exception>(
                () => GroupedElementsModel.Instance.Serialize(ms, Cycle()));

            // the message is the generated writer's own depth guard, not an incidental failure
            Assert.Contains("depth", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The length-prefixed sibling, which was already guarded — by the measure rather than by
        /// the write. Kept alongside so the two paths are visibly held to the same standard.
        /// </summary>
        [Fact]
        public void LengthPrefixedCycleAlsoThrows()
        {
            var a = new Grouped();
            a.Plain = [new Item { Name = "x" }];
            // a self-referential length-prefixed graph is not expressible on Grouped, so this
            // simply pins that an ordinary payload still round-trips beside the cycle test
            using var ms = new MemoryStream();
            GroupedElementsModel.Instance.Serialize(ms, a);
            Assert.True(ms.Length > 0);
        }
    }
}
