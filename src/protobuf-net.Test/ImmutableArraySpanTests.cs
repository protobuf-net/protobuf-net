using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Immutable;
using System.IO;
using Xunit;

namespace ProtoBuf.Test
{
    /// <summary>
    /// Does <c>ImmutableArray&lt;T&gt;.AsSpan()</c> fail safe on a <b>default</b> instance, on every
    /// framework we target? — `notes/gaps.md` B23.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This decides the emit shape for reaching an <c>ImmutableArray&lt;T&gt;</c> column as a span.
    /// The type is a <b>struct</b> whose default is not null but throws on most access
    /// (<c>Length</c>, the indexer, <c>GetEnumerator</c> all throw <c>NullReferenceException</c>),
    /// so "it is a struct, no null test needed" — which is what the existing notes say for the
    /// stateful path — does not by itself mean a span is reachable.
    /// </para>
    /// <para>
    /// If <c>AsSpan()</c> is safe, generated code can call it unguarded. If it throws, every
    /// emitted site needs an <c>IsDefaultOrEmpty</c> guard the array and list cases do not, which
    /// is a real cost in code size and a real chance of getting it wrong once.
    /// </para>
    /// <para>
    /// Run on <b>both</b> TFMs this project targets (net472 and net8.0) rather than assumed from
    /// one: <c>ImmutableArray&lt;T&gt;</c> comes from a package down-level and from the shared
    /// framework on modern .NET, and those are different implementations of the same surface.
    /// </para>
    /// </remarks>
    public class ImmutableArraySpanTests
    {
        [Fact]
        public void DefaultInstanceThrowsOnOrdinaryAccess()
        {
            // the premise: this really is a hostile default, not merely an empty one
            var value = default(ImmutableArray<int>);
            Assert.True(value.IsDefault);
            Assert.ThrowsAny<Exception>(() => value.Length);
        }

        [Fact]
        public void AsSpanOnDefaultIsSafeAndEmpty()
        {
            var value = default(ImmutableArray<int>);
            var span = value.AsSpan();          // must not throw
            Assert.True(span.IsEmpty);
            Assert.Equal(0, span.Length);
        }

        /// <summary>
        /// The trap that made the generated write drop a default instance: <c>ImmutableArray&lt;T&gt;</c>
        /// has lifted equality operators taking <c>ImmutableArray&lt;T&gt;?</c>, so <c>x != null</c>
        /// COMPILES for this struct — and evaluates to <b>false</b> for a default instance, because
        /// the nullable comparison unwraps both sides to a default value and finds them equal.
        /// </summary>
        /// <remarks>
        /// So a generated `if (tmp != null)` guard, which is correct and necessary for `T[]` and
        /// `List&lt;T&gt;`, silently SKIPS a default `ImmutableArray&lt;T&gt;`. For an unpacked
        /// member that is invisible (empty writes nothing anyway); for a packed one it drops the
        /// zero-length header protobuf-net emits, which is a real wire divergence.
        /// </remarks>
        [Fact]
        public void DefaultCompareEqualToNull_WhichIsWhyTheNullGuardIsWrongHere()
        {
            var value = default(ImmutableArray<int>);
            Assert.False(value != null);      // the guard a generated write would use
            Assert.True(value == null);
            // ...while a populated one behaves as you would expect
            Assert.True(ImmutableArray.Create(1) != null);
        }

        [Fact]
        public void AsSpanOnEmptyIsEmpty()
        {
            var span = ImmutableArray<int>.Empty.AsSpan();
            Assert.True(span.IsEmpty);
        }

        [Fact]
        public void AsSpanOnPopulatedRoundTripsTheElements()
        {
            var value = ImmutableArray.Create(1, 2, 3);
            var span = value.AsSpan();
            Assert.Equal(3, span.Length);
            Assert.Equal(1, span[0]);
            Assert.Equal(3, span[2]);
        }

        [ProtoContract]
        public class Holder
        {
            [ProtoMember(1)] public ImmutableArray<int> Values { get; set; }
            [ProtoMember(2, IsPacked = true)] public ImmutableArray<int> Packed { get; set; }
        }

        /// <summary>
        /// What protobuf-net writes for a default instance today — the behaviour any span-based
        /// emit must reproduce exactly.
        /// </summary>
        /// <remarks>
        /// <b>A default is treated exactly as empty</b>, not as absent: the packed member still
        /// writes its zero-length header (<c>12-00</c>) and the unpacked one writes nothing, which
        /// is precisely what both do for <c>ImmutableArray&lt;int&gt;.Empty</c>. That falls out of
        /// <c>ImmutableArraySerializer.Initialize</c> mapping <c>IsDefault</c> onto <c>Empty</c>.
        /// <para>
        /// Taken together with <see cref="AsSpanOnDefaultIsSafeAndEmpty"/> this is the whole
        /// answer for the emit: <c>AsSpan()</c> yields an empty span for a default, and an empty
        /// span produces exactly the bytes a default is supposed to produce — so generated code
        /// needs <b>no guard at all</b>, and the shape is the same as an array's.
        /// </para>
        /// </remarks>
        [Fact]
        public void DefaultInstanceSerializesAsEmptyRatherThanAbsent()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(Holder), true);

            using var fromDefault = new MemoryStream();
            model.Serialize(fromDefault, new Holder());   // both members left default

            using var fromEmpty = new MemoryStream();
            model.Serialize(fromEmpty, new Holder
            {
                Values = ImmutableArray<int>.Empty,
                Packed = ImmutableArray<int>.Empty,
            });

            Assert.Equal("12-00", BitConverter.ToString(fromDefault.ToArray()));
            Assert.Equal(
                BitConverter.ToString(fromEmpty.ToArray()),
                BitConverter.ToString(fromDefault.ToArray()));
        }

        [Fact]
        public void EmptyAndPopulatedMatchTheirArrayEquivalents()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(Holder), true);

            using var empty = new MemoryStream();
            model.Serialize(empty, new Holder { Values = ImmutableArray<int>.Empty, Packed = ImmutableArray<int>.Empty });

            using var populated = new MemoryStream();
            model.Serialize(populated, new Holder
            {
                Values = ImmutableArray.Create(1, 2, 300),
                Packed = ImmutableArray.Create(1, 2, 300),
            });

            // unpacked field 1: tag 08 per element; packed field 2: tag 12, length, then the body
            Assert.Equal("08-01-08-02-08-AC-02-12-04-01-02-AC-02",
                BitConverter.ToString(populated.ToArray()));
            // an empty packed collection still writes its zero-length header; the unpacked one writes nothing
            Assert.Equal("12-00", BitConverter.ToString(empty.ToArray()));
        }
    }
}
