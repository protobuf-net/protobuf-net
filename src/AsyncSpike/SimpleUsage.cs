#if DEBUG
#define VERBOSE
#endif

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.IO.Pipelines.Text.Primitives;
using System.Text;
using System.Text.Utf8;
using System.Threading.Tasks;
using Xunit;

public class SimpleUsage : IDisposable
{
    private PipeFactory _factory = new PipeFactory();
    void IDisposable.Dispose() => _factory.Dispose();

    static void Main()
    {
        try
        {
            Console.WriteLine("Running...");
            using (var rig = new SimpleUsage())
            {
                rig.RunGoogleTests().Wait();
            }
        }
        catch (Exception ex)
        {
            while (ex != null)
            {
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
                Console.Error.WriteLine();
                ex = ex.InnerException;
            }

        }
    }
    // see example in: https://developers.google.com/protocol-buffers/docs/encoding
    public async Task RunGoogleTests()
    {
        await Console.Out.WriteLineAsync(nameof(ReadTest1));
        await ReadTest1();

        await Console.Out.WriteLineAsync();
        await Console.Out.WriteLineAsync(nameof(ReadTest2));
        await ReadTest2();

        await Console.Out.WriteLineAsync();
        await Console.Out.WriteLineAsync(nameof(ReadTest3));
        await ReadTest3();

        await Console.Out.WriteLineAsync();
        await Console.Out.WriteLineAsync(nameof(WriteTest1));
        await WriteTest1();

        await Console.Out.WriteLineAsync();
        await Console.Out.WriteLineAsync(nameof(WriteTest2));
        await WriteTest2();

        await Console.Out.WriteLineAsync();
        await Console.Out.WriteLineAsync(nameof(WriteTest3));
        await WriteTest3();

    }

    [Xunit.Fact]
    public Task ReadTest1() => ReadTest<Test1>("08 96 01", DeserializeTest1Async, "A: 150");

    [Xunit.Fact]
    public Task WriteTest1() => WriteTest(new Test1 { A = 150 }, "08 96 01", SerializeTest1Async);

    [Xunit.Fact]
    public Task ReadTest2() => ReadTest<Test2>("12 07 74 65 73 74 69 6e 67", DeserializeTest2Async, "B: testing");

    [Xunit.Fact]
    public Task WriteTest2() => WriteTest(new Test2 { B = "testing" }, "12 07 74 65 73 74 69 6e 67", SerializeTest2Async);


    // note I've suffixed with another dummy "1" field to test the end sub-object code
    [Xunit.Fact]
    public Task ReadTest3() => ReadTest<Test3>("1a 03 08 96 01 08 96 01", DeserializeTest3Async, "C: [A: 150]");

    [Xunit.Fact]
    public Task WriteTest3() => WriteTest(new Test3 { C = new Test1 { A = 150 } }, "1a 03 08 96 01", SerializeTest3Async);

    class Test1
    {
        public int A;
        public override string ToString() => $"A: {A}";
    }

    class Test2
    {
        public string B { get; set; }
        public override string ToString() => $"B: {B}";
    }

    class Test3
    {
        public Test1 C { get; set; }
        public override string ToString() => $"C: [{C}]";
    }


    [Conditional("VERBOSE")]
    static void Trace(string message)
    {
#if VERBOSE
        Console.WriteLine(message);
#endif
    }

    // note: this code would be spat out my the roslyn generator API
    async ValueTask<Test1> DeserializeTest1Async(
        AsyncProtoReader reader, Test1 value = default(Test1))
    {
        Trace("Reading fields...");
        while (await reader.ReadNextFieldAsync())
        {
            Trace($"Reading field {reader.FieldNumber}...");
            switch (reader.FieldNumber)
            {
                case 1:
                    (value ?? Create(ref value)).A = await reader.ReadInt32Async();
                    break;
                default:
                    await reader.SkipFieldAsync();
                    break;
            }
            Trace($"Reading next field...");
        }
        return value ?? Create(ref value);
    }

    async ValueTask<long> SerializeTest1Async(AsyncProtoWriter writer, Test1 value)
    {
        long bytes = 0;
        if (value != null)
        {
            Trace($"Writing {nameof(Test1)} fields...");
            bytes += await writer.WriteVarintInt32Async(1, value.A);
        }
        return bytes;
    }
    async ValueTask<long> SerializeTest2Async(AsyncProtoWriter writer, Test2 value)
    {
        long bytes = 0;
        if (value != null)
        {
            Trace($"Writing {nameof(Test2)} fields...");
            bytes += await writer.WriteStringAsync(2, value.B);
        }
        return bytes;
    }

