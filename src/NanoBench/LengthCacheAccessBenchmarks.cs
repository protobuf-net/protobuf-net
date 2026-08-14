using BenchmarkDotNet.Attributes;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Nano.Bench;

/// <summary>
/// How a generated measure/write body should reach the length cache: gap B16.
/// </summary>
/// <remarks>
/// <para>
/// The emitter currently writes <c>var lengthsN = state.RawLengths;</c> ONCE PER MESSAGE MEMBER.
/// Marc asked for three arms: as-is, one hoisted local, and no local at all - reading the property
/// at each use and letting the JIT inline it.
/// </para>
/// <para>
/// The access is three DEPENDENT field loads, mirrored exactly here:
/// <c>state._writer</c> (a ref-struct field) then <c>writer.netCache</c> then
/// <c>netCache._rawLengths</c>, the last two on the heap. Roslyn performs no CSE across
/// statements, so the IL really does repeat the chain; the question is whether the JIT can.
/// </para>
/// <para>
/// <b>The intervening calls are the whole design.</b> Without a real
/// <c>TryGetValue</c> between the sites the JIT would hoist every load and all three arms would
/// measure identically - a meaningless pass. The lookups HIT, which is the realistic case: the
/// emitter's own comment says an enclosing measure has usually already recorded the object.
/// </para>
/// </remarks>
[MemoryDiagnoser(false)]
public class LengthCacheAccessBenchmarks
{
    // the real chain: NetObjectCache holds the field, ProtoWriter forwards, State forwards again
    private sealed class FakeCache
    {
        private readonly Dictionary<object, long> _rawLengths = new();
        internal Dictionary<object, long> RawLengths => _rawLengths;
    }

    private sealed class FakeWriter
    {
        internal readonly FakeCache NetCache = new();
        internal Dictionary<object, long> RawLengths => NetCache.RawLengths;
    }

    private readonly ref struct FakeState
    {
        private readonly FakeWriter _writer;
        public FakeState(FakeWriter writer) => _writer = writer;
        public Dictionary<object, long> RawLengths => _writer.RawLengths;
    }

    private readonly FakeWriter _writer = new();
    private object[] _objs = [];

    [GlobalSetup]
    public void Setup()
    {
        _objs = new object[SITES];
        for (int i = 0; i < SITES; i++)
        {
            _objs[i] = new object();
            _writer.RawLengths[_objs[i]] = i + 1;   // pre-populated: the lookups HIT
        }
    }

    private const int SITES = 8;

    // the miss arm, never taken here, but opaque enough that the JIT cannot assume anything
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Measure(object value) => value.GetHashCode();

    [Benchmark(Baseline = true)]
    public long PerSiteLocal()
    {
        var state = new FakeState(_writer);
        long len = 0;
            var tmp0 = _objs[0];
            if (tmp0 != null)
            {
                var lengths0 = state.RawLengths;
                if (!lengths0.TryGetValue(tmp0, out var len0))
                {
                    len0 = Measure(tmp0);
                    lengths0[tmp0] = len0;
                }
                len += 2 + len0;
            }
            var tmp1 = _objs[1];
            if (tmp1 != null)
            {
                var lengths1 = state.RawLengths;
                if (!lengths1.TryGetValue(tmp1, out var len1))
                {
                    len1 = Measure(tmp1);
                    lengths1[tmp1] = len1;
                }
                len += 2 + len1;
            }
            var tmp2 = _objs[2];
            if (tmp2 != null)
            {
                var lengths2 = state.RawLengths;
                if (!lengths2.TryGetValue(tmp2, out var len2))
                {
                    len2 = Measure(tmp2);
                    lengths2[tmp2] = len2;
                }
                len += 2 + len2;
            }
            var tmp3 = _objs[3];
            if (tmp3 != null)
            {
                var lengths3 = state.RawLengths;
                if (!lengths3.TryGetValue(tmp3, out var len3))
                {
                    len3 = Measure(tmp3);
                    lengths3[tmp3] = len3;
                }
                len += 2 + len3;
            }
            var tmp4 = _objs[4];
            if (tmp4 != null)
            {
                var lengths4 = state.RawLengths;
                if (!lengths4.TryGetValue(tmp4, out var len4))
                {
                    len4 = Measure(tmp4);
                    lengths4[tmp4] = len4;
                }
                len += 2 + len4;
            }
            var tmp5 = _objs[5];
            if (tmp5 != null)
            {
                var lengths5 = state.RawLengths;
                if (!lengths5.TryGetValue(tmp5, out var len5))
                {
                    len5 = Measure(tmp5);
                    lengths5[tmp5] = len5;
                }
                len += 2 + len5;
            }
            var tmp6 = _objs[6];
            if (tmp6 != null)
            {
                var lengths6 = state.RawLengths;
                if (!lengths6.TryGetValue(tmp6, out var len6))
                {
                    len6 = Measure(tmp6);
                    lengths6[tmp6] = len6;
                }
                len += 2 + len6;
            }
            var tmp7 = _objs[7];
            if (tmp7 != null)
            {
                var lengths7 = state.RawLengths;
                if (!lengths7.TryGetValue(tmp7, out var len7))
                {
                    len7 = Measure(tmp7);
                    lengths7[tmp7] = len7;
                }
                len += 2 + len7;
            }
        return len;
    }

