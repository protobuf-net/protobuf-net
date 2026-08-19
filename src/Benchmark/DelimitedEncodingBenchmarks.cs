using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Google.Protobuf;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace Benchmark
{
    /// <summary>
    /// Length-prefixed vs delimited (group) sub-message framing, in protobuf-net and in
    /// Google.Protobuf. Same shape, same data, byte-identical output within each framing; the two
    /// contracts differ only in the DataFormat of their sub-message members.
    /// <para>
    /// The protobuf-net side goes through the compile-time model (<see cref="DelimitedModel"/>),
    /// which is what v4 wires at build rather than at runtime.
    /// </para>
    /// <para>See docs/editions.md.</para>
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net80), MemoryDiagnoser]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    public class DelimitedEncodingBenchmarks
    {
        [ProtoContract]
        public class LengthPrefixedNode
        {
            [ProtoMember(1)] public int Value { get; set; }
            [ProtoMember(2)] public LengthPrefixedNode Child { get; set; }
            [ProtoMember(3)] public List<LengthPrefixedNode> Children { get; set; }
        }

        [ProtoContract]
        public class DelimitedNode
        {
            [ProtoMember(1)] public int Value { get; set; }
            [ProtoMember(2, DataFormat = DataFormat.Group)] public DelimitedNode Child { get; set; }
            [ProtoMember(3, DataFormat = DataFormat.Group)] public List<DelimitedNode> Children { get; set; }
        }

        /// <summary>Nesting depth for the deep tests, and child count for the wide tests.</summary>
        [Params(8, 64, 512)]
        public int Size { get; set; }

        // both libraries cap nesting when reading (protobuf-net at 512, Google.Protobuf at 100);
        // the deep cases here are about framing, not about those limits, so both are raised
        private const int RecursionLimit = 1024;

        private TypeModel _pbn;
        private LengthPrefixedNode _pbnDeep, _pbnWide;
        private DelimitedNode _pbnDeepDelimited, _pbnWideDelimited;
        private Delimited.LengthPrefixedNode _googleDeep, _googleWide;
        private Delimited.DelimitedNode _googleDeepDelimited, _googleWideDelimited;

        private byte[] _deepLengthPrefixedBytes, _deepDelimitedBytes, _wideLengthPrefixedBytes, _wideDelimitedBytes;
        private MemoryStream _stream;
        private ReusableBufferWriter _bufferWriter;

        [GlobalSetup]
        public void Setup()
        {
            _pbn = DelimitedModel.Instance;
            _pbn.MaxDepth = RecursionLimit;

            // non-zero values throughout: protobuf-net suppresses default values and editions
            // defaults to explicit presence, and we want the two writing identical bytes

            _pbnDeep = new LengthPrefixedNode { Value = 1 };
            var tail = _pbnDeep;
            for (int i = 1; i < Size; i++) tail = tail.Child = new LengthPrefixedNode { Value = i + 1 };

            _pbnDeepDelimited = new DelimitedNode { Value = 1 };
            var delimitedTail = _pbnDeepDelimited;
            for (int i = 1; i < Size; i++) delimitedTail = delimitedTail.Child = new DelimitedNode { Value = i + 1 };

            var children = new List<LengthPrefixedNode>(Size);
            var delimitedChildren = new List<DelimitedNode>(Size);
            for (int i = 0; i < Size; i++)
            {
                children.Add(new LengthPrefixedNode { Value = i + 1 });
                delimitedChildren.Add(new DelimitedNode { Value = i + 1 });
            }
            _pbnWide = new LengthPrefixedNode { Value = 1, Children = children };
            _pbnWideDelimited = new DelimitedNode { Value = 1, Children = delimitedChildren };

            _googleDeep = new Delimited.LengthPrefixedNode { Value = 1 };
            var googleTail = _googleDeep;
            for (int i = 1; i < Size; i++) googleTail = googleTail.Child = new Delimited.LengthPrefixedNode { Value = i + 1 };

            _googleDeepDelimited = new Delimited.DelimitedNode { Value = 1 };
            var googleDelimitedTail = _googleDeepDelimited;
            for (int i = 1; i < Size; i++) googleDelimitedTail = googleDelimitedTail.Child = new Delimited.DelimitedNode { Value = i + 1 };

            _googleWide = new Delimited.LengthPrefixedNode { Value = 1 };
            _googleWideDelimited = new Delimited.DelimitedNode { Value = 1 };
            for (int i = 0; i < Size; i++)
            {
                _googleWide.Children.Add(new Delimited.LengthPrefixedNode { Value = i + 1 });
                _googleWideDelimited.Children.Add(new Delimited.DelimitedNode { Value = i + 1 });
            }

            _stream = new MemoryStream();
            _bufferWriter = new ReusableBufferWriter();

            _deepLengthPrefixedBytes = ToBytes(_pbn, _pbnDeep);
            _deepDelimitedBytes = ToBytes(_pbn, _pbnDeepDelimited);
            _wideLengthPrefixedBytes = ToBytes(_pbn, _pbnWide);
            _wideDelimitedBytes = ToBytes(_pbn, _pbnWideDelimited);

            // the two libraries must agree byte-for-byte, or the columns are not comparable
            Verify(_deepLengthPrefixedBytes, _googleDeep.ToByteArray(), nameof(_deepLengthPrefixedBytes));
            Verify(_deepDelimitedBytes, _googleDeepDelimited.ToByteArray(), nameof(_deepDelimitedBytes));
            Verify(_wideLengthPrefixedBytes, _googleWide.ToByteArray(), nameof(_wideLengthPrefixedBytes));
            Verify(_wideDelimitedBytes, _googleWideDelimited.ToByteArray(), nameof(_wideDelimitedBytes));

            static byte[] ToBytes<T>(TypeModel model, T value)
            {
                using var ms = new MemoryStream();
                model.Serialize(ms, value);
                return ms.ToArray();
            }

            static void Verify(byte[] pbn, byte[] google, string what)
            {
                if (!pbn.AsSpan().SequenceEqual(google))
                {
                    throw new InvalidOperationException($"{what}: protobuf-net wrote {pbn.Length} bytes, Google.Protobuf wrote {google.Length}");
                }
            }
        }

        private long SerializePbn<T>(T value)
        {
            _stream.Position = 0;
            _stream.SetLength(0);
            _pbn.Serialize(_stream, value);
            return _stream.Length;
        }

        private long SerializeGoogle(IMessage value)
        {
            _stream.Position = 0;
            _stream.SetLength(0);
            using (var output = new CodedOutputStream(_stream, leaveOpen: true))
            {
                value.WriteTo(output);
            }
            return _stream.Length;
        }

        // the other write strategy: an IBufferWriter is the caller's buffer, so the writer must be
        // strictly forwards-only - a length prefix has to be MEASURED first rather than back-filled
        private long SerializePbnBuffer<T>(T value)
        {
            _bufferWriter.Reset();
            _pbn.Serialize(_bufferWriter, value);
            return _bufferWriter.WrittenCount;
        }

        private long SerializeGoogleBuffer(IMessage value)
        {
            _bufferWriter.Reset();
            value.WriteTo(_bufferWriter);
            return _bufferWriter.WrittenCount;
        }

        private T ParseGoogle<T>(MessageParser<T> parser, byte[] payload) where T : IMessage<T>
        {
            _stream.Position = 0;
            _stream.SetLength(0);
            _stream.Write(payload, 0, payload.Length);
            _stream.Position = 0;
            return parser.ParseFrom(CodedInputStream.CreateWithLimits(_stream, int.MaxValue, RecursionLimit));
        }

        [Benchmark(Baseline = true), BenchmarkCategory("SerializeDeep")]
        public long SerializeDeep_ProtobufNet_LengthPrefixed() => SerializePbn(_pbnDeep);

        [Benchmark, BenchmarkCategory("SerializeDeep")]
        public long SerializeDeep_ProtobufNet_Delimited() => SerializePbn(_pbnDeepDelimited);

        [Benchmark, BenchmarkCategory("SerializeDeep")]
        public long SerializeDeep_Google_LengthPrefixed() => SerializeGoogle(_googleDeep);

        [Benchmark, BenchmarkCategory("SerializeDeep")]
        public long SerializeDeep_Google_Delimited() => SerializeGoogle(_googleDeepDelimited);

        [Benchmark(Baseline = true), BenchmarkCategory("SerializeWide")]
        public long SerializeWide_ProtobufNet_LengthPrefixed() => SerializePbn(_pbnWide);

        [Benchmark, BenchmarkCategory("SerializeWide")]
        public long SerializeWide_ProtobufNet_Delimited() => SerializePbn(_pbnWideDelimited);

        [Benchmark, BenchmarkCategory("SerializeWide")]
        public long SerializeWide_Google_LengthPrefixed() => SerializeGoogle(_googleWide);

        [Benchmark, BenchmarkCategory("SerializeWide")]
        public long SerializeWide_Google_Delimited() => SerializeGoogle(_googleWideDelimited);

        [Benchmark(Baseline = true), BenchmarkCategory("DeserializeDeep")]
        public LengthPrefixedNode DeserializeDeep_ProtobufNet_LengthPrefixed()
            => _pbn.Deserialize<LengthPrefixedNode>(_deepLengthPrefixedBytes.AsMemory());

        [Benchmark, BenchmarkCategory("DeserializeDeep")]
        public DelimitedNode DeserializeDeep_ProtobufNet_Delimited()
            => _pbn.Deserialize<DelimitedNode>(_deepDelimitedBytes.AsMemory());

        [Benchmark, BenchmarkCategory("DeserializeDeep")]
        public Delimited.LengthPrefixedNode DeserializeDeep_Google_LengthPrefixed()
            => ParseGoogle(Delimited.LengthPrefixedNode.Parser, _deepLengthPrefixedBytes);

        [Benchmark, BenchmarkCategory("DeserializeDeep")]
        public Delimited.DelimitedNode DeserializeDeep_Google_Delimited()
            => ParseGoogle(Delimited.DelimitedNode.Parser, _deepDelimitedBytes);

        [Benchmark(Baseline = true), BenchmarkCategory("DeserializeWide")]
        public LengthPrefixedNode DeserializeWide_ProtobufNet_LengthPrefixed()
            => _pbn.Deserialize<LengthPrefixedNode>(_wideLengthPrefixedBytes.AsMemory());

        [Benchmark, BenchmarkCategory("DeserializeWide")]
        public DelimitedNode DeserializeWide_ProtobufNet_Delimited()
            => _pbn.Deserialize<DelimitedNode>(_wideDelimitedBytes.AsMemory());

        [Benchmark, BenchmarkCategory("DeserializeWide")]
        public Delimited.LengthPrefixedNode DeserializeWide_Google_LengthPrefixed()
            => ParseGoogle(Delimited.LengthPrefixedNode.Parser, _wideLengthPrefixedBytes);

        [Benchmark, BenchmarkCategory("DeserializeWide")]
        public Delimited.DelimitedNode DeserializeWide_Google_Delimited()
            => ParseGoogle(Delimited.DelimitedNode.Parser, _wideDelimitedBytes);

        [Benchmark(Baseline = true), BenchmarkCategory("BufferDeep")]
        public long BufferDeep_ProtobufNet_LengthPrefixed() => SerializePbnBuffer(_pbnDeep);

        [Benchmark, BenchmarkCategory("BufferDeep")]
        public long BufferDeep_ProtobufNet_Delimited() => SerializePbnBuffer(_pbnDeepDelimited);

        [Benchmark, BenchmarkCategory("BufferDeep")]
        public long BufferDeep_Google_LengthPrefixed() => SerializeGoogleBuffer(_googleDeep);

        [Benchmark, BenchmarkCategory("BufferDeep")]
        public long BufferDeep_Google_Delimited() => SerializeGoogleBuffer(_googleDeepDelimited);

        [Benchmark(Baseline = true), BenchmarkCategory("BufferWide")]
        public long BufferWide_ProtobufNet_LengthPrefixed() => SerializePbnBuffer(_pbnWide);

        [Benchmark, BenchmarkCategory("BufferWide")]
        public long BufferWide_ProtobufNet_Delimited() => SerializePbnBuffer(_pbnWideDelimited);

        [Benchmark, BenchmarkCategory("BufferWide")]
        public long BufferWide_Google_LengthPrefixed() => SerializeGoogleBuffer(_googleWide);

        [Benchmark, BenchmarkCategory("BufferWide")]
        public long BufferWide_Google_Delimited() => SerializeGoogleBuffer(_googleWideDelimited);

        /// <summary>
        /// A minimal reusable <see cref="IBufferWriter{T}"/> over a single array, so the buffer
        /// target costs nothing per operation and the measurement is of the writers, not of us.
        /// </summary>
        private sealed class ReusableBufferWriter : IBufferWriter<byte>
        {
            private byte[] _buffer = new byte[64 * 1024];
            public int WrittenCount { get; private set; }
            public void Reset() => WrittenCount = 0;

            public void Advance(int count) => WrittenCount += count;

            public Memory<byte> GetMemory(int sizeHint = 0)
            {
                Ensure(sizeHint);
                return new Memory<byte>(_buffer, WrittenCount, _buffer.Length - WrittenCount);
            }

            public Span<byte> GetSpan(int sizeHint = 0)
            {
                Ensure(sizeHint);
                return new Span<byte>(_buffer, WrittenCount, _buffer.Length - WrittenCount);
            }

            private void Ensure(int sizeHint)
            {
                if (sizeHint < 1) sizeHint = 1;
                if (_buffer.Length - WrittenCount >= sizeHint) return;
                var bigger = new byte[Math.Max(_buffer.Length * 2, WrittenCount + sizeHint)];
                Buffer.BlockCopy(_buffer, 0, bigger, 0, WrittenCount);
                _buffer = bigger;
            }
        }
    }

    /// <summary>
    /// The compile-time model for <see cref="DelimitedEncodingBenchmarks"/>; the generator fills
    /// this in at build from the declared roots.
    /// </summary>
    [ProtoModel]
    [ProtoSerializable(typeof(DelimitedEncodingBenchmarks.LengthPrefixedNode))]
    [ProtoSerializable(typeof(DelimitedEncodingBenchmarks.DelimitedNode))]
    public partial class DelimitedModel : TypeModel
    {
    }
}