    async ValueTask<long> SerializeTest3Async(AsyncProtoWriter writer, Test3 value)
    {
        long bytes = 0;
        if (value != null)
        {
            Trace($"Writing {nameof(Test3)} fields...");
            bytes += await writer.WriteSubObject(3, value.C, (α, β) => SerializeTest1Async(α, β));
        }
        return bytes;
    }

    private async Task ReadTest<T>(string hex, Func<AsyncProtoReader, T, ValueTask<T>> deserializer, string expected)
    {
        await ReadTestPipe<T>(hex, deserializer, expected);
        await ReadTestSpan<T>(hex, deserializer, expected);
    }
    private async Task ReadTestPipe<T>(string hex, Func<AsyncProtoReader, T, ValueTask<T>> deserializer, string expected)
    {
        var pipe = _factory.Create();
        await AppendPayloadAsync(pipe, hex);
        pipe.Writer.Complete(); // simulate EOF

        Trace("deserializing via PipeReader...");
        using (var reader = AsyncProtoReader.Create(pipe.Reader))
        {
            var obj = await deserializer(reader, default(T));
            string actual = obj?.ToString();
            Trace(actual);
            Assert.Equal(expected, actual);
        }
    }
    private async Task ReadTestSpan<T>(string hex, Func<AsyncProtoReader, T, ValueTask<T>> deserializer, string expected)
    {
        var blob = ParseBlob(hex);
        Trace("deserializing via SpanReader...");
        using (var reader = AsyncProtoReader.Create(blob))
        {
            var obj = await deserializer(reader, default(T));
            string actual = obj?.ToString();
            Trace(actual);
            Assert.Equal(expected, actual);
        }
    }

    private async Task WriteTest<T>(T value, string expected, Func<AsyncProtoWriter, T, ValueTask<long>> serializer)
    {
        long len = await WriteTestPipe(value, expected, serializer);
        await WriteTestSpan(len, value, expected, serializer);
    }
    private async ValueTask<long> WriteTestPipe<T>(T value, string expected, Func<AsyncProtoWriter, T, ValueTask<long>> serializer)
    {
        long bytes;
        string actual;
        if (value == null)
        {
            bytes = 0;
            actual = "";
        }
        else
        {
            var pipe = _factory.Create();
            using (var writer = AsyncProtoWriter.Create(pipe.Writer))
            {
                bytes = await serializer(writer, value);
                Trace($"Serialized to pipe in {bytes} bytes");
                await writer.FlushAsync(true);
            }
            var buffer = await pipe.Reader.ReadToEndAsync();
            actual = NormalizeHex(BitConverter.ToString(buffer.ToArray()));
        }
        expected = NormalizeHex(expected);
        Assert.Equal(expected, actual);
        return bytes;
    }
    private async Task WriteTestSpan<T>(long bytes, T value, string expected, Func<AsyncProtoWriter, T, ValueTask<long>> serializer)
    {
        long nullBytes = await serializer(AsyncProtoWriter.Null, value);
        Trace($"Serialized to nil-writer in {nullBytes} bytes");
        Assert.Equal(bytes, nullBytes);

        string actual;
        if (value == null)
        {
            actual = "";
        }
        else
        {
            var blob = new byte[bytes];
            Trace($"Allocated span of length {blob.Length}");
            using (var writer = AsyncProtoWriter.Create(blob))
            {
                long newBytes = await serializer(writer, value);
                Trace($"Serialized to span in {newBytes} bytes");
                Assert.Equal(bytes, newBytes);
                await writer.FlushAsync(true);
            }
            actual = NormalizeHex(BitConverter.ToString(blob));
        }
        expected = NormalizeHex(expected);
        Assert.Equal(expected, actual);
    }

    // note: this code would be spat out my the roslyn generator API
    async ValueTask<Test2> DeserializeTest2Async(
        AsyncProtoReader reader, Test2 value = default(Test2))
    {
        Trace("Reading fields...");
        while (await reader.ReadNextFieldAsync())
        {
            Trace($"Reading field {reader.FieldNumber}...");
            switch (reader.FieldNumber)
            {
                case 2:
                    (value ?? Create(ref value)).B = await reader.ReadStringAsync();
                    break;
                default:
                    await reader.SkipFieldAsync();
                    break;
            }
            Trace($"Reading next field...");
        }
        return value ?? Create(ref value);
    }

