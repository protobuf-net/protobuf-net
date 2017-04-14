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
            Console.Error.WriteLine(ex.Message);
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

        await Console.Out.WriteLineAsync(nameof(WriteTest1));
        await WriteTest1();

        await Console.Out.WriteLineAsync();
        await Console.Out.WriteLineAsync(nameof(WriteTest2));
        await WriteTest2();

    }

    [Xunit.Fact]
    public Task ReadTest1() => ReadTest<Test1>("08 96 01", DeserializeTest1Async, "A: 150");

    [Xunit.Fact]
    public Task WriteTest1() => WriteTest(new Test1 { A = 150 }, "08 96 01", SerializeTest1Async);

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
            Trace("Writing fields...");
            bytes += await writer.WriteVarintInt32Async(1, value.A);
        }
        return bytes;
    }
    async ValueTask<long> SerializeTest2Async(AsyncProtoWriter writer, Test2 value)
    {
        long bytes = 0;
        if (value != null)
        {
            Trace("Writing fields...");
            bytes += await writer.WriteStringAsync(2, value.B);
        }
        return bytes;
    }

    class Test1
    {
        public int A;
        public override string ToString() => $"A: {A}";
    }

    [Xunit.Fact]
    public Task ReadTest2() => ReadTest<Test2>("12 07 74 65 73 74 69 6e 67", DeserializeTest2Async, "B: testing");

    [Xunit.Fact]
    public Task WriteTest2() => WriteTest(new Test2 { B = "testing"}, "12 07 74 65 73 74 69 6e 67", SerializeTest2Async);


    // note I've suffixed with another dummy "1" field to test the end sub-object code
    [Xunit.Fact]
    public Task ReadTest3() => ReadTest<Test3>("1a 03 08 96 01 08 96 01", DeserializeTest3Async, "C: [A: 150]");

    class Test3
    {
        public Test1 C { get; set; }
        public override string ToString() => $"C: [{C}]";
    }

    private async Task ReadTest<T>(string hex, Func<AsyncProtoReader, T, ValueTask<T>> deserializer, string expected)
    {
        var pipe = _factory.Create();
        await AppendPayloadAsync(pipe, hex);
        pipe.Writer.Complete(); // simulate EOF

        Trace("Pipe loaded; deserializing");

        using (AsyncProtoReader reader = new PipeReader(pipe.Reader))
        {
            var obj = await deserializer(reader, default(T));
            string actual = obj?.ToString();
            Trace(actual);
            Assert.Equal(expected, actual);
        }
    }

    private async Task WriteTest<T>(T value, string expected, Func<AsyncProtoWriter, T, ValueTask<long>> serializer)
    {
        var pipe = _factory.Create();
        using (AsyncProtoWriter writer = new PipeWriter(pipe.Writer))
        {
            await serializer(writer, value);
            await writer.FlushAsync();
        }
        var buffer = await pipe.Reader.ReadToEndAsync();
        var actual = NormalizeHex(BitConverter.ToString(buffer.ToArray()));
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

    class Test2
    {
        public string B { get; set; }
        public override string ToString() => $"B: {B}";
    }
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
        protected abstract void ApplyDataConstraint();
        protected abstract void RemoveDataConstraint();
        public virtual void Dispose() { }

        public virtual async ValueTask<bool> SkipFieldAsync()
        {
            switch(WireType)
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

        public abstract ValueTask<string> ReadStringAsync();
        public virtual async ValueTask<int> ReadInt32Async()
        {
            var val = await TryReadVarintInt32Async();
            if (val == null) throw new EndOfStreamException();
            return val.GetValueOrDefault();
        }
        protected abstract ValueTask<int?> TryReadVarintInt32Async();


    }
    abstract class AsyncProtoWriter : IDisposable
    {
        public virtual ValueTask<bool> FlushAsync() => new ValueTask<bool>(false);
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
                + value.Length == 0 ? await WriteVarintUInt32Async(0) : await WriteStringWithLengthPrefix(value);
        }
        protected static readonly Encoding Encoding = Encoding.UTF8;
        protected static TextEncoder Encoder = TextEncoder.Utf8;

        public async ValueTask<int> WriteBytesAsync(int fieldNumber, ReadOnlySpan<byte> value)
        {
            int bytes = await WriteFieldHeader(fieldNumber, WireType.String) + await WriteVarintUInt32Async((uint)value.Length);
            if(value.Length != 0) bytes += await WriteBytes(value);
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
    }
    sealed class PipeWriter : AsyncProtoWriter
    {
        private IPipeWriter _writer;
        private WritableBuffer _output;
        private readonly bool _closePipe;
        private volatile bool _isFlushing;
        public PipeWriter(IPipeWriter writer, bool closePipe = true)
        {
            _writer = writer;
            _closePipe = closePipe;
            _output = writer.Alloc();
        }

        public override async ValueTask<bool> FlushAsync()
        {
            if (_isFlushing) throw new InvalidOperationException("already flushing");
            _isFlushing = true;
            Trace("Flushing...");
            await _output.FlushAsync();
            Trace("Flushed");
            _isFlushing = false;
            return true;
        }

        protected override ValueTask<int> WriteStringWithLengthPrefix(string value)
        {
            // magic cutoff here is *must be less than* 32 bytes - and conveniently 31 is "11111", so test via binary;
            // max 4 bytes per char per RFC3629; 4 * 32 128, which would be *just too big* to guarantee the length prefix fits
            // in one byte; but: for up to 31 chars we can write the value to offset 1, then write the actual length in afterwards
            // into offset 0
            int bytes = (value.Length & ~31) == 0
                ?  WriteShortStringWithLengthPrefix(value)
                : WriteLongStringWithLengthPrefix(value);
            return new ValueTask<int>(bytes);
        }
        unsafe int WriteShortStringWithLengthPrefix(string value)
        {
            // to encode without checking bytes, need 4 times length, plus 1 - the sneaky way
            _output.Ensure((value.Length << 2) | 1);
            var span = _output.Buffer.Span;
            int bytesWritten;
            bool success = Encoder.TryEncode(value, span.Slice(1), out bytesWritten);
            Debug.Assert(success);
            Debug.Assert(bytesWritten <= 127);
            span[0] = (byte)bytesWritten++;
            _output.Advance(bytesWritten);
            Trace($"Wrote '{value}' in {bytesWritten} bytes (including length prefix) without checking length first");
            return bytesWritten;
        }
        unsafe int WriteLongStringWithLengthPrefix(string value)
        {
            int payloadBytes = Encoding.GetByteCount(value);
            _output.Ensure(5);
            int headerBytes = WriteVarintUInt32(_output.Buffer.Span, (uint)payloadBytes);
            _output.Advance(headerBytes);

            if(payloadBytes <= _output.Buffer.Length)
            {
                // already enough space in the output buffer - just write it
                int bytesWritten;
                bool success = Encoder.TryEncode(value, _output.Buffer.Span, out bytesWritten);
                Debug.Assert(success);
                Debug.Assert(bytesWritten == payloadBytes);
                _output.Advance(payloadBytes);
                Trace($"Wrote '{value}' in {bytesWritten} bytes into available buffer space");
            }
            else
            {
                fixed (char* c = value)
                {
                    var utf16 = new ReadOnlySpan<char>(c, value.Length);
#if DEBUG
                    int totalBytesWritten = 0;
#endif
                    do
                    {
                        _output.Ensure(Math.Min(utf16.Length << 2, 128)); // ask for a humble amount, but prepare to be amazed

                        int bytesWritten, charsConsumed;
                        Encoder.TryEncode(utf16, _output.Buffer.Span, out charsConsumed, out bytesWritten);
                        utf16 = utf16.Slice(charsConsumed);
                        _output.Advance(bytesWritten);

                        Trace($"Wrote {charsConsumed} chars of long string in {bytesWritten} bytes");
#if DEBUG
                        totalBytesWritten += bytesWritten;
#endif
                    } while (utf16.Length != 0);
#if DEBUG
                    Debug.Assert(totalBytesWritten == payloadBytes);
#endif

                }
            }
            return headerBytes + payloadBytes;
        }


        protected override ValueTask<int> WriteBytes(ReadOnlySpan<byte> bytes)
        {
            _output.Write(bytes);
            return new ValueTask<int>(bytes.Length);
        }

        protected unsafe override ValueTask<int> WriteVarintUInt32Async(uint value)
        {
            _output.Ensure(5);
            int len = WriteVarintUInt32(_output.Buffer.Span, value);
            _output.Advance(len);
            Trace($"Wrote {value} in {len} bytes");
            return new ValueTask<int>(len);
        }
        protected unsafe override ValueTask<int> WriteVarintUInt64Async(ulong value)
        {
            _output.Ensure(10);
            int len = WriteVarintUInt64(_output.Buffer.Span, value);
            _output.Advance(len);
            Trace($"Wrote {value} in {len} bytes");
            return new ValueTask<int>(len);
        }
        private static unsafe int WriteVarintUInt32(Span<byte> span, uint value)
        {
            Debug.Assert(span.Length >= 5);

            const uint SEVENBITS = (uint)0x7F;
            const byte CONTINUE = (byte)0x80;
            if ((value & ~SEVENBITS) == 0)
            {
                span[0] = (byte)value;
                return 1;
            }

            fixed (byte* spanPtr = &span.DangerousGetPinnableReference())
            {
                var ptr = spanPtr;
                // least significant group first
                *ptr = (byte)(value & SEVENBITS);
                value >>= 7;
                int count = 1;
                do
                {
                    *ptr |= CONTINUE;
                    *(++ptr) = (byte)(value & SEVENBITS);
                    value >>= 7;
                    count++;
                } while (value != 0);
                return count;
            }
        }
        private static unsafe int WriteVarintUInt64(Span<byte> span, ulong value)
        {
            Debug.Assert(span.Length >= 10);

            const ulong SEVENBITS = (uint)0x7F;
            const byte CONTINUE = (byte)0x80;
            if ((value & ~SEVENBITS) == 0)
            {
                span[0] = (byte)value;
                return 1;
            }
            fixed (byte* spanPtr = &span.DangerousGetPinnableReference())
            {
                var ptr = spanPtr;
                // least significant group first
                *ptr = (byte)(value & SEVENBITS);
                value >>= 7;
                int count = 1;
                do
                {
                    *ptr |= CONTINUE;
                    *(++ptr) = (byte)(value & SEVENBITS);
                    value >>= 7;
                    count++;
                } while (value != 0);
                return count;
            }
        }
        public override void Dispose()
        {
            var tmp = _writer;
            _writer = null;
            if(tmp != null)
            {
                if(_isFlushing)
                {
                    tmp.CancelPendingFlush();
                }
                if (_closePipe)
                {
                    tmp.Complete();
                }
            }            
        }
    }
    sealed class PipeReader : AsyncProtoReader
    {
        private IPipeReader _reader;
        private readonly bool _closePipe;
        private volatile bool _isReading;
        ReadableBuffer _available, _originalAsReceived;
        public PipeReader(IPipeReader reader, bool closePipe = true)
        {
            _reader = reader;
            _closePipe = closePipe;
        }
        public override async ValueTask<string> ReadStringAsync()
        {
            var lenOrNull = await TryReadVarintInt32Async();
            if (lenOrNull == null)
            {
                throw new EndOfStreamException();
            }
            int len = lenOrNull.GetValueOrDefault();
            Trace($"String length: {len}");
            if (len == 0)
            {
                return "";
            }
            while (_available.Length < len)
            {
                if (!await RequestMoreDataAsync()) throw new EndOfStreamException();
            }
            var s = _available.Slice(0, len).GetUtf8String();
            Trace($"Read string: {s}");
            _available = _available.Slice(len);
            Advance(len);
            return s;
        }
        private static (int value, int consumed) TryPeekVarintInt32(ref ReadableBuffer buffer)
        {
            Trace($"Parsing varint from {buffer.Length} bytes...");
            return (buffer.IsSingleSpan || buffer.First.Length >= MaxBytesForVarint)
                ? TryPeekVarintSingleSpan(buffer.First.Span)
                : TryPeekVarintMultiSpan(ref buffer);
        }
        private static unsafe (int value, int consumed) TryPeekVarintSingleSpan(Span<byte> span)
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
                int wasForConsoleMessage = _available.Length;
                int allow = checked((int)(End - Position));
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

    private static Task AppendPayloadAsync(IPipe pipe, string hex)
    {
        hex = NormalizeHex(hex);
        var len = hex.Length / 2;
        byte[] blob = new byte[len];
        for (int i = 0; i < blob.Length; i++)
        {
            blob[i] = Convert.ToByte(hex.Substring(2 * i, 2), 16);
        }
        return pipe.Writer.WriteAsync(blob);
    }
}