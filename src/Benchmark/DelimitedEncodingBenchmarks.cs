using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Google.Protobuf;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;

namespace Benchmark
{
    /// <summary>
    /// Length-prefixed vs delimited (group) sub-message framing, in protobuf-net and in
    /// Google.Protobuf. Same shape, same data, byte-identical output within each framing; the
    /// models differ only in how sub-messages are framed.
    /// <para>
    /// A delimited writer emits a start tag, the body, and an end tag, and never looks back. A
    /// length-prefixed one has to know the body length before it can write it: protobuf-net's
    /// stream writer reserves a byte for the prefix, takes a flush lock (nothing can go to the
    /// underlying stream while a sub-item is open), and back-fills at the end, shuffling the body
    /// along when the length outgrows the byte reserved for it. So the cost tracks nesting depth
    /// and sub-message count.
    /// </para>
    /// <para>
    /// Google.Protobuf is here for scale, not as a scoreboard - and its own numbers make the point
    /// more loudly than protobuf-net's do: its generated WriteTo calls CalculateSize() on each
    /// sub-message before writing it, and that walks the whole subtree, so length-prefixing a chain
    /// of n nested messages is O(n squared). The delimited path never asks for a length, and is
    /// linear. Deep serialization at Size=512 was ~180x apart when this was written.
    /// </para>
    /// <para>
    /// Allocations are not like-for-like: protobuf-net's serialize path allocates nothing, while
    /// Google's API creates a CodedOutputStream / CodedInputStream, and its buffer, per call.
    /// </para>
    /// <para>See docs/editions.md, which quotes these numbers.</para>
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net80), MemoryDiagnoser]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    public class DelimitedEncodingBenchmarks
    {
        [ProtoContract]
        public class Node
        {
            [ProtoMember(1)] public int Value { get; set; }
            [ProtoMember(2)] public Node Child { get; set; }
            [ProtoMember(3)] public List<Node> Children { get; set; }
        }

        /// <summary>Nesting depth for the deep tests, and child count for the wide tests.</summary>
        [Params(8, 64, 512)]
        public int Size { get; set; }

        // both libraries cap nesting when reading (protobuf-net at 512, Google.Protobuf at 100);
        // the deep cases here are about framing, not about those limits, so both are raised
        private const int RecursionLimit = 1024;

        private TypeModel _pbnLengthPrefixed, _pbnDelimited;
        private Node _pbnDeep, _pbnWide;
        private Delimited.LengthPrefixedNode _googleDeep, _googleWide;
        private Delimited.DelimitedNode _googleDeepDelimited, _googleWideDelimited;

        private byte[] _deepLengthPrefixedBytes, _deepDelimitedBytes, _wideLengthPrefixedBytes, _wideDelimitedBytes;
        private MemoryStream _stream;

        private static TypeModel BuildModel(DataFormat dataFormat)
        {
            var model = RuntimeTypeModel.Create();
            model.MaxDepth = RecursionLimit;
            var node = model.Add<Node>();
            node[2].DataFormat = dataFormat; // Child
            node[3].DataFormat = dataFormat; // Children
            model.CompileInPlace();
            return model;
        }

        [GlobalSetup]
        public void Setup()
        {
            _pbnLengthPrefixed = BuildModel(DataFormat.Default);
            _pbnDelimited = BuildModel(DataFormat.Group);

            // non-zero values throughout: protobuf-net suppresses default values and editions
            // defaults to explicit presence, and we want the two writing identical bytes

            _pbnDeep = new Node { Value = 1 };
            var tail = _pbnDeep;
            for (int i = 1; i < Size; i++) tail = tail.Child = new Node { Value = i + 1 };

            var children = new List<Node>(Size);
            for (int i = 0; i < Size; i++) children.Add(new Node { Value = i + 1 });
            _pbnWide = new Node { Value = 1, Children = children };

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

            _deepLengthPrefixedBytes = ToBytes(_pbnLengthPrefixed, _pbnDeep);
            _deepDelimitedBytes = ToBytes(_pbnDelimited, _pbnDeep);
            _wideLengthPrefixedBytes = ToBytes(_pbnLengthPrefixed, _pbnWide);
            _wideDelimitedBytes = ToBytes(_pbnDelimited, _pbnWide);

            static byte[] ToBytes(TypeModel model, Node value)
            {
                using var ms = new MemoryStream();
                model.Serialize(ms, value);
                return ms.ToArray();
            }
        }

        private long Serialize(TypeModel model, Node value)
        {
            _stream.Position = 0;
            _stream.SetLength(0);
            model.Serialize(_stream, value);
            return _stream.Length;
        }

        private long Serialize(IMessage value)
        {
            _stream.Position = 0;
            _stream.SetLength(0);
            using (var output = new CodedOutputStream(_stream, leaveOpen: true))
            {
                value.WriteTo(output);
            }
            return _stream.Length;
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
        public long SerializeDeep_ProtobufNet_LengthPrefixed() => Serialize(_pbnLengthPrefixed, _pbnDeep);

        [Benchmark, BenchmarkCategory("SerializeDeep")]
        public long SerializeDeep_ProtobufNet_Delimited() => Serialize(_pbnDelimited, _pbnDeep);

        [Benchmark, BenchmarkCategory("SerializeDeep")]
        public long SerializeDeep_Google_LengthPrefixed() => Serialize(_googleDeep);

        [Benchmark, BenchmarkCategory("SerializeDeep")]
        public long SerializeDeep_Google_Delimited() => Serialize(_googleDeepDelimited);

        [Benchmark(Baseline = true), BenchmarkCategory("SerializeWide")]
        public long SerializeWide_ProtobufNet_LengthPrefixed() => Serialize(_pbnLengthPrefixed, _pbnWide);

        [Benchmark, BenchmarkCategory("SerializeWide")]
        public long SerializeWide_ProtobufNet_Delimited() => Serialize(_pbnDelimited, _pbnWide);

        [Benchmark, BenchmarkCategory("SerializeWide")]
        public long SerializeWide_Google_LengthPrefixed() => Serialize(_googleWide);

        [Benchmark, BenchmarkCategory("SerializeWide")]
        public long SerializeWide_Google_Delimited() => Serialize(_googleWideDelimited);

        [Benchmark(Baseline = true), BenchmarkCategory("DeserializeDeep")]
        public Node DeserializeDeep_ProtobufNet_LengthPrefixed()
            => _pbnLengthPrefixed.Deserialize<Node>(_deepLengthPrefixedBytes.AsMemory());

        [Benchmark, BenchmarkCategory("DeserializeDeep")]
        public Node DeserializeDeep_ProtobufNet_Delimited()
            => _pbnDelimited.Deserialize<Node>(_deepDelimitedBytes.AsMemory());

        [Benchmark, BenchmarkCategory("DeserializeDeep")]
        public Delimited.LengthPrefixedNode DeserializeDeep_Google_LengthPrefixed()
            => ParseGoogle(Delimited.LengthPrefixedNode.Parser, _deepLengthPrefixedBytes);

        [Benchmark, BenchmarkCategory("DeserializeDeep")]
        public Delimited.DelimitedNode DeserializeDeep_Google_Delimited()
            => ParseGoogle(Delimited.DelimitedNode.Parser, _deepDelimitedBytes);

        [Benchmark(Baseline = true), BenchmarkCategory("DeserializeWide")]
        public Node DeserializeWide_ProtobufNet_LengthPrefixed()
            => _pbnLengthPrefixed.Deserialize<Node>(_wideLengthPrefixedBytes.AsMemory());

        [Benchmark, BenchmarkCategory("DeserializeWide")]
        public Node DeserializeWide_ProtobufNet_Delimited()
            => _pbnDelimited.Deserialize<Node>(_wideDelimitedBytes.AsMemory());

        [Benchmark, BenchmarkCategory("DeserializeWide")]
        public Delimited.LengthPrefixedNode DeserializeWide_Google_LengthPrefixed()
            => ParseGoogle(Delimited.LengthPrefixedNode.Parser, _wideLengthPrefixedBytes);

        [Benchmark, BenchmarkCategory("DeserializeWide")]
        public Delimited.DelimitedNode DeserializeWide_Google_Delimited()
            => ParseGoogle(Delimited.DelimitedNode.Parser, _wideDelimitedBytes);
    }
}
