using Newtonsoft.Json;
using Pipelines.Sockets.Unofficial.Buffers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test
{
    public class MeasureTests
    {
        [Fact]
        public void TestMeasure_Stream()
        {
            var orig = Invent();
            using var measure = Serializer.Measure(orig);
            Assert.Equal(10330, measure.Length);

            var measureHits = measure.GetLengthHits(out var measureMisses);
            Log?.WriteLine($"After measure: {measureHits} hits, {measureMisses} misses");

            var expectedJson = JsonConvert.SerializeObject(orig);

            using var ms = new MemoryStream();
            measure.Serialize(ms);

            var serializeHits = measure.GetLengthHits(out var serializeMisses);
            Log?.WriteLine($"After serialize: {serializeHits} hits, {serializeMisses} misses");

            Assert.Equal(10330, ms.Length);
            Assert.Equal(measureMisses, serializeMisses);
            Assert.True(measureHits <= serializeHits, "expected no fewer hits");

            ms.Position = 0;
            var clone = Serializer.Deserialize<MeasureMe>(ms);
            Assert.NotSame(orig, clone);
            var actualJson = JsonConvert.SerializeObject(clone);
            Assert.Equal(expectedJson, actualJson);

            Log?.WriteLine(ToHex(ms));
        }

        public MeasureTests(ITestOutputHelper log) => Log = log;
        private ITestOutputHelper Log { get; }

        [Fact]
        public void TestMeasure_Buffer()
        {
            var orig = Invent();
            using var measure = Serializer.Measure(orig);
            Assert.Equal(10330, measure.Length);

            var measureHits = measure.GetLengthHits(out var measureMisses);
            Log?.WriteLine($"After measure: {measureHits} hits, {measureMisses} misses");

            var expectedJson = JsonConvert.SerializeObject(orig);

            using var bw = BufferWriter<byte>.Create(blockSize: 7);
            measure.Serialize(bw);

            var serializeHits = measure.GetLengthHits(out var serializeMisses);
            Log?.WriteLine($"After serialize: {serializeHits} hits, {serializeMisses} misses");

            Assert.Equal(10330, bw.Length);
            Assert.Equal(measureMisses, serializeMisses);
            Assert.True(measureHits < serializeHits, "expected more hits");

            using var payload = bw.Flush();
            var ros = payload.Value;
            Assert.Equal(10330, ros.Length);

            var clone = Serializer.Deserialize<MeasureMe>(ros);
            Assert.NotSame(orig, clone);
            var actualJson = JsonConvert.SerializeObject(clone);
            Assert.Equal(expectedJson, actualJson);

            int segments = CountSegments(ros);
            Log?.WriteLine($"segments: {segments}");
            Log?.WriteLine(ToHex(ros));
        }

        static string ToHex(MemoryStream bytes)
        {
            if (!bytes.TryGetBuffer(out var buffer)) return "n/a";
            return BitConverter.ToString(buffer.Array, buffer.Offset, buffer.Count);
        }

        static int CountSegments(ReadOnlySequence<byte> bytes)
        {
            if (bytes.IsEmpty) return 0;
            if (bytes.IsSingleSegment) return 1;
            int count = 0;
            foreach (var _ in bytes)
                count++;
            return count;
        }

        static string ToHex(ReadOnlySequence<byte> bytes)
        {
            int len = checked((int)bytes.Length);
            var arr = ArrayPool<byte>.Shared.Rent(len);
            try
            {
                bytes.CopyTo(arr);
                return BitConverter.ToString(arr, 0, len);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(arr);
            }
        }

        private MeasureMe Invent()
        {
            var rand = new Random(12345);
            string alphabet = " 0123456789 abcdefghijklmnopqrstuvwxyz ";
            unsafe string InventString()
            {
                const int MAXLEN = 64;
                char* c = stackalloc char[MAXLEN];
                var len = rand.Next(MAXLEN);
                for (int i = 0; i < len; i++)
                {
                    c[i] = alphabet[rand.Next(alphabet.Length)];
                }
                return new string(c, 0, len);
            }
            var orig = new MeasureMe
            {
                Name = InventString(),
            };
            for (int i = 0; i < 250; i++)
            {
                Thing thing = rand.Next(10) > 7 ? new SubThing { SubId = rand.Next(10000) } : new Thing();
                thing.Id = rand.Next(10000);
                thing.Name = InventString();
                orig.Things.Add(thing);
            }
            return orig;
        }

        [ProtoContract]
        public class MeasureMe
        {
            [ProtoMember(1)]
            public string Name { get; set; }

            [ProtoMember(2)]
            public List<Thing> Things { get; } = new List<Thing>();
        }

        [ProtoContract]
        [ProtoInclude(3, typeof(SubThing))]
        public class Thing
        {
            [ProtoMember(1)]
            public int Id { get; set; }
            [ProtoMember(2)]
            public string Name { get; set; }
        }

        [ProtoContract]
        public class SubThing : Thing
        {
            [ProtoMember(1)]
            public int SubId { get; set; }
        }
    }
}
