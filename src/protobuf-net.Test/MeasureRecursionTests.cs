using ProtoBuf.Meta;
using System;
using System.IO;
using Xunit;

#if PLAT_SPANS
using System.Buffers;
#endif

namespace ProtoBuf.Tests
{
    /// <summary>
    /// A cyclic object graph must be REPORTED, not crash the process, on every backend.
    /// </summary>
    /// <remarks>
    /// The measure walk recurses exactly as the write walk does - <c>Measure</c> calls
    /// <c>serializer.Write</c>, which re-enters <c>WriteMessage</c> - but it used to bypass
    /// <c>PreSubItem</c>, where both the depth cap and the recursion stack live. So a
    /// measure-first backend overflowed the STACK where the classic reserve-and-back-fill path
    /// threw politely; and a stack overflow cannot be caught, so it takes the process with it.
    /// <para>
    /// The exposure was specific to measure-first: the buffer-writer has always measured every
    /// sub-message through the null writer, so it has always been reachable there.
    /// </para>
    /// </remarks>
    public class MeasureRecursionTests
    {
        [ProtoContract]
        public class Node
        {
            [ProtoMember(1)] public int Value { get; set; }
            [ProtoMember(2)] public Node Next { get; set; }
        }

        private static Node CreateCycle()
        {
            var node = new Node { Value = 1 };
            node.Next = node; // the shortest possible cycle
            return node;
        }

        private static RuntimeTypeModel CreateModel()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(Node), true);
            return model;
        }

        [Fact]
        public void StreamBackendReportsACycle()
        {
            var model = CreateModel();
            using var ms = new MemoryStream();
            var ex = Assert.ThrowsAny<Exception>(() => model.Serialize(ms, CreateCycle()));
            Assert.Contains("recursion", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

#if PLAT_SPANS
        /// <summary>ArrayBufferWriter is not available down-level, and this needs nothing from it.</summary>
        private sealed class MinimalBufferWriter : IBufferWriter<byte>
        {
            private byte[] _buffer = new byte[1024];
            private int _index;
            public void Advance(int count) => _index += count;
            public Memory<byte> GetMemory(int sizeHint = 0) { Grow(sizeHint); return new Memory<byte>(_buffer, _index, _buffer.Length - _index); }
            public Span<byte> GetSpan(int sizeHint = 0) { Grow(sizeHint); return new Span<byte>(_buffer, _index, _buffer.Length - _index); }
            private void Grow(int sizeHint)
            {
                if (sizeHint <= 0) sizeHint = 1;
                while (_buffer.Length - _index < sizeHint) Array.Resize(ref _buffer, _buffer.Length * 2);
            }
        }

        [Fact]
        public void BufferWriterBackendReportsACycle()
        {
            var model = CreateModel();
            var bw = new MinimalBufferWriter();
            var value = CreateCycle();
            var ex = Assert.ThrowsAny<Exception>(() => model.Serialize<Node>(bw, value));
            Assert.Contains("recursion", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
#endif

        [ProtoContract]
        public class Hooked
        {
            [ProtoMember(1)] public int Value { get; set; }
            [ProtoMember(2)] public Hooked Child { get; set; }

            [ProtoIgnore] public static int BeforeCount;

            [ProtoBeforeSerialization]
            public void OnBefore() => BeforeCount++;
        }

        /// <summary>
        /// A serialization callback must fire ONCE per serialize, whatever the destination.
        /// </summary>
        /// <remarks>
        /// It does not today: the buffer-writer backend is measure-first, and its measure is a
        /// real write to the null writer, so every callback runs twice - once to size the message
        /// and once to write it. The stream backend reserves-and-back-fills instead, so it runs
        /// them once. Nothing caught this because the corpus differential compares BYTES, and a
        /// callback that does not mutate leaves the bytes identical.
        /// </remarks>
        [Fact]
        public void SerializationCallbackFiresOncePerBackend()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(Hooked), true);
            var value = new Hooked { Value = 1, Child = new Hooked { Value = 2 } };

            Hooked.BeforeCount = 0;
            using (var ms = new MemoryStream()) model.Serialize(ms, value);
            var viaStream = Hooked.BeforeCount;

            Assert.Equal(2, viaStream); // two instances, one callback each - correct

#if PLAT_SPANS
            Hooked.BeforeCount = 0;
            model.Serialize<Hooked>(new MinimalBufferWriter(), value);
            var viaBufferWriter = Hooked.BeforeCount;

            // PINNING A DIVERGENCE, not endorsing it. 3, not 2: the root is never measured
            // (roots carry no length prefix), so it fires once - but every NESTED message is
            // measured and then written, so it fires twice. The count is the proof of the
            // mechanism. Change this to Assert.Equal(viaStream, viaBufferWriter) when the
            // measure pass stops running user callbacks; see docs/nano-writer.md.
            Assert.Equal(3, viaBufferWriter);
            Assert.NotEqual(viaStream, viaBufferWriter);
#endif
        }

        /// <summary>
        /// Depth, as distinct from a cycle: a long-but-finite chain past MaxDepth must also be
        /// reported rather than run the stack out.
        /// </summary>
        [Fact]
        public void DeepChainIsReportedRatherThanOverflowing()
        {
            var model = CreateModel();
            model.MaxDepth = 20;

            var head = new Node { Value = 0 };
            var tail = head;
            for (int i = 1; i < 200; i++)
            {
                tail.Next = new Node { Value = i };
                tail = tail.Next;
            }

            using var ms = new MemoryStream();
            Assert.ThrowsAny<Exception>(() => model.Serialize(ms, head));
        }
    }
}