    async ValueTask<Test3> DeserializeTest3Async(
        AsyncProtoReader reader, Test3 value = default(Test3))
    {
        Trace("Reading fields...");
        while (await reader.ReadNextFieldAsync())
        {
            Trace($"Reading field {reader.FieldNumber}...");
            switch (reader.FieldNumber)
            {
                case 3:
                    var token = await reader.BeginSubObjectAsync();
                    (value ?? Create(ref value)).C = await DeserializeTest1Async(reader, value?.C);
                    reader.EndSubObject(ref token);
                    break;
                default:
                    await reader.SkipFieldAsync();
                    break;
            }
            Trace($"Reading next field...");
        }
        return value ?? Create(ref value);
    }

    static T Create<T>(ref T obj) where T : class, new() => obj ?? (obj = new T());

    public struct SubObjectToken
    {
        internal SubObjectToken(long oldEnd, long end)
        {
            OldEnd = oldEnd;
            End = end;
        }
        internal readonly long OldEnd, End;
    }

    /// <summary>
    /// Indicates the encoding used to represent an individual value in a protobuf stream
    /// </summary>
    public enum WireType
    {
        /// <summary>
        /// Represents an error condition
        /// </summary>
        None = -1,

        /// <summary>
        /// Base-128 variant-length encoding
        /// </summary>
        Varint = 0,

        /// <summary>
        /// Fixed-length 8-byte encoding
        /// </summary>
        Fixed64 = 1,

        /// <summary>
        /// Length-variant-prefixed encoding
        /// </summary>
        String = 2,

        /// <summary>
        /// Indicates the start of a group
        /// </summary>
        StartGroup = 3,

        /// <summary>
        /// Indicates the end of a group
        /// </summary>
        EndGroup = 4,

        /// <summary>
        /// Fixed-length 4-byte encoding
        /// </summary>10
        Fixed32 = 5,
    }
    public abstract class AsyncProtoReader : IDisposable
    {
        protected static readonly TextEncoder Encoder = TextEncoder.Utf8;
        protected abstract void ApplyDataConstraint();
        protected abstract void RemoveDataConstraint();
        public virtual void Dispose() { }

        public static AsyncProtoReader Create(ReadOnlySpan<byte> span) => new SpanReader(span);
        public static AsyncProtoReader Create(IPipeReader pipe, bool closePipe = true, long bytes = long.MaxValue) => new PipeReader(pipe, closePipe, bytes);

        public virtual async ValueTask<bool> SkipFieldAsync()
        {
            switch (WireType)
            {
                case WireType.Varint:
                    await ReadInt32Async(); // drop the result on te floor
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }

        int _fieldHeader;
        public int FieldNumber => _fieldHeader >> 3;

        protected AsyncProtoReader(long length = long.MaxValue) { _end = length; }
        public WireType WireType => (WireType)(_fieldHeader & 7);
        public async ValueTask<bool> ReadNextFieldAsync()
        {
            var next = await TryReadVarintInt32Async();
            if (next == null)
            {
                return false;
            }
            else
            {
                _fieldHeader = next.GetValueOrDefault();
                return true;
            }
        }
        protected void Advance(int count) => _position += count;
        public long Position => _position;
        long _position, _end;
        protected long End => _end;
        public async ValueTask<SubObjectToken> BeginSubObjectAsync()
        {
            switch (WireType)
            {
                case WireType.String:
                    int len = await ReadInt32Async();
                    var result = new SubObjectToken(_end, _end = _position + len);
                    ApplyDataConstraint();
                    return result;
                default:
                    throw new InvalidOperationException();
            }
        }
        public void EndSubObject(ref SubObjectToken token)
        {
            if (token.End != _end) throw new InvalidOperationException("Sub-object ended in wrong order");
            if (token.End != _position) throw new InvalidOperationException("Sub-object not fully consumed");
            RemoveDataConstraint();
            _end = token.OldEnd;
            if (_end != long.MaxValue)
            {
                ApplyDataConstraint();
            }
            token = default(SubObjectToken);
        }

        public async ValueTask<int> ReadInt32Async()
        {
            var val = await TryReadVarintInt32Async();
            if (val == null) throw new EndOfStreamException();
            return val.GetValueOrDefault();
        }
        protected abstract ValueTask<int?> TryReadVarintInt32Async();

        public async ValueTask<string> ReadStringAsync()
        {
            var lenOrNull = await TryReadVarintInt32Async();
            if (lenOrNull == null)
            {
                throw new EndOfStreamException();
            }
            int len = lenOrNull.GetValueOrDefault();
            Trace($"String length: {len}");
            return len == 0 ? "" : await ReadStringAsync(len);
        }
        protected abstract ValueTask<string> ReadStringAsync(int bytes);
    }
    public abstract class AsyncProtoWriter : IDisposable
    {
        public virtual ValueTask<bool> FlushAsync(bool final) => new ValueTask<bool>(false);
        public virtual void Dispose() { }
        public async ValueTask<int> WriteVarintInt32Async(int fieldNumber, int value) =>
            await WriteFieldHeader(fieldNumber, WireType.Varint) + await WriteVarintUInt64Async((ulong)(long)value);

