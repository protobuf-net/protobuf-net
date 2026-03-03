using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;
using System.Buffers;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test.Issues
{
    public class Issue1232
    {
        private readonly TypeModel model;

        private readonly ITestOutputHelper log;

        private static int writeCallCount;

        private static int measureCallCount;

        public Issue1232(ITestOutputHelper log)
        {
            RuntimeTypeModel model = RuntimeTypeModel.Create();
            model.Add<Stream>().SerializerType = typeof(StreamSerializer);
            this.model = model;
            this.log = log;
            writeCallCount = 0;
            measureCallCount = 0;
        }

        [Theory]
        [InlineData(1024, false, false)] // Test around 1024 size since that is the chunkSize used by the serializer internally to try to catch possible off by one errors in length calculation
        [InlineData(1025, false, false)]
        [InlineData(1023, false, false)]
        [InlineData(1024, true, false)]
        [InlineData(1025, true, false)]
        [InlineData(1023, true, false)]
#if NETCOREAPP
        [InlineData(1024, true, true)]
#endif
        public void StreamSerializer_RootStream(int size, bool trySkipWritingWhenMeasuring, bool useBufferWritter)
        {
            StreamSerializer.trySkipWritingWhenMeasuring = trySkipWritingWhenMeasuring;
            Assert.Equal(0, writeCallCount);

            byte[] data = new byte[size];
            new Random(42).NextBytes(data);
            Stream obj = new MemoryStream(data);

            using var measure = this.model.Measure(obj);
            this.log.WriteLine($"Measured length: {measure.Length}");

            var measureHits = measure.GetLengthHits(out var measureMisses);
            this.log.WriteLine($"After measure: {measureHits} hits, {measureMisses} misses");

            Stream clone = null;
            if (useBufferWritter)
            {
#if NETCOREAPP
                ArrayBufferWriter<byte> writer = new ArrayBufferWriter<byte>();
                measure.Serialize(writer);

                Assert.Equal(writer.WrittenCount, measure.Length);
                clone = this.model.Deserialize<Stream>(writer.WrittenMemory.ToArray());
#else
                throw new InvalidOperationException("IBufferWriter is only available in dotnet core");
#endif
            }
            else
            {
                using var ms = new MemoryStream();
                measure.Serialize(ms);

                Assert.Equal(measure.Length, ms.Length);
                ms.Position = 0;
                clone = this.model.Deserialize<Stream>(ms);
            }


            var serializeHits = measure.GetLengthHits(out var serializeMisses);
            this.log.WriteLine($"After serialize: {serializeHits} hits, {serializeMisses} misses");


            Assert.NotNull(clone);
            Assert.Equal(data, GetBuffer(clone));

            if (trySkipWritingWhenMeasuring)
            {
                Assert.Equal(1, writeCallCount);
                Assert.Equal(1, measureCallCount);
            }
            else
            {
                Assert.Equal(2, writeCallCount);
            }
        }

        [Theory]
        [InlineData(1024, false, false)] // Test around 1024 size since that is the chunkSize used by the serializer internally to try to catch possible off by one errors in length calculation
        [InlineData(1025, false, false)]
        [InlineData(1023, false, false)]
        [InlineData(1024, true, false)]
        [InlineData(1025, true, false)]
        [InlineData(1023, true, false)]
#if NETCOREAPP
        [InlineData(1024, true, true)]
#endif
        public void StreamSerializer_NonRootStream(int size, bool trySkipWritingWhenMeasuring, bool useBufferWritter)
        {
            StreamSerializer.trySkipWritingWhenMeasuring = trySkipWritingWhenMeasuring;
            Assert.Equal(0, writeCallCount);

            byte[] data = new byte[size];
            byte[] data2 = new byte[size];
            new Random(42).NextBytes(data);
            new Random(42).NextBytes(data2);
            Stream stream = new MemoryStream(data);
            Stream stream2 = new MemoryStream(data2);

            StreamHolder obj = new StreamHolder()
            {
                Stream = stream,
                TestInt = 8,
                Stream2 = stream2,
                Numbers = [1, 2, 3]
            };

            using var measure = this.model.Measure(obj);
            this.log.WriteLine($"Measured length: {measure.Length}");

            var measureHits = measure.GetLengthHits(out var measureMisses);
            this.log.WriteLine($"After measure: {measureHits} hits, {measureMisses} misses");


            StreamHolder clone = null;
            if (useBufferWritter)
            {
#if NETCOREAPP
                ArrayBufferWriter<byte> writer = new ArrayBufferWriter<byte>();
                measure.Serialize(writer);

                Assert.Equal(writer.WrittenCount, measure.Length);
                clone = this.model.Deserialize<StreamHolder>(writer.WrittenMemory.ToArray());
#else
                throw new InvalidOperationException("IBufferWriter is only available in dotnet core");
#endif
            }
            else
            {
                using var ms = new MemoryStream();
                measure.Serialize(ms);

                Assert.Equal(measure.Length, ms.Length);
                ms.Position = 0;
                clone = this.model.Deserialize<StreamHolder>(ms);
            }

            var serializeHits = measure.GetLengthHits(out var serializeMisses);
            this.log.WriteLine($"After serialize: {serializeHits} hits, {serializeMisses} misses");

            Assert.NotNull(clone);
            Assert.Equal(8, clone.TestInt);
            Assert.Equal(data, GetBuffer(clone.Stream));
            Assert.Equal(new int[] { 1, 2, 3}, clone.Numbers);
            Assert.Equal(data2, GetBuffer(clone.Stream2));

            if (trySkipWritingWhenMeasuring)
            {
                Assert.Equal(2, writeCallCount);
                Assert.Equal(2, measureCallCount);
            }
            else
            {
                Assert.Equal(4, writeCallCount);
            }
        }

        [Theory]
        [InlineData(false)]
#if NETCOREAPP
        [InlineData(true)]
#endif
        public void StreamSerializer_Throws_WhenMeasuredLengthDoesNotMatchActualLength(bool useBufferWritter)
        {
            StreamSerializer.trySkipWritingWhenMeasuring = true;
            StreamSerializer.intentionallyMiscalculateLength = true;
            Assert.Equal(0, writeCallCount);

            byte[] data = new byte[1024];
            new Random(42).NextBytes(data);
            Stream obj = new MemoryStream(data);

            using var measure = this.model.Measure(obj);
            this.log.WriteLine($"Measured length: {measure.Length}");

            var measureHits = measure.GetLengthHits(out var measureMisses);
            this.log.WriteLine($"After measure: {measureHits} hits, {measureMisses} misses");

            Exception ex = null;
            try
            {
                if (useBufferWritter)
                {
#if NETCOREAPP
                    ArrayBufferWriter<byte> writer = new ArrayBufferWriter<byte>();
                    measure.Serialize(writer);
#else
                throw new InvalidOperationException("IBufferWriter is only available in dotnet core");
#endif
                }
                else
                {
                    using var ms = new MemoryStream();
                    measure.Serialize(ms);
                }
            }
            catch (Exception e)
            {
                ex = e;
            }

            var serializeHits = measure.GetLengthHits(out var serializeMisses);
            this.log.WriteLine($"After serialize: {serializeHits} hits, {serializeMisses} misses");
            Assert.NotNull(ex);
            if (useBufferWritter)
            {
                Assert.Contains("Length mismatch", ex.Message);
            }
            else
            {
                Assert.Contains("Invalid length", ex.Message);
            }

        }

        private static byte[] GetBuffer(Stream stream)
        {
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, (int)stream.Length);
            return buffer;
        }

        [ProtoContract]
        public class StreamHolder
        {
            [ProtoMember(1, DataFormat = DataFormat.Group)]
            public Stream Stream { get; set; }

            [ProtoMember(2, IsPacked = true)]
            public int[] Numbers { get; set; }

            [ProtoMember(3)]
            public int TestInt { get; set; }

            /// Different to Stream since it does not use Group data format
            [ProtoMember(4)]
            public Stream Stream2 { get; set; }
        }

        public class StreamSerializer : IMeasuringSerializer<Stream>
        {
            private static readonly int chunkSize = 1024;

            public static bool trySkipWritingWhenMeasuring = false;
            public static bool intentionallyMiscalculateLength = false;

            public SerializerFeatures Features
            {
                get
                {
                    if (trySkipWritingWhenMeasuring)
                    {
                        return SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString | SerializerFeatures.OptionTrySkipWritingWhenMeasuring | SerializerFeatures.CategoryMessageWrappedAtRoot;
                    }

                    return SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString;
                }
            }

            public void Write(ref ProtoWriter.State state, Stream value)
            {
                writeCallCount++;
                if (value is null)
                {
                    return;
                }

                if (!value.CanSeek)
                {
                    throw new NotSupportedException("Stream must support seeking");
                }

                value.Seek(0, SeekOrigin.Begin);
                byte[] buffer = new byte[chunkSize];
                try
                {
                    int bytesRead;
                    while ((bytesRead = value.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        state.WriteFieldHeader(1, WireType.String);
                        state.WriteBytes(new ReadOnlyMemory<byte>(buffer, 0, bytesRead));
                    }
                }
                finally
                {
                    value.Seek(0, SeekOrigin.Begin);
                }
            }

            public Stream Read(ref ProtoReader.State state, Stream value)
            {
                value ??= new MemoryStream();
                var ms = new MemoryStream();
                int field;
                while ((field = state.ReadFieldHeader()) > 0)
                {
                    switch (field)
                    {
                        case 1:
                            var chunk = state.AppendBytes(default(ReadOnlyMemory<byte>));
                            if (chunk.Length > 0)
                            {
                                ms.Write(chunk.ToArray(), 0, chunk.Length);
                            }
                            break;
                        default:
                            state.SkipField();
                            break;
                    }
                }

                ms.Position = 0;
                ms.CopyTo(value);
                value.Position = 0;
                return value;
            }

            public int Measure(ISerializationContext context, WireType wireType, Stream value)
            {
                measureCallCount++;
                if (intentionallyMiscalculateLength)
                {
                    return 985;
                }

                if (value is null)
                {
                    return -1;
                }

                try
                {
                    if (value.Length <= 0)
                    {
                        return -1;
                    }

                    // The serialized length of the message is the length of the stream payload + the sum of the length of all the field headers
                    // Since we write one header for every chunk and each header takes 1 byte (because the field number is less than 15)
                    // we can simplify this to:
                    int totalLength = 0;

                    int lastChunkPayloadLength = (int)value.Length % chunkSize;
                    lastChunkPayloadLength = lastChunkPayloadLength == 0 ? chunkSize : lastChunkPayloadLength;
                    totalLength = 1 + GetVarintLength(lastChunkPayloadLength) + lastChunkPayloadLength;


                    int chunksCount = ((int)value.Length + chunkSize - 1) / chunkSize;
                    if (chunksCount > 1)
                    {
                        totalLength += (chunksCount - 1) + ((chunksCount - 1) * GetVarintLength(chunkSize)) + (chunksCount - 1) * chunkSize;
                    }

                    return totalLength;
                }
                catch (Exception)
                {
                    return -1;
                }
            }

            /// <summary>
            /// Calculates the number of bytes needed to encode a uint as a varint.
            /// </summary>
            /// <param name="value">Unsigned integer to encode.</param>
            /// <returns>Length in bytes of the varint representation.</returns>
            private static int GetVarintLength(int value)
            {
                int length = 0;
                do
                {
                    value >>= 7; // Shift right by 7 bits
                    length++;    // Increment the byte length
                } while (value != 0);
                return length;
            }
        }
    }
}
