
using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using System.IO.Pipelines.Text.Primitives;
using System.Buffers;

public class SimpleUsage
{
    // note: pretty much everything here should be ValueTask<T> throughout
    static void Main()
    {
        try
        {
            Console.WriteLine("Running...");
            new SimpleUsage().RunGoogleTests().Wait();
        }
        catch(Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
        }
    }
    // see example in: https://developers.google.com/protocol-buffers/docs/encoding
    
    public async Task RunGoogleTests()
    {
        await RunTest1();
        await RunTest2();

    }

    [Xunit.Fact]
    public Task RunTest1()
    {
        return RunTest("08 96 01", reader => Deserialize<Test1>(reader, DeserializeTest1Async));
    }

    // note: this code would be spat out my the roslyn generator API
    async Task<Test1> DeserializeTest1Async(
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
    public Task RunTest2()
    {
        return RunTest("12 07 74 65 73 74 69 6e 67", reader => Deserialize<Test2>(reader, DeserializeTest2Async));
    }

    static async Task Deserialize<T>(IPipeReader reader, Func<AsyncProtoReader, T, Task<T>> deserializer) where T : class, new()
    {
        // may be other AsyncProtoReader - over Stream, perhaps
        using (AsyncProtoReader protoReader = new PipeReader(reader))
        {
            var obj = await deserializer(protoReader, null);
            await Console.Out.WriteLineAsync(obj?.ToString() ?? "null");
        }
    }

    private async static Task RunTest(string hex, Func<IPipeReader, Task> test)
    {
        using (var factory = new PipeFactory())
        {
            var pipe = factory.Create();
            await AppendPayloadAsync(pipe, hex);
            pipe.Writer.Complete(); // simulate EOF

            await Console.Out.WriteLineAsync("Pipe loaded; deserializing");

            await test(pipe.Reader);
        }
    }

    
    // note: this code would be spat out my the roslyn generator API
    async Task<Test2> DeserializeTest2Async(
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
    static T Create<T>(ref T obj) where T : class, new()
         => obj ?? (obj = new T());


    class Test2
    {
        public string B { get; set; }
        public override string ToString() => $"B: {B}";
    }
    abstract class AsyncProtoReader : IDisposable
    {
        public virtual void Dispose() { }

        public virtual Task SkipFieldAsync() => throw new NotImplementedException();

        int _fieldHeader;
        public int FieldNumber => _fieldHeader >> 3;
        public async Task<bool> ReadNextFieldAsync()
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
        public abstract Task<string> ReadStringAsync();
        public virtual async Task<int> ReadInt32Async()
        {
            var val = await TryReadVarintInt32Async();
            if (val == null) throw new EndOfStreamException();
            return val.GetValueOrDefault();
        }
        protected abstract Task<int?> TryReadVarintInt32Async();
    }

    sealed class PipeReader : AsyncProtoReader
    {
        private IPipeReader _reader;
        private readonly bool _closePipe;
        private bool _isReading;
        ReadableBuffer _available;
        public PipeReader(IPipeReader reader, bool closePipe = true)
        {
            _reader = reader;
            _closePipe = closePipe;
        }

        public override async Task<string> ReadStringAsync()
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
                if(!await RequestMoreDataAsync()) throw new EndOfStreamException();
            }
            var s = _available.Slice(0, len).GetUtf8String();
            await Console.Out.WriteLineAsync($"Read string: {s}");
            _available = _available.Slice(len);
            return s;
        }
        private static int? TryReadVarintInt32(ref ReadableBuffer buffer)
        {
            int value = 0;
            int consumed = 0, shift = 0;
            Console.WriteLine($"Parsing varint from {buffer.Length} bytes...");
            if (buffer.IsSingleSpan)
            {
                if (ReadVarint(buffer.First.Span, ref value, ref consumed, ref shift))
                {
                    buffer = buffer.Slice(consumed);
                    return value;
                }
            }
            else
            {
                foreach (var segment in buffer)
                {
                    if (ReadVarint(segment.Span, ref value, ref consumed, ref shift))
                    {
                        buffer = buffer.Slice(consumed);
                        return value;
                    }
                }
            }
            return null;
        }

        private static unsafe bool ReadVarint(Span<byte> span, ref int value, ref int consumed, ref int shift)
        {
            // TODO: move the foreach inside here
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
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        const int MaxBytesForVarint = 10;


        private async Task<bool> RequestMoreDataAsync()
        {
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

                if(read.IsCancelled)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }
            }
            while (_available.Length <= oldLen && !read.IsCompleted);
            _available = read.Buffer;

            return _available.Length > oldLen;
        }
        protected override async Task<int?> TryReadVarintInt32Async()
        {
            do
            {
                int? value = TryReadVarintInt32(ref _available);
                if (value != null) return value.GetValueOrDefault();
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
            if(reader != null)
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