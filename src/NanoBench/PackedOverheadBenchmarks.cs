using BenchmarkDotNet.Attributes;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Buffers;

namespace ProtoBuf.Nano.Bench;

/// <summary>
/// Is there a fixed per-member cost at all? — <c>notes/packed-writes.md</c>, item 3.
/// </summary>
/// <remarks>
/// <para>
/// That note records "every member costs roughly a microsecond regardless of payload size" and
/// ranks it the largest remaining number. The evidence given was a division: the fixed-int
/// category came to 965 ns/member and 0.161 ns/byte. But those are the <b>same number</b> — each
/// member there carries about 6 KB, and 6000 × 0.161 ≈ 965 — so the figure is consistent with a
/// fixed cost per member <i>and</i> with none at all, and cannot distinguish them.
/// </para>
/// <para>
/// The way to separate them is to <b>shrink the payload and look at the intercept</b>. A genuine
/// fixed cost stays put as the element count falls; a per-byte cost vanishes with it. Members are
/// held constant at one and the element count is swept, so nothing but the payload moves.
/// </para>
/// <para>
/// <c>Empty</c> is the control: the same contract with the member left null, so the model still
/// dispatches a contract and writes a root but touches no collection. Whatever separates
/// <c>Count = 1</c> from <c>Empty</c> is the cost of <i>having</i> a member; whatever separates
/// <c>Empty</c> from zero is the cost of the call itself.
/// </para>
/// </remarks>
[MemoryDiagnoser(false)]
public class PackedOverheadBenchmarks
{
    [ProtoContract]
    public class OneMember
    {
        [ProtoMember(1, IsPacked = true)] public int[] Values { get; set; }
    }

    /// <summary>Four members of the same shape, to test whether the cost scales with the count.</summary>
    [ProtoContract]
    public class FourMembers
    {
        [ProtoMember(1, IsPacked = true)] public int[] A { get; set; }
        [ProtoMember(2, IsPacked = true)] public int[] B { get; set; }
        [ProtoMember(3, IsPacked = true)] public int[] C { get; set; }
        [ProtoMember(4, IsPacked = true)] public int[] D { get; set; }
    }

    private readonly TypeModel _generated = OverheadModel.Instance;

    /// <summary>Object-typed dispatch on a GENERATED model, to separate the two variables.</summary>
    [Benchmark]
    public long FourMemberGenObject() => WriteWith<object>(_generated, _four);

    [Benchmark]
    public long FourMemberGenGeneric() => WriteWith(_generated, _four);

    private long WriteWith<T>(TypeModel model, T value)
    {
        _sink.Reset();
        model.Serialize(_sink, value);
        return _sink.Written;
    }

    [Params(0, 1, 4, 16, 64, 256, 999)]
    public int Count { get; set; }

    private readonly TypeModel _model = RuntimeTypeModel.Create();
    private OneMember _one;
    private FourMembers _four;
    private readonly OneMember _empty = new();
    private readonly Sink _sink = new();

    [GlobalSetup]
    public void Setup()
    {
        var model = (RuntimeTypeModel)_model;
        model.Add(typeof(OneMember), true);
        model.Add(typeof(FourMembers), true);

        // deliberately SMALL values: this measures the fixed cost, so the per-element work should
        // be as cheap as it ever gets - anything left over is the overhead being looked for
        var values = new int[Count];
        for (int i = 0; i < Count; i++) values[i] = i % 128;
        _one = new OneMember { Values = values };
        _four = new FourMembers { A = values, B = values, C = values, D = values };
    }

    private long Write(object value)
    {
        _sink.Reset();
        _model.Serialize<object>(_sink, value);
        return _sink.Written;
    }

    /// <summary>
    /// The same three writes through the <b>generic</b> entry point rather than the object-typed
    /// one. If these are dramatically cheaper, the fixed cost belongs to the API the harness chose
    /// and not to the model — which would make every end-to-end number in
    /// <c>notes/packed-writes.md</c> carry the same constant.
    /// </summary>
    [Benchmark]
    public long EmptyGeneric() => WriteTyped(_empty);

    [Benchmark]
    public long OneMemberGeneric() => WriteTyped(_one);

    [Benchmark]
    public long FourMemberGeneric() => WriteTyped(_four);

    private long WriteTyped<T>(T value)
    {
        _sink.Reset();
        _model.Serialize(_sink, value);
        return _sink.Written;
    }

    /// <summary>The floor: a contract with nothing to write.</summary>
    [Benchmark(Baseline = true)]
    public long Empty() => Write(_empty);

    [Benchmark]
    public long OneMemberWrite() => Write(_one);

    [Benchmark]
    public long FourMemberWrite() => Write(_four);

    private sealed class Sink : IBufferWriter<byte>
    {
        private readonly byte[] _buffer = new byte[1024 * 64];
        public int Written { get; private set; }
        public void Reset() => Written = 0;
        public void Advance(int count) => Written += count;
        public Memory<byte> GetMemory(int sizeHint = 0) => new(_buffer, Written, _buffer.Length - Written);
        public Span<byte> GetSpan(int sizeHint = 0) => new(_buffer, Written, _buffer.Length - Written);
    }
}

/// <summary>The same two contracts under a generated model, so "object API" and "runtime model"
/// can be told apart rather than confounded.</summary>
[ProtoModel]
[ProtoSerializable(typeof(PackedOverheadBenchmarks.OneMember))]
[ProtoSerializable(typeof(PackedOverheadBenchmarks.FourMembers))]
public partial class OverheadModel : ProtoBuf.Meta.TypeModel { }
