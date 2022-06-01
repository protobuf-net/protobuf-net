// #define DESERIALIZE_ONLY
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Google.Protobuf;
using ProtoBuf.Nano;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Benchmark.Nano;

[SimpleJob(RuntimeMoniker.Net60)]
[SimpleJob(RuntimeMoniker.Net472)]
[MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory, BenchmarkLogicalGroupRule.ByJob)]
public class NanoBenchmarks
{
    private const int ExtraResultSize = 32;
    private static readonly ByteString extraResult = ByteString.CopyFrom(new string('b', ExtraResultSize), Encoding.ASCII);

    private byte[] _requestBA, _responseBA;
    private GoogleCodeGen.ForwardRequest _gpbRequest;
    private GoogleCodeGen.ForwardResponse _gpbResponse;
    private HandWrittenPool.ForwardRequest _hwRequest;
    private HandWrittenPool.ForwardResponse _hwResponse;
    private HandWrittenNoPool.ForwardRequest _hwnpRequest;
    private HandWrittenNoPool.ForwardResponse _hwnpResponse;
    private HandWrittenSlab.ForwardRequest _hwsRequest;
    private HandWrittenSlab.ForwardResponse _hwsResponse;

    [GlobalSetup]
    public void Setup()
    {
        const int BatchSize = 3500, RequestContextSize = 32 * 1024, ItemContextSize = 64;

        {
            var request = new GoogleCodeGen.ForwardRequest { TraceId = Guid.NewGuid().ToString("N") };
            for (int i = 0; i < BatchSize; ++i)
            {
                var itemRequest = new GoogleCodeGen.ForwardPerItemRequest { ItemId = i.ToString() };
                var bytes = new string('b', ItemContextSize);
                itemRequest.ItemContext = ByteString.CopyFrom(bytes, Encoding.ASCII);
                request.ItemRequests.Add(itemRequest);
            }

            request.RequestContextInfo = ByteString.CopyFrom(new string('a', RequestContextSize) + "end", Encoding.ASCII);
            request.TraceId = Guid.NewGuid().ToString("N");
            using (var ms = new MemoryStream())
            {
                request.WriteTo(ms);
                _requestBA = ms.ToArray();
                Console.WriteLine($"Request: {_requestBA.Length} bytes");
            }
            var now = DateTime.UtcNow;
            var e2eWatch = Stopwatch.StartNew();
            var response = new GoogleCodeGen.ForwardResponse();
            foreach (var itemRequest in request.ItemRequests)
            {
                var itemResponse = new GoogleCodeGen.ForwardPerItemResponse
                {
                    Result = 100,
                    ExtraResult = extraResult
                };
                response.ItemResponses.Add(itemResponse);
            }
            e2eWatch.Stop();
            response.RouteLatencyInUs = ElapsedInUs(e2eWatch);
            response.RouteStartTimeInTicks = now.Ticks;
            using (var ms = new MemoryStream())
            {
                response.WriteTo(ms);
                _responseBA = ms.ToArray();
                Console.WriteLine($"Response: {_responseBA.Length} bytes");
            }
        }

        // now prepare isolated objects for the serialize
        _gpbRequest = Prepare(_requestBA, GoogleCodeGen.ForwardRequest.Parser.ParseFrom, (obj, bw) => obj.WriteTo(bw), Empty(), obj => obj.CalculateSize(), null);
        _gpbResponse = Prepare(_responseBA, GoogleCodeGen.ForwardResponse.Parser.ParseFrom, (obj, bw) => obj.WriteTo(bw), Empty(), obj => obj.CalculateSize(), null);

        _hwRequest = Prepare(_requestBA, buffer =>
        {
            var reader = new Reader(buffer);
            var obj = HandWrittenPool.ForwardRequest.Merge(null, ref reader);
            reader.Dispose();
            return obj;
        }, (obj, bw) =>
        {
            var writer = new Writer(bw);
            HandWrittenPool.ForwardRequest.WriteSingle(obj, ref writer);
            writer.Dispose();
        }, Empty(), obj => (long)HandWrittenPool.ForwardRequest.Measure(obj), obj => obj.Dispose());

        _hwResponse = Prepare(_responseBA, buffer =>
        {
            var reader = new Reader(buffer);
            var obj = HandWrittenPool.ForwardResponse.Merge(null, ref reader);
            reader.Dispose();
            return obj;
        }, (obj, bw) =>
        {
            var writer = new Writer(bw);
            HandWrittenPool.ForwardResponse.WriteSingle(obj, ref writer);
            writer.Dispose();
        }, Empty(), obj => (long)HandWrittenPool.ForwardResponse.Measure(obj), obj => obj.Dispose());

        _hwnpRequest = Prepare(_requestBA, buffer =>
        {
            var reader = new Reader(buffer);
            HandWrittenNoPool.ForwardRequest obj = default;
            HandWrittenNoPool.ForwardRequest.Merge(ref obj, ref reader, true);
            reader.Dispose();
            return obj;
        }, (obj, bw) =>
        {
            var writer = new Writer(bw);
            HandWrittenNoPool.ForwardRequest.WriteSingle(obj, ref writer);
            writer.Dispose();
        }, Empty(), obj => (long)HandWrittenNoPool.ForwardRequest.Measure(obj), null);

        _hwnpResponse = Prepare(_responseBA, buffer =>
        {
            var reader = new Reader(buffer);
            var obj = HandWrittenNoPool.ForwardResponse.Merge(null, ref reader);
            reader.Dispose();
            return obj;
        }, (obj, bw) =>
        {
            var writer = new Writer(bw);
            HandWrittenNoPool.ForwardResponse.WriteSingle(obj, ref writer);
            writer.Dispose();
        }, Empty(), obj => (long)HandWrittenNoPool.ForwardResponse.Measure(obj), null);

        _hwsRequest = Prepare(_requestBA, buffer =>
        {
            var reader = new Reader(buffer);
            var obj = HandWrittenSlab.ForwardRequest.Merge(null, ref reader);
            reader.Dispose();
            return obj;
        }, (obj, bw) =>
        {
            var writer = new Writer(bw);
            HandWrittenSlab.ForwardRequest.WriteSingle(obj, ref writer);
            writer.Dispose();
        }, Empty(), obj => (long)HandWrittenSlab.ForwardRequest.Measure(obj), obj => obj.Dispose());

        _hwsResponse = Prepare(_responseBA, buffer =>
        {
            var reader = new Reader(buffer);
            var obj = HandWrittenSlab.ForwardResponse.Merge(null, ref reader);
            reader.Dispose();
            return obj;
        }, (obj, bw) =>
        {
            var writer = new Writer(bw);
            HandWrittenSlab.ForwardResponse.WriteSingle(obj, ref writer);
            writer.Dispose();
        }, Empty(), obj => (long)HandWrittenSlab.ForwardResponse.Measure(obj), obj => obj.Dispose());
    }