        private ValueTask<int> WriteFieldHeader(int fieldNumber, WireType wireType) => WriteVarintUInt32Async((uint)((fieldNumber << 3) | (int)wireType));

        public async ValueTask<int> WriteStringAsync(int fieldNumber, Utf8String value)
        {
            return await WriteFieldHeader(fieldNumber, WireType.String)
                + await WriteVarintUInt32Async((uint)value.Length)
                + await WriteBytes(value.Bytes);
        }
        public async ValueTask<int> WriteStringAsync(int fieldNumber, string value)
        {
            if (value == null) return 0;
            return await WriteFieldHeader(fieldNumber, WireType.String)
                + (value.Length == 0 ? await WriteVarintUInt32Async(0) : await WriteStringWithLengthPrefix(value));
        }
        protected static readonly Encoding Encoding = Encoding.UTF8;
        protected static TextEncoder Encoder = TextEncoder.Utf8;

        public async ValueTask<int> WriteBytesAsync(int fieldNumber, ReadOnlySpan<byte> value)
        {
            int bytes = await WriteFieldHeader(fieldNumber, WireType.String) + await WriteVarintUInt32Async((uint)value.Length);
            if (value.Length != 0) bytes += await WriteBytes(value);
            return bytes;
        }

        protected async virtual ValueTask<int> WriteStringWithLengthPrefix(string value)
        {
            byte[] bytes = Encoding.GetBytes(value); // cheap and nasty, but it works
            return await WriteVarintUInt32Async((uint)bytes.Length) + await WriteBytes(bytes);
        }
        protected abstract ValueTask<int> WriteBytes(ReadOnlySpan<byte> bytes);

        protected virtual ValueTask<int> WriteVarintUInt32Async(uint value) => WriteVarintUInt64Async(value);
        protected abstract ValueTask<int> WriteVarintUInt64Async(ulong value);

        internal virtual async ValueTask<long> WriteSubObject<T>(int fieldNumber, T value, Func<AsyncProtoWriter, T, ValueTask<long>> serializer)
        {
            if (value == null) return 0;
            long payloadLength = await serializer(Null, value);
            long prefixLength = await WriteFieldHeader(fieldNumber, WireType.String)
                + await WriteVarintUInt64Async((ulong)payloadLength);
            var bytesWritten = await serializer(this, value);
            Debug.Assert(bytesWritten == payloadLength, "Payload length mismatch in WriteSubObject");

            return prefixLength + payloadLength;
        }

        public static AsyncProtoWriter Create(IPipeWriter writer, bool closePipe = true) => new PipeWriter(writer, closePipe);

        public static AsyncProtoWriter Create(Span<byte> span) => new SpanWriter(span);

        /// <summary>
        /// Provides an AsyncProtoWriter that computes lengths without requiring backing storage
        /// </summary>
        public static readonly AsyncProtoWriter Null = new NullWriter();
        sealed class NullWriter : AsyncProtoWriter
        {
            protected override ValueTask<int> WriteBytes(ReadOnlySpan<byte> bytes) => new ValueTask<int>(bytes.Length);

            protected override ValueTask<int> WriteStringWithLengthPrefix(string value)
            {
                int bytes = Encoding.GetByteCount(value);
                return new ValueTask<int>(GetVarintLength((uint)bytes) + bytes);
            }
            static int GetVarintLength(uint value)
            {
                int count = 0;
                do
                {
                    count++;
                    value >>= 7;
                }
                while (value != 0);
                return count;
            }
            static int GetVarintLength(ulong value)
            {
                int count = 0;
                do
                {
                    count++;
                    value >>= 7;
                }
                while (value != 0);
                return count;
            }
            protected override ValueTask<int> WriteVarintUInt32Async(uint value)
                => new ValueTask<int>(GetVarintLength(value));

