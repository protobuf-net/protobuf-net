// gap B6's message-valued maps, measured rather than reasoned about - because B41 stage 1 taught
// this arc that a contract can pass every correctness gate and still be SLOWER, and this has the
// same shape as the thing that caught it out.
//
// The concern, stated so the numbers can refute it: a map's WRITE stays on the stateful
// MapSerializer whatever we do. So a contract holding a message-valued map now walks each value
// arithmetically for its OWN length prefix, and the engine then asks IMeasuringSerializer again per
// value when it writes it. That is two arithmetic walks where stage 1's mistake was exactly one
// too many.
//
// The counter-argument, which is what this is testing: before the arm existed, the map member made
// the whole contract unmeasurable, so the contract's own length came from a full WRITE-TO-COUNT
// crawl - and an arithmetic walk is much cheaper than a crawl. Whether that dominates the extra
// walk is a measurement, not an opinion.
#if NET8_0_OR_GREATER
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;

namespace Benchmark
{
    [SimpleJob(RuntimeMoniker.Net80), MemoryDiagnoser]
    public class MapValueMeasureBenchmarks
    {
        /// <summary>Entries in the map - the axis the extra walk scales on.</summary>
        [Params(1, 8, 64)] public int Entries { get; set; }

        private MapCarrier _value;
        private WideMapCarrier _wide;
        private MapHolder _nested;
        private TypeModel _classic, _generated;
        private InheritanceDepthBenchmarks.ReusableWriter _buffer;

        [GlobalSetup]
        public void Setup()
        {
            _value = new MapCarrier { Sequence = 42, Tag = "carrier", Notes = new() };
            for (var i = 0; i < Entries; i++)
            {
                _value.Notes[i] = new MapNote { Text = "note-" + i, Rank = i, Extra = "x" + i };
            }

            _wide = new WideMapCarrier { Sequence = 42, Tag = "carrier", Notes = new() };
            for (var i = 0; i < Entries; i++)
            {
                _wide.Notes[i] = new MapNote { Text = "note-" + i, Rank = i, Extra = "x" + i };
            }
            for (var i = 0; i < 12; i++)
            {
                typeof(WideMapCarrier).GetProperty("N" + i).SetValue(_wide, i + 1);
                typeof(WideMapCarrier).GetProperty("S" + i).SetValue(_wide, "value" + i);
            }

            _classic = RuntimeTypeModel.Create();
            ((RuntimeTypeModel)_classic).Add(typeof(MapCarrier), true);
            ((RuntimeTypeModel)_classic).Add(typeof(WideMapCarrier), true);
            ((RuntimeTypeModel)_classic).Add(typeof(MapHolder), true);
            ((RuntimeTypeModel)_classic).CompileInPlace();
            _generated = (TypeModel)Activator.CreateInstance(typeof(MapValueModel), nonPublic: true);
            _buffer = new InheritanceDepthBenchmarks.ReusableWriter();

            // a perf number for two engines that disagree on the bytes is worthless, and a
            // generated model that silently dropped the map would otherwise just look fast
            static byte[] Bytes(TypeModel model, object value)
            {
                using var ms = new MemoryStream();
                model.Serialize(ms, value);
                return ms.ToArray();
            }
            _nested = new MapHolder { Inner = _value, Tag = 5 };
            foreach (var value in new object[] { _value, _wide, _nested })
            {
                var a = Bytes(_classic, value);
                var b = Bytes(_generated, value);
                if (a.Length == 0) throw new InvalidOperationException("nothing was written");
                if (BitConverter.ToString(a) != BitConverter.ToString(b))
                {
                    throw new InvalidOperationException("engines disagree for " + value.GetType().Name);
                }
            }
        }

        private long ToBuffer(TypeModel model, object value)
        {
            _buffer.Reset();
            model.Serialize(_buffer, value);
            return 0;
        }

        [Benchmark(Baseline = true, Description = "classic, 2 other members")]
        public long Classic() => ToBuffer(_classic, _value);

        [Benchmark(Description = "generated, 2 other members")]
        public long Generated() => ToBuffer(_generated, _value);

