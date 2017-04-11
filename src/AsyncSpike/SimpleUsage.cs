
using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using System.IO.Pipelines.Text.Primitives;
using System.Buffers;

public class SimpleUsage
{
    [Xunit.Fact]
    public async Task CanRead()
    {  // see example in: https://developers.google.com/protocol-buffers/docs/encoding
        using (var factory = new PipeFactory())
        {
            var pipe = factory.Create();
            await WritePayloadAsync(pipe, "12 07 74 65 73 74 69 6e 67");


            var obj = await DeserializeTest2Async(pipe.Reader);
        }
    }
    // note: should be ValueTask<T> throughout
    async Task<Test2> DeserializeTest2Async(
        IPipeReader reader, Test2 value = default(Test2))
    {
        // may be other AsyncProtoReader - over Stream, perhaps
        using (AsyncProtoReader protoReader = new PipeReader(reader))
        {
            while (await protoReader.ReadNextFieldAsync())
            {
                switch (protoReader.FieldNumber)
                {
                    case 2:
                        (value ?? Create(ref value)).B = await protoReader.ReadStringAsync();
                        break;
                    default:
                        await protoReader.SkipFieldAsync();
                        break;
                }
            }
            return value ?? Create(ref value);
        }

    }
    static T Create<T>(ref T obj) where T : class, new()
         => obj ?? (obj = new T());


    class Test2
    {
        public string B { get; set; }
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
        protected abstract Task<int?> TryReadVarintInt32Async();
    }

    sealed class PipeReader : AsyncProtoReader
    {
        private IPipeReader _reader;
        ReadableBuffer _available;
        public PipeReader(IPipeReader reader)
        {
            this._reader = reader;
        }

        public override async Task<string> ReadStringAsync()
        {
            var lenOrNull = await TryReadVarintInt32Async();
            if(lenOrNull == null)
            {
                throw new EndOfStreamException();
            }
            int len = lenOrNull.GetValueOrDefault();
            if(len == 0)
            {
                return "";
            }
            while (_available.Length < len)
            {
                _reader.Advance(_available.Start, _available.End);
                var read = await _reader.ReadAsync();
                if (read.IsCompleted)
                {
                    throw new EndOfStreamException();
                }
            }
            var s = _available.Slice(0, len).GetUtf8String();
            _available = _available.Slice(len);
            return s;
        }
        private static int? TryReadVarintInt32(ref ReadableBuffer buffer)
        {
            int value = 0;
            int consumed = 0, shift = 0;
            if(buffer.IsSingleSpan)
            {
                if(ReadVarint(buffer.First.Span, ref value, ref consumed, ref shift))
                {
                    buffer = buffer.Slice(consumed);
                    return value;
                }
            }
            else
            {
                foreach(var segment in buffer)
                {
                    if(ReadVarint(segment.Span, ref value, ref consumed, ref shift))
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
                        if((val & 128) == 0)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        const int MaxBytesForVarint = 10;
        protected override async Task<int?> TryReadVarintInt32Async()
        {
            
            int? value;
            while (!(value = TryReadVarintInt32(ref _available)).HasValue && _available.Length < MaxBytesForVarint)
            {
                _reader.Advance(_available.Start, _available.End);
                var read = await _reader.ReadAsync();
                if (read.IsCompleted)
                {
                    if (_available.Length == 0)
                    {
                        return null;
                    }
                    throw new EndOfStreamException();
                }
                _available = read.Buffer;
            }
            return value.GetValueOrDefault();
        }

        public override void Dispose()
        {
            _reader = null;
        }
    }

    private Task WritePayloadAsync(IPipe pipe, string hex)
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