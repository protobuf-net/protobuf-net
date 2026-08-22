// The inheritance perf sweep, which did not exist: grep for ProtoInclude across this project before
// 2026-08-22 and there were no hits at all. DelimitedEncodingBenchmarks compares length-prefixed
// against delimited, but only through ordinary sub-message MEMBERS - never through sub-type markers.
//
// That matters because the two multiply. B35 measured delimited at 4.5x length-prefixed for a nested
// member; a hierarchy writes one marker PER LAYER, so depth multiplies whatever the per-marker cost
// is. And hierarchies are entirely off measure-first (gap B41), so every marker's length today comes
// from a write-to-count crawl - which is exactly the cost delimited framing removes.
//
// This is the BASELINE for B41: without it there is nothing for the measure-first version to be
// compared against, and "it got faster" would be unfalsifiable.
//
// NOTE there is deliberately no Google.Protobuf column. It has no concept of inheritance, so it
// cannot appear in this matrix at all; every comparison here is internal - classic against
// generated, prefixed against delimited, shallow against deep.
#if NET8_0_OR_GREATER
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Buffers;
using System.IO;

namespace Benchmark
{
    /// <summary>
    /// Serializing a hierarchy, across depth and marker framing, on both engines.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Depth is the runtime type, not the declared one.</b> Every case serializes through the
    /// ROOT (<c>P0</c>/<c>D0</c>), so the writer dispatches down the <c>is</c> chain and emits one
    /// marker per layer; instantiating <c>P3</c> writes three markers, <c>P15</c> fifteen. That is
    /// what makes a single 16-deep chain serve all three depths.
    /// </para>
    /// <para>
    /// <b>The two families differ in exactly one thing</b> - <c>DataFormat.Group</c> on the
    /// <c>[ProtoInclude]</c> - so any gap between them is the framing and nothing else. A delimited
    /// marker needs no length, so it needs no measure; a length-prefixed one does, and today that
    /// means the stateful engine crawling the layer to count it.
    /// </para>
    /// </remarks>
    [SimpleJob(RuntimeMoniker.Net80), MemoryDiagnoser]
    public class InheritanceDepthBenchmarks
    {
        /// <summary>1 = no inheritance at all (the control), 4 = moderate, 16 = deep.</summary>
        [Params(1, 4, 16)] public int Depth { get; set; }

        private P0 _prefixed;
        private D0 _delimited;
        private TypeModel _classic, _generated;
        private ReusableWriter _buffer;

        [GlobalSetup]
        public void Setup()
        {
            _prefixed = (P0)Fill(Activator.CreateInstance(PrefixedTypeAt(Depth)));
            _delimited = (D0)Fill(Activator.CreateInstance(DelimitedTypeAt(Depth)));

            _classic = RuntimeTypeModel.Create();
            ((RuntimeTypeModel)_classic).Add(typeof(P0), true);
            ((RuntimeTypeModel)_classic).Add(typeof(D0), true);
            ((RuntimeTypeModel)_classic).CompileInPlace();

            _generated = (TypeModel)Activator.CreateInstance(typeof(InheritanceModel), nonPublic: true);
            _buffer = new ReusableWriter();

            // self-policing: a perf number for two engines that disagree on the bytes is worthless,
            // and a generated model that silently DROPPED the hierarchy would otherwise just look
            // fast. Both are caught here rather than assumed.
            AssertSame(_prefixed);
            AssertSame(_delimited);
        }

        private void AssertSame(object value)
        {
            static byte[] Bytes(TypeModel model, object value)
            {
                using var ms = new MemoryStream();
                model.Serialize(ms, value);
                return ms.ToArray();
            }
            var classic = Bytes(_classic, value);
            var generated = Bytes(_generated, value);
            if (classic.Length == 0) throw new InvalidOperationException("nothing was written for " + value.GetType().Name);
            if (BitConverter.ToString(classic) != BitConverter.ToString(generated))
            {
                throw new InvalidOperationException($"engines disagree for {value.GetType().Name}:"
                    + Environment.NewLine + "classic   " + BitConverter.ToString(classic)
                    + Environment.NewLine + "generated " + BitConverter.ToString(generated));
            }
        }

        private static Type PrefixedTypeAt(int depth) => depth switch
        {
            1 => typeof(P0),
            4 => typeof(P3),
            _ => typeof(P15),
        };

        private static Type DelimitedTypeAt(int depth) => depth switch
        {
            1 => typeof(D0),
            4 => typeof(D3),
            _ => typeof(D15),
        };

        /// <summary>Every layer gets a value, so no layer is skipped by a trivial-value guard.</summary>
        private static object Fill(object value)
        {
            var type = value.GetType();
            var seed = 1;
            while (type is not null && type != typeof(object))
            {
                foreach (var property in type.GetProperties(System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
                {
                    if (property.PropertyType == typeof(int)) property.SetValue(value, seed++);
                    else if (property.PropertyType == typeof(string)) property.SetValue(value, "layer" + seed);
                }
                type = type.BaseType;
            }
            return value;
        }

        private sealed class ReusableWriter : IBufferWriter<byte>
        {
            private byte[] _array = new byte[64 * 1024];
            private int _index;
            public void Reset() => _index = 0;
            public void Advance(int count) => _index += count;
            public Memory<byte> GetMemory(int sizeHint = 0) => _array.AsMemory(_index);
            public Span<byte> GetSpan(int sizeHint = 0) => _array.AsSpan(_index);
        }

        private long ToBuffer(TypeModel model, object value)
        {
            _buffer.Reset();
            model.Serialize(_buffer, value);
            return 0;
        }

        [Benchmark(Baseline = true, Description = "classic, length-prefixed markers")]
        public long Classic_Prefixed() => ToBuffer(_classic, _prefixed);

        [Benchmark(Description = "classic, delimited markers")]
        public long Classic_Delimited() => ToBuffer(_classic, _delimited);

        [Benchmark(Description = "generated, length-prefixed markers")]
        public long Generated_Prefixed() => ToBuffer(_generated, _prefixed);

        [Benchmark(Description = "generated, delimited markers")]
        public long Generated_Delimited() => ToBuffer(_generated, _delimited);
    }

    [ProtoModel]
    [ProtoSerializable(typeof(P0))]
    [ProtoSerializable(typeof(D0))]
    public partial class InheritanceModel : TypeModel { }
}
#endif