    const string CategorySerialize = "Serialize", CategoryDeserialize = "Deserialize", CategoryMeasure = "Measure", CategoryRequest = "Request", CategoryResponse = "Response";

#if !DESERIALIZE_ONLY
    [Benchmark]
    [BenchmarkCategory(CategoryMeasure, CategoryRequest)]
    public void MeasureRequestGBP()
    {
        _gpbRequest.CalculateSize();
    }

    [Benchmark]
    [BenchmarkCategory(CategoryMeasure, CategoryResponse)]
    public void MeasureResponseGBP()
    {
        _gpbResponse.CalculateSize();
    }

    [Benchmark]
    [BenchmarkCategory(CategoryMeasure, CategoryRequest)]
    public void MeasureRequestNano()
    {
        HandWrittenPool.ForwardRequest.Measure(_hwRequest);
    }

    [Benchmark]
    [BenchmarkCategory(CategoryMeasure, CategoryResponse)]
    public void MeasureResponseNano()
    {
        HandWrittenPool.ForwardResponse.Measure(_hwResponse);
    }

    [Benchmark]
    [BenchmarkCategory(CategoryMeasure, CategoryRequest)]
    public void MeasureRequestNanoNoPool()
    {
        HandWrittenNoPool.ForwardRequest.Measure(_hwnpRequest);
    }

