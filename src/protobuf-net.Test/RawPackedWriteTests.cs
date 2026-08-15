using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ProtoBuf.Test
{
    /// <summary>
    /// The raw packed write surface, gated against <c>RepeatedSerializer</c>'s bytes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the oracle that matters for the raw path: the generator will emit these calls in
    /// place of <c>WriteRepeated</c>, so "byte-identical to the stateful packed write" is the
    /// whole contract. A round trip cannot check it — it would agree with itself — and the AOT
    /// differential cannot either, until the generator is actually wired to this surface.
    /// </para>
    /// <para>
    /// The counts are chosen for the framing rules rather than for the SIMD block: <b>0</b> is the
    /// zero-length header, <b>1</b> is written UNPACKED with its own per-element header, and 2 is
    /// the smallest genuinely packed case. 31/32/33 straddle the blit's block, and 999 leaves a
    /// ragged tail. Every one of those is a distinct arm of <c>PackedTotal</c> or the blit.
    /// </para>
    /// </remarks>
    public class RawPackedWriteTests
    {
        [ProtoContract] public class U32 { [ProtoMember(3, IsPacked = true)] public uint[] V { get; set; } }
        [ProtoContract] public class I32 { [ProtoMember(3, IsPacked = true)] public int[] V { get; set; } }
        [ProtoContract] public class U64 { [ProtoMember(3, IsPacked = true)] public ulong[] V { get; set; } }
        [ProtoContract] public class I64 { [ProtoMember(3, IsPacked = true)] public long[] V { get; set; } }
        [ProtoContract] public class Bools { [ProtoMember(3, IsPacked = true)] public bool[] V { get; set; } }
        [ProtoContract] public class Floats { [ProtoMember(3, IsPacked = true)] public float[] V { get; set; } }
        [ProtoContract] public class Doubles { [ProtoMember(3, IsPacked = true)] public double[] V { get; set; } }

        // field 3, deliberately: a one-byte tag either way, but not 1, so a field number accidentally
        // hard-coded or shifted shows up rather than coinciding with the common case
        private const int Field = 3;

        public static IEnumerable<object[]> Counts()
        {
            foreach (var n in new[] { 0, 1, 2, 3, 31, 32, 33, 47, 64, 999 }) yield return [n];
        }

        [Theory, MemberData(nameof(Counts))]
        public void UnsignedVarintMatchesTheLibrary(int n)
        {
            var values = Build(n, i => (uint)(i % 200));           // straddles the 128 blit cutoff
            Assert.Equal(Reference<U32>(values), Raw((ref ProtoWriter.State s) => s.WriteRawPackedVarint(Field, values)));
            AssertMeasure<U32>(values, ProtoWriter.State.MeasureRawPackedVarint(Field, values));
        }

        [Theory, MemberData(nameof(Counts))]
        public void SignedVarintMatchesTheLibrary(int n)
        {
            // a quarter negative: the ten-byte sign-extended form, which must never be blitted
            var values = Build(n, i => (i & 3) == 3 ? -(i + 1) : i % 200);
            Assert.Equal(Reference<I32>(values), Raw((ref ProtoWriter.State s) => s.WriteRawPackedVarint(Field, values)));
            AssertMeasure<I32>(values, ProtoWriter.State.MeasureRawPackedVarint(Field, values));
        }

        [Theory, MemberData(nameof(Counts))]
        public void UnsignedLongMatchesTheLibrary(int n)
        {
            var values = Build(n, i => (ulong)(i % 200) | ((ulong)(i % 7) << 40));
            Assert.Equal(Reference<U64>(values), Raw((ref ProtoWriter.State s) => s.WriteRawPackedVarint(Field, values)));
            AssertMeasure<U64>(values, ProtoWriter.State.MeasureRawPackedVarint(Field, values));
        }

        [Theory, MemberData(nameof(Counts))]
        public void SignedLongPunsOntoUnsigned(int n)
        {
            var values = Build(n, i => (i & 3) == 3 ? -(long)(i + 1) : i % 200);
            var punned = System.Runtime.InteropServices.MemoryMarshal.Cast<long, ulong>(values);
            Assert.Equal(Reference<I64>(values), Raw((ref ProtoWriter.State s) => s.WriteRawPackedVarint(Field, global::System.Runtime.InteropServices.MemoryMarshal.Cast<long, ulong>(values))));
            AssertMeasure<I64>(values, ProtoWriter.State.MeasureRawPackedVarint(Field, punned));
        }

        [Theory, MemberData(nameof(Counts))]
        public void BoolsMatchTheLibrary(int n)
        {
            var values = Build(n, i => (i % 3) == 0);
            Assert.Equal(Reference<Bools>(values), Raw((ref ProtoWriter.State s) => s.WriteRawPackedBool(Field, values)));
            AssertMeasure<Bools>(values, ProtoWriter.State.MeasureRawPackedBool(Field, values.Length));
        }

        [Theory, MemberData(nameof(Counts))]
        public void FloatsMatchTheLibrary(int n)
        {
            var values = Build(n, i => i * 1.5f);
            var punned = System.Runtime.InteropServices.MemoryMarshal.Cast<float, uint>(values);
            Assert.Equal(Reference<Floats>(values), Raw((ref ProtoWriter.State s) => s.WriteRawPackedFixed32(Field, global::System.Runtime.InteropServices.MemoryMarshal.Cast<float, uint>(values))));
            AssertMeasure<Floats>(values, ProtoWriter.State.MeasureRawPackedFixed32(Field, values.Length));
        }

        [Theory, MemberData(nameof(Counts))]
        public void DoublesMatchTheLibrary(int n)
        {
            var values = Build(n, i => i * 1.25d);
            var punned = System.Runtime.InteropServices.MemoryMarshal.Cast<double, ulong>(values);
            Assert.Equal(Reference<Doubles>(values), Raw((ref ProtoWriter.State s) => s.WriteRawPackedFixed64(Field, global::System.Runtime.InteropServices.MemoryMarshal.Cast<double, ulong>(values))));
            AssertMeasure<Doubles>(values, ProtoWriter.State.MeasureRawPackedFixed64(Field, values.Length));
        }

        /// <summary>
        /// The point of the exercise: an enum column punned to its underlying type is byte-identical
        /// to the primitive one, so it needs no arm of its own anywhere in the library.
        /// </summary>
        public enum Level { None = 0, Low = 1, Mid = 2, High = 3 }
        [ProtoContract] public class Enums { [ProtoMember(3, IsPacked = true)] public Level[] V { get; set; } }

        [Theory, MemberData(nameof(Counts))]
        public void EnumsPunOntoTheirUnderlyingType(int n)
        {
            var values = Build(n, i => (Level)(i & 3));
            var punned = System.Runtime.InteropServices.MemoryMarshal.Cast<Level, int>(values);
            Assert.Equal(Reference<Enums>(values), Raw((ref ProtoWriter.State s) => s.WriteRawPackedVarint(Field, global::System.Runtime.InteropServices.MemoryMarshal.Cast<Level, int>(values))));
            AssertMeasure<Enums>(values, ProtoWriter.State.MeasureRawPackedVarint(Field, punned));
        }

        private static T[] Build<T>(int n, Func<int, T> gen)
        {
            var arr = new T[n];
            for (int i = 0; i < n; i++) arr[i] = gen(i);
            return arr;
        }

        /// <summary>What <c>RepeatedSerializer</c> writes for the same column.</summary>
        private static string Reference<TContract>(object values) where TContract : class, new()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(TContract), true);
            var obj = new TContract();
            typeof(TContract).GetProperty("V")!.SetValue(obj, values);
            using var ms = new MemoryStream();
            model.Serialize(ms, obj);
            return BitConverter.ToString(ms.ToArray());
        }

        /// <summary>
        /// The measure must equal the bytes the library actually writes — not merely the bytes the
        /// raw path writes, which would let a measure and a write agree on the same mistake.
        /// A hex dump renders n bytes as 3n-1 characters, hence the arithmetic.
        /// </summary>
        private static void AssertMeasure<TContract>(object values, long measured) where TContract : class, new()
            => Assert.Equal((long)((Reference<TContract>(values).Length + 1) / 3), measured);

        private delegate void RawWrite(ref ProtoWriter.State state);

        private static string Raw(RawWrite write)
        {
            var sink = new Sink();
            var state = ProtoWriter.State.Create(sink, null);
            try
            {
                write(ref state);
                state.Flush();
            }
            finally { state.Dispose(); }
            return BitConverter.ToString(sink.ToArray());
        }

        private sealed class Sink : IBufferWriter<byte>
        {
            private readonly byte[] _buffer = new byte[1024 * 64];
            private int _written;
            public byte[] ToArray()
            {
                var result = new byte[_written];
                Buffer.BlockCopy(_buffer, 0, result, 0, _written);
                return result;
            }
            public void Advance(int count) => _written += count;
            public Memory<byte> GetMemory(int sizeHint = 0) => new(_buffer, _written, _buffer.Length - _written);
            public Span<byte> GetSpan(int sizeHint = 0) => new(_buffer, _written, _buffer.Length - _written);
        }
    }
}