            protected override ValueTask<int> WriteVarintUInt64Async(ulong value)
                => new ValueTask<int>(GetVarintLength(value));

            internal async override ValueTask<long> WriteSubObject<T>(int fieldNumber, T value, Func<AsyncProtoWriter, T, ValueTask<long>> serializer)
            {
                if (value == null) return 0;
                long len = await serializer(this, value);
                return GetVarintLength((uint)(fieldNumber << 3)) + GetVarintLength((ulong)len) + len;
            }
        }
    }

    internal sealed class PipeWriter : AsyncProtoWriter
    {
        private IPipeWriter _writer;
        private WritableBuffer _output;
        private readonly bool _closePipe;
        private volatile bool _isFlushing;
        internal PipeWriter(IPipeWriter writer, bool closePipe = true)
        {
            _writer = writer;
            _closePipe = closePipe;
            _output = writer.Alloc();
        }

        public override async ValueTask<bool> FlushAsync(bool final)
        {
            if (_isFlushing) throw new InvalidOperationException("already flushing");
            _isFlushing = true;
            Trace("Flushing...");
            var tmp = _output;
            _output = default(WritableBuffer);
            await tmp.FlushAsync();
            Trace("Flushed");
            _isFlushing = false;

            if (!final)
            {
                _output = _writer.Alloc();
            }
            return true;
        }

        protected override ValueTask<int> WriteStringWithLengthPrefix(string value)
        {
            // can write up to 127 characters (if ASCII) in a single-byte prefix - and conveniently
            // 127 is handy for binary testing
            int bytes = ((value.Length & ~127) == 0) ? TryWriteShortStringWithLengthPrefix(value) : 0;
            if (bytes == 0)
            {
                bytes = WriteLongStringWithLengthPrefix(value);
            }
            return new ValueTask<int>(bytes);
        }
        int TryWriteShortStringWithLengthPrefix(string value)
        {
            // to encode without checking bytes, need 4 times length, plus 1 - the sneaky way
            _output.Ensure(Math.Min(128, value.Length << 2) | 1);
            var span = _output.Buffer.Span;
            int bytesWritten;
            if (Encoder.TryEncode(value, span.Slice(1, Math.Min(127, span.Length - 1)), out bytesWritten))
            {
                Debug.Assert(bytesWritten <= 127, "Too many bytes written in TryWriteShortStringWithLengthPrefix");
                span[0] = (byte)bytesWritten++; // note the post-increment here to account for the prefix byte
                _output.Advance(bytesWritten);
                Trace($"Wrote '{value}' in {bytesWritten} bytes (including length prefix) without checking length first");
                return bytesWritten;
            }
            return 0; // failure
        }
        int WriteLongStringWithLengthPrefix(string value)
        {
            int payloadBytes = Encoding.GetByteCount(value);
            _output.Ensure(5);
            int headerBytes = WriteVarintUInt32(_output.Buffer.Span, (uint)payloadBytes), bytesWritten;
            _output.Advance(headerBytes);

            if (payloadBytes <= _output.Buffer.Length)
            {
                // already enough space in the output buffer - just write it
                bool success = Encoder.TryEncode(value, _output.Buffer.Span, out bytesWritten);
                Debug.Assert(success, "TryEncode failed in WriteLongStringWithLengthPrefix");
                Trace($"Wrote '{value}' in {bytesWritten} bytes into available buffer space");
            }
            else
            {
                bytesWritten = WriteLongString(value, ref _output);
            }
            Debug.Assert(bytesWritten == payloadBytes, "Payload length mismatch in WriteLongStringWithLengthPrefix");
            _output.Advance(payloadBytes);

            return headerBytes + payloadBytes;
        }
        static unsafe int WriteLongString(string value, ref WritableBuffer output)
        {
            fixed (char* c = value)
            {
                var utf16 = new ReadOnlySpan<char>(c, value.Length);
                int totalBytesWritten = 0;
                do
                {
                    output.Ensure(Math.Min(utf16.Length << 2, 128)); // ask for a humble amount, but prepare to be amazed

                    int bytesWritten, charsConsumed;
                    // note: not expecting success here (except for the last one)
                    Encoder.TryEncode(utf16, output.Buffer.Span, out charsConsumed, out bytesWritten);
                    utf16 = utf16.Slice(charsConsumed);
                    output.Advance(bytesWritten);

                    Trace($"Wrote {charsConsumed} chars of long string in {bytesWritten} bytes");
                    totalBytesWritten += bytesWritten;
                } while (utf16.Length != 0);
                return totalBytesWritten;
            }
        }