    [Benchmark]
    [BenchmarkCategory(CategoryMeasure, CategoryResponse)]
    public void MeasureResponseNanoNoPool()
    {
        HandWrittenNoPool.ForwardResponse.Measure(_hwnpResponse);
    }

    [Benchmark]
    [BenchmarkCategory(CategoryMeasure, CategoryRequest)]
    public void MeasureRequestNanoSlab()
    {
        HandWrittenSlab.ForwardRequest.Measure(_hwsRequest);
    }

    [Benchmark]
    [BenchmarkCategory(CategoryMeasure, CategoryResponse)]
    public void MeasureResponseNanoSlab()
    {
        HandWrittenSlab.ForwardResponse.Measure(_hwsResponse);
    }

    [Benchmark]
    [BenchmarkCategory(CategorySerialize, CategoryRequest)]
    public void SerializeRequestGBP()
    {
        _gpbRequest.WriteTo(Empty());
    }

    [Benchmark]
    [BenchmarkCategory(CategorySerialize, CategoryResponse)]
    public void SerializeResponseGBP()
    {
        _gpbResponse.WriteTo(Empty());
    }

    [Benchmark]
    [BenchmarkCategory(CategorySerialize, CategoryRequest)]
    public void SerializeRequestNano()
    {
        var writer = new Writer(Empty());
        HandWrittenPool.ForwardRequest.WriteSingle(_hwRequest, ref writer);
        writer.Dispose();
    }

    [Benchmark]
    [BenchmarkCategory(CategorySerialize, CategoryResponse)]
    public void SerializeResponseNano()
    {
        var writer = new Writer(Empty());
        HandWrittenPool.ForwardResponse.WriteSingle(_hwResponse, ref writer);
        writer.Dispose();
    }

    [Benchmark]
    [BenchmarkCategory(CategorySerialize, CategoryRequest)]
    public void SerializeRequestNanoNoPool()
    {
        var writer = new Writer(Empty());
        HandWrittenNoPool.ForwardRequest.WriteSingle(_hwnpRequest, ref writer);
        writer.Dispose();
    }

    [Benchmark]
    [BenchmarkCategory(CategorySerialize, CategoryResponse)]
    public void SerializeResponseNanoNoPool()
    {
        var writer = new Writer(Empty());
        HandWrittenNoPool.ForwardResponse.WriteSingle(_hwnpResponse, ref writer);
        writer.Dispose();
    }

    [Benchmark]
    [BenchmarkCategory(CategorySerialize, CategoryRequest)]
    public void SerializeRequestNanoSlab()
    {
        var writer = new Writer(Empty());
        HandWrittenSlab.ForwardRequest.WriteSingle(_hwsRequest, ref writer);
        writer.Dispose();
    }

    [Benchmark]
    [BenchmarkCategory(CategorySerialize, CategoryResponse)]
    public void SerializeResponseNanoSlab()
    {
        var writer = new Writer(Empty());
        HandWrittenSlab.ForwardResponse.WriteSingle(_hwsResponse, ref writer);
        writer.Dispose();
    }
#endif

    [Benchmark]
    [BenchmarkCategory(CategoryDeserialize, CategoryRequest)]
    public void DeserializeRequestGBP()
    {
        GoogleCodeGen.ForwardRequest.Parser.ParseFrom(_requestBA);
    }

    [Benchmark]
    [BenchmarkCategory(CategoryDeserialize, CategoryResponse)]
    public void DeserializeResponseGBP()
    {
        GoogleCodeGen.ForwardResponse.Parser.ParseFrom(_responseBA);
    }

    [Benchmark]
    [BenchmarkCategory(CategoryDeserialize, CategoryRequest)]
    public void DeserializeRequestNano()
    {
        var reader = new Reader(_requestBA);
        var obj = HandWrittenPool.ForwardRequest.Merge(null, ref reader);
        reader.Dispose();
        obj.Dispose();
    }