    [Benchmark]
    public long HoistedLocal()
    {
        var state = new FakeState(_writer);
        long len = 0;
        var lengths = state.RawLengths;
            var tmp0 = _objs[0];
            if (tmp0 != null)
            {
                if (!lengths.TryGetValue(tmp0, out var len0))
                {
                    len0 = Measure(tmp0);
                    lengths[tmp0] = len0;
                }
                len += 2 + len0;
            }
            var tmp1 = _objs[1];
            if (tmp1 != null)
            {
                if (!lengths.TryGetValue(tmp1, out var len1))
                {
                    len1 = Measure(tmp1);
                    lengths[tmp1] = len1;
                }
                len += 2 + len1;
            }
            var tmp2 = _objs[2];
            if (tmp2 != null)
            {
                if (!lengths.TryGetValue(tmp2, out var len2))
                {
                    len2 = Measure(tmp2);
                    lengths[tmp2] = len2;
                }
                len += 2 + len2;
            }
            var tmp3 = _objs[3];
            if (tmp3 != null)
            {
                if (!lengths.TryGetValue(tmp3, out var len3))
                {
                    len3 = Measure(tmp3);
                    lengths[tmp3] = len3;
                }
                len += 2 + len3;
            }
            var tmp4 = _objs[4];
            if (tmp4 != null)
            {
                if (!lengths.TryGetValue(tmp4, out var len4))
                {
                    len4 = Measure(tmp4);
                    lengths[tmp4] = len4;
                }
                len += 2 + len4;
            }
            var tmp5 = _objs[5];
            if (tmp5 != null)
            {
                if (!lengths.TryGetValue(tmp5, out var len5))
                {
                    len5 = Measure(tmp5);
                    lengths[tmp5] = len5;
                }
                len += 2 + len5;
            }
            var tmp6 = _objs[6];
            if (tmp6 != null)
            {
                if (!lengths.TryGetValue(tmp6, out var len6))
                {
                    len6 = Measure(tmp6);
                    lengths[tmp6] = len6;
                }
                len += 2 + len6;
            }
            var tmp7 = _objs[7];
            if (tmp7 != null)
            {
                if (!lengths.TryGetValue(tmp7, out var len7))
                {
                    len7 = Measure(tmp7);
                    lengths[tmp7] = len7;
                }
                len += 2 + len7;
            }
        return len;
    }

    [Benchmark]
    public long NoLocal()
    {
        var state = new FakeState(_writer);
        long len = 0;
            var tmp0 = _objs[0];
            if (tmp0 != null)
            {
                if (!state.RawLengths.TryGetValue(tmp0, out var len0))
                {
                    len0 = Measure(tmp0);
                    state.RawLengths[tmp0] = len0;
                }
                len += 2 + len0;
            }
            var tmp1 = _objs[1];
            if (tmp1 != null)
            {
                if (!state.RawLengths.TryGetValue(tmp1, out var len1))
                {
                    len1 = Measure(tmp1);
                    state.RawLengths[tmp1] = len1;
                }
                len += 2 + len1;
            }
            var tmp2 = _objs[2];
            if (tmp2 != null)
            {
                if (!state.RawLengths.TryGetValue(tmp2, out var len2))
                {
                    len2 = Measure(tmp2);
                    state.RawLengths[tmp2] = len2;
                }
                len += 2 + len2;
            }
            var tmp3 = _objs[3];
            if (tmp3 != null)
            {
                if (!state.RawLengths.TryGetValue(tmp3, out var len3))
                {
                    len3 = Measure(tmp3);
                    state.RawLengths[tmp3] = len3;
                }
                len += 2 + len3;
            }
            var tmp4 = _objs[4];
            if (tmp4 != null)
            {
                if (!state.RawLengths.TryGetValue(tmp4, out var len4))
                {
                    len4 = Measure(tmp4);
                    state.RawLengths[tmp4] = len4;
                }
                len += 2 + len4;
            }
            var tmp5 = _objs[5];
            if (tmp5 != null)
            {
                if (!state.RawLengths.TryGetValue(tmp5, out var len5))
                {
                    len5 = Measure(tmp5);
                    state.RawLengths[tmp5] = len5;
                }
                len += 2 + len5;
            }
            var tmp6 = _objs[6];
            if (tmp6 != null)
            {
                if (!state.RawLengths.TryGetValue(tmp6, out var len6))
                {
                    len6 = Measure(tmp6);
                    state.RawLengths[tmp6] = len6;
                }
                len += 2 + len6;
            }
            var tmp7 = _objs[7];
            if (tmp7 != null)
            {
                if (!state.RawLengths.TryGetValue(tmp7, out var len7))
                {
                    len7 = Measure(tmp7);
                    state.RawLengths[tmp7] = len7;
                }
                len += 2 + len7;
            }
        return len;
    }
}