        protected override ValueTask<int> WriteBytes(ReadOnlySpan<byte> bytes)
        {
            _output.Write(bytes);
            return new ValueTask<int>(bytes.Length);
        }

        protected override ValueTask<int> WriteVarintUInt32Async(uint value)
        {
            _output.Ensure(5);
            int len = WriteVarintUInt32(_output.Buffer.Span, value);
            _output.Advance(len);
            Trace($"Wrote {value} in {len} bytes");
            return new ValueTask<int>(len);
        }
        protected override ValueTask<int> WriteVarintUInt64Async(ulong value)
        {
            _output.Ensure(10);
            int len = WriteVarintUInt64(_output.Buffer.Span, value);
            _output.Advance(len);
            Trace($"Wrote {value} in {len} bytes");
            return new ValueTask<int>(len);
        }
        internal static int WriteVarintUInt32(Span<byte> span, uint value)
        {
            const uint SEVENBITS = 0x7F, CONTINUE = 0x80;

            // least significant group first
            int offset = 0;
            while ((value & ~SEVENBITS) != 0)
            {
                span[offset++] = (byte)((value & SEVENBITS) | CONTINUE);
                value >>= 7;
            }
            span[offset++] = (byte)value;
            return offset;
        }
        internal static int WriteVarintUInt64(Span<byte> span, ulong value)
        {
            const ulong SEVENBITS = 0x7F, CONTINUE = 0x80;

            // least significant group first
            int offset = 0;
            while((value & ~SEVENBITS) != 0)
            {
                span[offset++] = (byte)((value & SEVENBITS) | CONTINUE);
                value >>= 7;
            }
            span[offset++] = (byte)value;
            return offset;
        }
        public override void Dispose()
        {
            var writer = _writer;
            var output = _output;
            _writer = null;
            _output = default(WritableBuffer);
            if (writer != null)
            {
                if (_isFlushing)
                {
                    writer.CancelPendingFlush();
                }
                try { output.Commit(); } catch { /* swallow */ }
                if (_closePipe)
                {
                    writer.Complete();
                }
            }
        }
    }
    internal sealed class SpanWriter : AsyncProtoWriter
    {
        private Span<byte> _span;
        internal SpanWriter(Span<byte> span)
        {
            _span = span;
        }

        protected override ValueTask<int> WriteBytes(ReadOnlySpan<byte> bytes)
        {
            bytes.CopyTo(_span);
            Trace($"Wrote {bytes} raw bytes ({_span.Length - bytes.Length} remain)");
            _span = _span.Slice(bytes.Length);
            return new ValueTask<int>(bytes.Length);
        }

        protected override ValueTask<int> WriteVarintUInt64Async(ulong value)
        {
            int bytes = PipeWriter.WriteVarintUInt64(_span, value);
            Trace($"Wrote {value} as varint in {bytes} bytes ({_span.Length - bytes} remain)");
            _span = _span.Slice(bytes);
            return new ValueTask<int>(bytes);
        }
        protected override ValueTask<int> WriteVarintUInt32Async(uint value)
        {
            int bytes = PipeWriter.WriteVarintUInt32(_span, value);
            Trace($"Wrote {value} as varint in {bytes} bytes ({_span.Length - bytes} remain)");
            _span = _span.Slice(bytes);
            return new ValueTask<int>(bytes);
        }
       
