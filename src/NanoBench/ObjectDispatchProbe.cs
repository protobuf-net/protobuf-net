using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Buffers;

namespace ProtoBuf.Nano.Bench;

/// <summary>
/// Where do 2272 bytes per call come from? — <c>notes/gaps.md</c> B24.
/// </summary>
/// <remarks>
/// <para>
/// <c>Serialize&lt;object&gt;</c> against a <c>RuntimeTypeModel</c> costs ~2951 ns and allocates
/// 2272 B, where the typed form costs ~72 ns and allocates nothing. Neither half is slow alone: the
/// same object-typed dispatch against a <b>generated</b> model is 76 ns and zero. So it is the pair,
/// and the question is which step in the pair is responsible.
/// </para>
/// <para>
/// <c>GC.GetAllocatedBytesForCurrentThread()</c> answers that exactly and needs no profiler
/// attached, which matters because this has to be reproducible by anyone. It is precise per-thread
/// and counts every allocation, so bracketing a step gives that step's bytes outright.
/// </para>
/// </remarks>
internal static class ObjectDispatchProbe
{
    [ProtoContract]
    public class Payload
    {
        [ProtoMember(1)] public int Id { get; set; }
        [ProtoMember(2)] public string Name { get; set; }
    }

    private sealed class Sink : IBufferWriter<byte>
    {
        private readonly byte[] _buffer = new byte[64 * 1024];
        private int _written;
        public void Reset() => _written = 0;
        public void Advance(int count) => _written += count;
        public Memory<byte> GetMemory(int sizeHint = 0) => new(_buffer, _written, _buffer.Length - _written);
        public Span<byte> GetSpan(int sizeHint = 0) => new(_buffer, _written, _buffer.Length - _written);
    }

    private static long Measure(string label, int iterations, Action action)
    {
        for (int i = 0; i < 50; i++) action();          // settle any one-off caches first
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < iterations; i++) action();
        var total = GC.GetAllocatedBytesForCurrentThread() - before;
        var per = total / (double)iterations;
        Console.WriteLine($"{label,-46} {per,10:F1} B/call");
        return (long)per;
    }

    public static void Run()
    {
        const int N = 2000;
        var runtime = RuntimeTypeModel.Create();
        runtime.Add(typeof(Payload), true);
        var value = new Payload { Id = 42, Name = "hello" };
        var sink = new Sink();

        Console.WriteLine("=== gaps.md B24: allocation per call ===");
        Console.WriteLine();

        Measure("RuntimeTypeModel, generic", N, () =>
        {
            sink.Reset();
            runtime.Serialize(sink, value);
        });

        Measure("RuntimeTypeModel, object", N, () =>
        {
            sink.Reset();
            runtime.Serialize<object>(sink, value);
        });

        // ---- bisect: does the DESTINATION change the answer? ----
        var ms = new System.IO.MemoryStream();
        Measure("RuntimeTypeModel, object -> Stream", N, () =>
        {
            ms.Position = 0;
            ms.SetLength(0);
            runtime.Serialize<object>(ms, value);
        });

        Measure("RuntimeTypeModel, generic -> Stream", N, () =>
        {
            ms.Position = 0;
            ms.SetLength(0);
            runtime.Serialize(ms, value);
        });

        Console.WriteLine();
        Console.WriteLine("=== attribution: the two serializer lookups the object path makes ===");

        // object has no service, so this MISSES the cache every call - and GetServicesSlow only
        // stores POSITIVE results, so the miss is never memoised
        Measure("  TryGetSerializer<object>  (never cached)", N,
            () => { _ = TypeModel.TryGetSerializer<object>(runtime); });

        // the concrete type does have one, so this is a Hashtable hit
        Measure("  TryGetSerializer<Payload> (cached)", N,
            () => { _ = TypeModel.TryGetSerializer<Payload>(runtime); });

        Console.WriteLine();
        Console.WriteLine("=== is it per-call, or a one-off that never warms? ===");
        foreach (var n in new[] { 1, 10, 100, 1000 })
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < n; i++) { sink.Reset(); runtime.Serialize<object>(sink, value); }
            var total = GC.GetAllocatedBytesForCurrentThread() - before;
            Console.WriteLine($"  {n,5} calls -> {total,9} B  ({total / (double)n,8:F1} B/call)");
        }
    }
}