    [Benchmark]
    [BenchmarkCategory(CategoryDeserialize, CategoryResponse)]
    public void DeserializeResponseNano()
    {
        var reader = new Reader(_responseBA);
        var obj = HandWrittenPool.ForwardResponse.Merge(null, ref reader);
        reader.Dispose();
        obj.Dispose();
    }

    [Benchmark]
    [BenchmarkCategory(CategoryDeserialize, CategoryRequest)]
    public void DeserializeRequestNanoNoPool()
    {
        var reader = new Reader(_requestBA);
        HandWrittenNoPool.ForwardRequest obj = default;
        HandWrittenNoPool.ForwardRequest.Merge(ref obj, ref reader, true);
        reader.Dispose();
    }

    [Benchmark]
    [BenchmarkCategory(CategoryDeserialize, CategoryResponse)]
    public void DeserializeResponseNanoNoPool()
    {
        var reader = new Reader(_responseBA);
        HandWrittenNoPool.ForwardResponse.Merge(null, ref reader);
        reader.Dispose();
    }

    [Benchmark]
    [BenchmarkCategory(CategoryDeserialize, CategoryRequest)]
    public void DeserializeRequestNanoSlab()
    {
        var reader = new Reader(_requestBA);
        var obj = HandWrittenSlab.ForwardRequest.Merge(null, ref reader);
        reader.Dispose();
        obj.Dispose();
    }

    [Benchmark]
    [BenchmarkCategory(CategoryDeserialize, CategoryResponse)]
    public void DeserializeResponseNanoSlab()
    {
        var reader = new Reader(_responseBA);
        var obj = HandWrittenSlab.ForwardResponse.Merge(null, ref reader);
        reader.Dispose();
        obj.Dispose();
    }


    private readonly SimpleBufferWriter _bw = new SimpleBufferWriter();
    private SimpleBufferWriter Empty()
    {
        _bw.Clear();
        return _bw;
    }
    
    // tests deserialize, serialize and measure, and validates serialize and measure outputs
    private static T Prepare<T>(byte[] payload, Func<byte[], T> parser, Action<T, IBufferWriter<byte>> writer,
        SimpleBufferWriter bw, Func<T, long> measure, Action<T>? dispose)
    {
        var obj = parser(payload);
        writer(obj, bw);
        if (!bw.TotalSpan.SequenceEqual(payload))
        {
            throw new InvalidOperationException("Data round trip error");
        }
        var length = measure(obj);
        if (length != bw.TotalLength)
        {
            throw new InvalidOperationException("Measure error");
        }
        dispose?.Invoke(obj);
        return obj;
    }

    private static long ElapsedInUs(Stopwatch timer) => (long)(timer.Elapsed.TotalMilliseconds * 1000);

    sealed class SimpleBufferWriter : IBufferWriter<byte>
    {
        byte[] _arr = Array.Empty<byte>();
        int _space;

        public int TotalLength => _arr.Length - _space;
        public ReadOnlySpan<byte> TotalSpan => new ReadOnlySpan<byte>(_arr, 0, TotalLength);

        public void Clear() => _space = _arr.Length;
        public void Advance(int count) => _space -= count;

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            if (sizeHint < 10) sizeHint = 10;
            if (sizeHint > _space) Resize(sizeHint);
            return new Memory<byte>(_arr, _arr.Length - _space, _space);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            if (sizeHint < 10) sizeHint = 10;
            if (sizeHint > _space) Resize(sizeHint);
            return new Span<byte>(_arr, _arr.Length - _space, _space);
        }

        private void Resize(int sizeHint)
        {
            var usedBytes = TotalLength;
            var newMinBytes = usedBytes + sizeHint;
            var newArr = ArrayPool<byte>.Shared.Rent(newMinBytes);
            Buffer.BlockCopy(_arr, 0, newArr, 0, usedBytes);
            ArrayPool<byte>.Shared.Return(_arr);
            _arr = newArr;
            _space = newArr.Length - usedBytes;
        }
    }
}