        protected override ValueTask<int> WriteStringWithLengthPrefix(string value)
        {
            // can write up to 127 characters (if ASCII) in a single-byte prefix - and conveniently
            // 127 is handy for binary testing
            int bytes = ((value.Length & ~127) == 0) ? TryWriteShortStringWithLengthPrefix(value) : 0;
            if (bytes == 0)
            {
                bytes = WriteLongStringWithLengthPrefix(value);
            }
            return new ValueTask<int>(bytes);
        }
        int TryWriteShortStringWithLengthPrefix(string value)
        {
            // to encode without checking bytes, need 4 times length, plus 1 - the sneaky way
            int bytesWritten;
            if (Encoder.TryEncode(value, _span.Slice(1, Math.Min(127, _span.Length - 1)), out bytesWritten))
            {
                Debug.Assert(bytesWritten <= 127, "Too many bytes written in TryWriteShortStringWithLengthPrefix");
                _span[0] = (byte)bytesWritten++; // note the post-increment here to account for the prefix byte
                Trace($"Wrote '{value}' in {bytesWritten} bytes (including length prefix) without checking length first ({_span.Length - bytesWritten} remain)");
                _span = _span.Slice(bytesWritten);
                return bytesWritten;
            }
            return 0; // failure
        }
        int WriteLongStringWithLengthPrefix(string value)
        {
            int payloadBytes = Encoding.GetByteCount(value);
            int headerBytes = PipeWriter.WriteVarintUInt32(_span, (uint)payloadBytes), bytesWritten;
            Trace($"Wrote '{value}' header in {headerBytes} bytes into available buffer space ({_span.Length - headerBytes} remain)");
            _span = _span.Slice(headerBytes);

            // we should already have enough space in the output buffer - just write it
            bool success = Encoder.TryEncode(value, _span, out bytesWritten);
            if(!success) throw new InvalidOperationException("Span range would be exceeded");
            Trace($"Wrote '{value}' payload in {payloadBytes} bytes into available buffer space ({_span.Length - payloadBytes} remain)");
            Debug.Assert(bytesWritten == payloadBytes, "Payload length mismatch in WriteLongStringWithLengthPrefix");
            
            _span = _span.Slice(payloadBytes);

            return headerBytes + payloadBytes;
        }
    }
    internal sealed class SpanReader : AsyncProtoReader
    {
        private ReadOnlySpan<byte> _active, _original;
        internal SpanReader(ReadOnlySpan<byte> span) : base(span.Length)
        {
            _active = _original = span;
        }
        protected override ValueTask<string> ReadStringAsync(int bytes)
        {
            bool result = Encoder.TryDecode(_active.Slice(0, bytes), out string text, out int consumed);
            Debug.Assert(result, "TryDecode failed");
            Debug.Assert(consumed == bytes, "TryDecode used wrong count");
            _active = _active.Slice(bytes);
            Advance(bytes);
            return new ValueTask<string>(text);
        }
        protected override ValueTask<int?> TryReadVarintInt32Async()
        {
            var result = PipeReader.TryPeekVarintSingleSpan(_active);
            if (result.consumed == 0)
            {
                return new ValueTask<int?>((int?)null);
            }
            _active = _active.Slice(result.consumed);
            Advance(result.consumed);
            return new ValueTask<int?>(result.value);
        }
        protected override void ApplyDataConstraint()
        {
            if (End != long.MaxValue)
            {
                _active = _original.Slice((int)Position, (int)(End - Position));
            }
        }
        protected override void RemoveDataConstraint()
        {
            _active = _original.Slice((int)Position);
        }
    }
    internal sealed class PipeReader : AsyncProtoReader
    {
        private IPipeReader _reader;
        private readonly bool _closePipe;
        private volatile bool _isReading;
        ReadableBuffer _available, _originalAsReceived;
        internal PipeReader(IPipeReader reader, bool closePipe, long bytes) : base(bytes)
        {
            _reader = reader;
            _closePipe = closePipe;
        }
        protected override async ValueTask<string> ReadStringAsync(int bytes)
        {
            while (_available.Length < bytes)
            {
                if (!await RequestMoreDataAsync()) throw new EndOfStreamException();
            }
            var s = _available.Slice(0, bytes).GetUtf8String();
            Trace($"Read string: {s}");
            _available = _available.Slice(bytes);
            Advance(bytes);
            return s;
        }
        private static (int value, int consumed) TryPeekVarintInt32(ref ReadableBuffer buffer)
        {
            Trace($"Parsing varint from {buffer.Length} bytes...");
            return (buffer.IsSingleSpan || buffer.First.Length >= MaxBytesForVarint)
                ? TryPeekVarintSingleSpan(buffer.First.Span)
                : TryPeekVarintMultiSpan(ref buffer);
        }
        internal static unsafe (int value, int consumed) TryPeekVarintSingleSpan(ReadOnlySpan<byte> span)
        {
            int len = span.Length;
            if (len == 0) return (0, 0);
            // thought: optimize the "I have tons of data" case? (remove the length checks)
            fixed (byte* spanPtr = &span.DangerousGetPinnableReference())
            {
                var ptr = spanPtr;

                // least significant group first
                int val = *ptr & 127;
                if ((*ptr & 128) == 0) return (val, 1);
                if (len == 1) return (0, 0);

                val |= (*++ptr & 127) << 7;
                if ((*ptr & 128) == 0) return (val, 2);
                if (len == 2) return (0, 0);

                val |= (*++ptr & 127) << 14;
                if ((*ptr & 128) == 0) return (val, 3);
                if (len == 3) return (0, 0);

                val |= (*++ptr & 127) << 21;
                if ((*ptr & 128) == 0) return (val, 4);
                if (len == 4) return (0, 0);

                val |= (*++ptr & 127) << 28;
                if ((*ptr & 128) == 0) return (val, 5);
                if (len == 5) return (0, 0);

                // TODO switch to long and check up to 10 bytes (for -1)
                throw new NotImplementedException("need moar pointer math");
            }
        }
        private static unsafe (int value, int consumed) TryPeekVarintMultiSpan(ref ReadableBuffer buffer)
        {
            int value = 0;
            int consumed = 0, shift = 0;
            foreach (var segment in buffer)
            {
                var span = segment.Span;
                if (span.Length != 0)
                {
                    fixed (byte* ptr = &span.DangerousGetPinnableReference())
                    {
                        byte* head = ptr;
                        while (consumed++ < MaxBytesForVarint)
                        {
                            int val = *head++;
                            value |= (val & 127) << shift;
                            shift += 7;
                            if ((val & 128) == 0)
                            {
                                return (value, consumed);
                            }
                        }
                    }
                }
            }
            return (0, 0);
        }

