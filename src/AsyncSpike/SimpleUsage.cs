
using System;
using System.IO;
using System.IO.Pipelines;
using System.IO.Pipelines.Text.Primitives;
using System.Threading.Tasks;

public class SimpleUsage
{
    static void Main()
    {
        try
        {
            Console.WriteLine("Running...");
            new SimpleUsage().RunGoogleTests().Wait();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
        }
    }
    // see example in: https://developers.google.com/protocol-buffers/docs/encoding

    public async Task RunGoogleTests()
    {
        await Console.Out.WriteLineAsync(nameof(RunTest1));
        await RunTest1();

        await Console.Out.WriteLineAsync();
        await Console.Out.WriteLineAsync(nameof(RunTest2));
        await RunTest2();

        await Console.Out.WriteLineAsync();
        await Console.Out.WriteLineAsync(nameof(RunTest3));
        await RunTest3();
    }

    [Xunit.Fact]
    public async Task RunTest1()
    {
        await RunTest("08 96 01", reader => Deserialize<Test1>(reader, DeserializeTest1Async));
    }

    // note: this code would be spat out my the roslyn generator API
    async ValueTask<Test1> DeserializeTest1Async(
        AsyncProtoReader reader, Test1 value = default(Test1))
    {
        await Console.Out.WriteLineAsync("Reading fields...");
        while (await reader.ReadNextFieldAsync())
        {
            await Console.Out.WriteLineAsync($"Reading field {reader.FieldNumber}...");
            switch (reader.FieldNumber)
            {
                case 1:
                    (value ?? Create(ref value)).A = await reader.ReadInt32Async();
                    break;
                default:
                    await reader.SkipFieldAsync();
                    break;
            }
            await Console.Out.WriteLineAsync($"Reading next field...");
        }
        return value ?? Create(ref value);
    }

    class Test1
    {
        public int A;
        public override string ToString() => $"A: {A}";
    }

    [Xunit.Fact]
    public async Task RunTest2()
    {
        await RunTest("12 07 74 65 73 74 69 6e 67", reader => Deserialize<Test2>(reader, DeserializeTest2Async));
    }

    static async ValueTask<bool> Deserialize<T>(IPipeReader reader, Func<AsyncProtoReader, T, ValueTask<T>> deserializer) where T : class, new()
    {
        // may be other AsyncProtoReader - over Stream, perhaps
        using (AsyncProtoReader protoReader = new PipeReader(reader))
        {
            var obj = await deserializer(protoReader, null);
            await Console.Out.WriteLineAsync(obj?.ToString() ?? "null");
            return obj != null;
        }
    }


    [Xunit.Fact]
    public async Task RunTest3()
    {
        // note I've suffixed with another dummy "1" field to test the end sub-object code
        await RunTest("1a 03 08 96 01 08 96 01", reader => Deserialize<Test3>(reader, DeserializeTest3Async));
    }

    class Test3
    {
        public Test1 C { get; set; }
        public override string ToString() => $"C: {C}";
    }

    private async static ValueTask<bool> RunTest(string hex, Func<IPipeReader, ValueTask<bool>> test)
    {
        using (var factory = new PipeFactory())
        {
            var pipe = factory.Create();
            await AppendPayloadAsync(pipe, hex);
            pipe.Writer.Complete(); // simulate EOF

            await Console.Out.WriteLineAsync("Pipe loaded; deserializing");

            return await test(pipe.Reader);
        }
    }


    // note: this code would be spat out my the roslyn generator API
    async ValueTask<Test2> DeserializeTest2Async(
        AsyncProtoReader reader, Test2 value = default(Test2))
    {
        await Console.Out.WriteLineAsync("Reading fields...");
        while (await reader.ReadNextFieldAsync())
        {
            await Console.Out.WriteLineAsync($"Reading field {reader.FieldNumber}...");
            switch (reader.FieldNumber)
            {
                case 2:
                    (value ?? Create(ref value)).B = await reader.ReadStringAsync();
                    break;
                default:
                    await reader.SkipFieldAsync();
                    break;
            }
            await Console.Out.WriteLineAsync($"Reading next field...");
        }
        return value ?? Create(ref value);
    }

    async ValueTask<Test3> DeserializeTest3Async(
        AsyncProtoReader reader, Test3 value = default(Test3))
    {
        await Console.Out.WriteLineAsync("Reading fields...");
        while (await reader.ReadNextFieldAsync())
        {
            await Console.Out.WriteLineAsync($"Reading field {reader.FieldNumber}...");
            switch (reader.FieldNumber)
            {
                case 3:
                    var token = await reader.ReadSubObjectAsync();
                    (value ?? Create(ref value)).C = await DeserializeTest1Async(reader, value?.C);
                    reader.EndSubObject(token);
                    break;
                default:
                    await reader.SkipFieldAsync();
                    break;
            }
            await Console.Out.WriteLineAsync($"Reading next field...");
        }
        return value ?? Create(ref value);
    }

    static T Create<T>(ref T obj) where T : class, new()
         => obj ?? (obj = new T());


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
        public async ValueTask<SubObjectToken> ReadSubObjectAsync()
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
        public void EndSubObject(SubObjectToken token)
        {
            if (token.End != _end) throw new InvalidOperationException("Sub-object ended in wrong order");
            if (token.End != _position) throw new InvalidOperationException("Sub-object not fully consumed");
            RemoveDataConstraint();
            _end = token.OldEnd;
            if (_end != long.MaxValue)
            {
                ApplyDataConstraint();
            }
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

    sealed class PipeReader : AsyncProtoReader
    {
        private IPipeReader _reader;
        private readonly bool _closePipe;
        private bool _isReading;
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
            await Console.Out.WriteLineAsync($"String length: {len}");
            if (len == 0)
            {
                return "";
            }
            while (_available.Length < len)
            {
                if (!await RequestMoreDataAsync()) throw new EndOfStreamException();
            }
            var s = _available.Slice(0, len).GetUtf8String();
            await Console.Out.WriteLineAsync($"Read string: {s}");
            _available = _available.Slice(len);
            Advance(len);
            return s;
        }
        private static (int value, int consumed) TryPeekVarintInt32(ref ReadableBuffer buffer)
        {
            Console.WriteLine($"Parsing varint from {buffer.Length} bytes...");
            return buffer.IsSingleSpan
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
                await Console.Out.WriteLineAsync("Refusing more data to sub-object");
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
                Console.Out.WriteLine($"Data constraint removed; {_available.Length} bytes available (was {wasForConsoleMessage})");
            }
        }
        protected override void ApplyDataConstraint()
        {
            if (End != long.MaxValue && checked(Position + _available.Length) > End)
            {
                int wasForConsoleMessage = _available.Length;
                int allow = checked((int)(End - Position));
                _available = _available.Slice(0, allow);
                Console.Out.WriteLine($"Data constraint imposed; {_available.Length} bytes available (was {wasForConsoleMessage})");
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
                    reader.Advance(available.Start, available.End);
                }

                if (_closePipe)
                {
                    reader.Complete();
                }
            }
        }
    }

    private static Task AppendPayloadAsync(IPipe pipe, string hex)
    {
        hex = hex.Replace('-', ' ').Replace(" ", "").Trim();
        var len = hex.Length / 2;
        byte[] blob = new byte[len];
        for (int i = 0; i < blob.Length; i++)
        {
            blob[i] = Convert.ToByte(hex.Substring(2 * i, 2), 16);
        }
        return pipe.Writer.WriteAsync(blob);
    }
}