        [Benchmark(Description = "classic, 26 other members")]
        public long ClassicWide() => ToBuffer(_classic, _wide);

        [Benchmark(Description = "generated, 26 other members")]
        public long GeneratedWide() => ToBuffer(_generated, _wide);

        [Benchmark(Description = "classic, NESTED one level")]
        public long ClassicNested() => ToBuffer(_classic, _nested);

        [Benchmark(Description = "generated, NESTED one level")]
        public long GeneratedNested() => ToBuffer(_generated, _nested);
    }

    [ProtoContract]
    public class MapNote
    {
        [ProtoMember(1)] public string Text { get; set; }
        [ProtoMember(2)] public int Rank { get; set; }
        [ProtoMember(3)] public string Extra { get; set; }
    }

    /// <summary>A contract holding a message-valued map, plus ordinary members either side of it.</summary>
    [ProtoContract]
    public class MapCarrier
    {
        [ProtoMember(1)] public int Sequence { get; set; }
        [ProtoMember(2)] public Dictionary<int, MapNote> Notes { get; set; }
        [ProtoMember(3)] public string Tag { get; set; }
    }

    /// <summary>
    /// The same shape but WIDE - because the cascade's value is proportional to what ELSE the
    /// contract holds, which is the trap the inheritance work fell into first time round: one
    /// unmeasurable member takes the WHOLE contract off measure-first, so a carrier with two
    /// trivial members has almost nothing to win back and measures flat.
    /// </summary>
    [ProtoContract]
    public class WideMapCarrier
    {
        [ProtoMember(1)] public int Sequence { get; set; }
        [ProtoMember(2)] public Dictionary<int, MapNote> Notes { get; set; }
        [ProtoMember(3)] public string Tag { get; set; }
        [ProtoMember(4)] public int N0 { get; set; }
        [ProtoMember(5)] public int N1 { get; set; }
        [ProtoMember(6)] public int N2 { get; set; }
        [ProtoMember(7)] public int N3 { get; set; }
        [ProtoMember(8)] public int N4 { get; set; }
        [ProtoMember(9)] public int N5 { get; set; }
        [ProtoMember(10)] public int N6 { get; set; }
        [ProtoMember(11)] public int N7 { get; set; }
        [ProtoMember(12)] public int N8 { get; set; }
        [ProtoMember(13)] public int N9 { get; set; }
        [ProtoMember(14)] public int N10 { get; set; }
        [ProtoMember(15)] public int N11 { get; set; }
        [ProtoMember(16)] public string S0 { get; set; }
        [ProtoMember(17)] public string S1 { get; set; }
        [ProtoMember(18)] public string S2 { get; set; }
        [ProtoMember(19)] public string S3 { get; set; }
        [ProtoMember(20)] public string S4 { get; set; }
        [ProtoMember(21)] public string S5 { get; set; }
        [ProtoMember(22)] public string S6 { get; set; }
        [ProtoMember(23)] public string S7 { get; set; }
        [ProtoMember(24)] public string S8 { get; set; }
        [ProtoMember(25)] public string S9 { get; set; }
        [ProtoMember(26)] public string S10 { get; set; }
        [ProtoMember(27)] public string S11 { get; set; }
    }

    /// <summary>
    /// The carrier NESTED one level, which is where measurability actually pays.
    /// </summary>
    /// <remarks>
    /// At the ROOT nothing needs a length, and a contract can be raw-WRITING without being
    /// measurable - so making the carrier measurable buys its own members nothing there. The
    /// cascade is for REFERRERS: this outer contract needs a length prefix for Inner, and takes it
    /// from arithmetic when Inner is measurable and from a write-to-count crawl when it is not.
    /// </remarks>
    [ProtoContract]
    public class MapHolder
    {
        [ProtoMember(1)] public MapCarrier Inner { get; set; }
        [ProtoMember(2)] public int Tag { get; set; }
    }

    [ProtoModel]
    [ProtoSerializable(typeof(MapCarrier))]
    [ProtoSerializable(typeof(WideMapCarrier))]
    [ProtoSerializable(typeof(MapHolder))]
    public partial class MapValueModel : TypeModel { }
}
#endif