        const int MaxBytesForVarint = 10;


        private async ValueTask<bool> RequestMoreDataAsync()
        {
            if (Position >= End)
            {
                Trace("Refusing more data to sub-object");
                return false;
            }

            // TODO: add length limit here by slicing the returned data
            int oldLen = _available.Length;
            ReadResult read;
            do
            {
                _reader.Advance(_available.Start, _available.End);
                _available = default(ReadableBuffer);

                _isReading = true;
                read = await _reader.ReadAsync();
                _isReading = false;

                if (read.IsCancelled)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }
            }
            while (read.Buffer.Length <= oldLen && !read.IsCompleted);
            _originalAsReceived = _available = read.Buffer;

            if (End != long.MaxValue)
            {
                ApplyDataConstraint();
            }

            return _available.Length > oldLen;
        }
        protected override void RemoveDataConstraint()
        {
            if(_available.End != _originalAsReceived.End)
            {
                int wasForConsoleMessage = _available.Length;
                // change back to the original right hand boundary
                _available = _originalAsReceived.Slice(_available.Start);
                Trace($"Data constraint removed; {_available.Length} bytes available (was {wasForConsoleMessage})");
            }
        }
        protected override void ApplyDataConstraint()
        {
            if (End != long.MaxValue && checked(Position + _available.Length) > End)
            {
                int allow = checked((int)(End - Position));
                int wasForConsoleMessage = _available.Length;
                _available = _available.Slice(0, allow);
                Trace($"Data constraint imposed; {_available.Length} bytes available (was {wasForConsoleMessage})");
            }
        }
        protected override async ValueTask<int?> TryReadVarintInt32Async()
        {
            do
            {
                var read = TryPeekVarintInt32(ref _available);
                if (read.consumed != 0)
                {
                    Advance(read.consumed);
                    _available = _available.Slice(read.consumed);
                    return read.value;
                }
            }
            while (await RequestMoreDataAsync());

            if (_available.Length == 0) return null;
            throw new EndOfStreamException();
        }

        public override void Dispose()
        {
            var reader = _reader;
            var available = _available;
            _reader = null;
            _available = default(ReadableBuffer);
            if (reader != null)
            {
                if (_isReading)
                {
                    reader.CancelPendingRead();
                }
                else
                {
                    reader.Advance(available.Start);
                }
                
                if (_closePipe)
                {
                    reader.Complete();
                }
            }
        }
    }
    static string NormalizeHex(string hex) => hex.Replace('-', ' ').Replace(" ", "").Trim().ToUpperInvariant();

    private static byte[] ParseBlob(string hex)
    {
        hex = NormalizeHex(hex);
        var len = hex.Length / 2;
        byte[] blob = new byte[len];
        for (int i = 0; i < blob.Length; i++)
        {
            blob[i] = Convert.ToByte(hex.Substring(2 * i, 2), 16);
        }
        return blob;
    }
    private static Task AppendPayloadAsync(IPipe pipe, string hex)
    {
        var blob = ParseBlob(hex);
        return pipe.Writer.WriteAsync(blob);
    }
}