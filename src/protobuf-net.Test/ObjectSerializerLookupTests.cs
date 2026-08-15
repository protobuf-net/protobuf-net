using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.IO;
using Xunit;

namespace ProtoBuf.Test
{
    /// <summary>
    /// The non-generic (<c>object</c>-typed) API must not re-run the serializer lookup on every
    /// call.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>RuntimeTypeModel</c> caches serializer lookups, but only <b>positive</b> ones: the cache
    /// is a <c>Hashtable</c>, so a miss and a cached null are indistinguishable, and
    /// <c>GetServicesSlow</c> stores a result only <c>if (service is not null)</c>. A type with no
    /// service therefore re-runs the whole slow path — a lock, the repeated-provider probe, and
    /// <c>FindOrAddAuto</c> — every single time it is asked for.
    /// </para>
    /// <para>
    /// <c>typeof(object)</c> is exactly such a type, and the non-generic API asks for it on every
    /// call: the typed lookup misses, and only then does the dynamic path resolve the concrete
    /// type. Measured before the fix at <b>~2.3 KB allocated and ~2.9 µs per call</b>, against
    /// ~72 ns and zero allocation for the same serialization reached generically — a 40× penalty on
    /// a call shape a great deal of pre-generic code still uses.
    /// </para>
    /// </remarks>
    public class ObjectSerializerLookupTests
    {
        [ProtoContract]
        public class Payload
        {
            [ProtoMember(1)] public int Id { get; set; }
            [ProtoMember(2)] public string Name { get; set; }
        }

        private static RuntimeTypeModel CreateModel()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(Payload), true);
            return model;
        }

        /// <summary>
        /// The shortcut is safe precisely because this throws — <c>object</c> cannot acquire a
        /// serializer later, so a permanent "no" cannot become wrong.
        /// </summary>
        /// <remarks>
        /// Pinned as a test rather than trusted as a comment: if this restriction were ever lifted,
        /// the shortcut would silently start returning the wrong answer, and nothing else in the
        /// suite would notice.
        /// </remarks>
        [Fact]
        public void ObjectCannotBeAddedToAModel()
        {
            var model = CreateModel();
            var ex = Assert.Throws<ArgumentException>(() => model.Add(typeof(object), true));
            Assert.Contains("cannot reconfigure", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>The object-typed API still produces the same bytes as the typed one.</summary>
        [Fact]
        public void ObjectTypedSerializeMatchesTheTypedForm()
        {
            var model = CreateModel();
            var value = new Payload { Id = 42, Name = "hello" };

            using var viaObject = new MemoryStream();
            model.Serialize<object>(viaObject, value);

            using var viaTyped = new MemoryStream();
            model.Serialize(viaTyped, value);

            Assert.Equal(
                BitConverter.ToString(viaTyped.ToArray()),
                BitConverter.ToString(viaObject.ToArray()));
            Assert.NotEmpty(viaObject.ToArray());
        }

        /// <summary>...and still round-trips through the object-typed API in both directions.</summary>
        [Fact]
        public void ObjectTypedRoundTrips()
        {
            var model = CreateModel();
            using var ms = new MemoryStream();
            model.Serialize<object>(ms, new Payload { Id = 7, Name = "world" });
            ms.Position = 0;
            var clone = (Payload)model.Deserialize(ms, null, typeof(Payload));
            Assert.Equal(7, clone.Id);
            Assert.Equal("world", clone.Name);
        }

        /// <summary>
        /// A type the model does NOT know still fails, rather than being silently swallowed by the
        /// shortcut — the shortcut is for <c>object</c> alone, not for "anything unresolvable".
        /// </summary>
        [Fact]
        public void AnUnknownTypeStillThrows()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoAddMissingTypes = false;
            using var ms = new MemoryStream();
            Assert.ThrowsAny<Exception>(() => model.Serialize<object>(ms, new NotAContract()));
        }

        public class NotAContract { public int Whatever { get; set; } }

#if NET5_0_OR_GREATER
        /// <summary>
        /// The regression guard proper: repeated object-typed serialization must not allocate per
        /// call.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>GC.GetAllocatedBytesForCurrentThread</c> is exact and per-thread, so this needs no
        /// profiler and does not depend on collection timing — but it is net5.0+, which is why this
        /// one test is guarded while the semantic tests above run on every TFM.
        /// </para>
        /// <para>
        /// The threshold is deliberately loose. Before the fix this was <b>2272 bytes per call</b>,
        /// flat from one call to a thousand; after it, zero. Anything under a few hundred bytes
        /// distinguishes those two beyond argument, and a loose bound will not turn into a flaky
        /// test the first time an unrelated allocation appears somewhere in the path.
        /// </para>
        /// </remarks>
        [Fact]
        public void ObjectTypedSerializeDoesNotAllocatePerCall()
        {
            var model = CreateModel();
            var value = new Payload { Id = 42, Name = "hello" };
            using var ms = new MemoryStream();

            void Serialize()
            {
                ms.Position = 0;
                ms.SetLength(0);
                model.Serialize<object>(ms, value);
            }

            for (int i = 0; i < 200; i++) Serialize();   // settle every one-off cache first

            const int Iterations = 500;
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Iterations; i++) Serialize();
            var perCall = (GC.GetAllocatedBytesForCurrentThread() - before) / (double)Iterations;

            Assert.True(perCall < 256,
                $"object-typed serialize allocated {perCall:F1} B/call; it was 2272 B/call when the "
                + "serializer lookup for `object` was re-run on every call");
        }
#endif
    }
}
