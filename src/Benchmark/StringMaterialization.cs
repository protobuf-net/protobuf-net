using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Benchmark
{
    [SimpleJob(RuntimeMoniker.Net472), SimpleJob(RuntimeMoniker.NetCoreApp31), SimpleJob(RuntimeMoniker.Net60), MemoryDiagnoser]
    public class StringMaterialization
    {
        private readonly byte[] _rawPayload = CreateData(1024);

        private static byte[] CreateData(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz -+0123456789";

            var rand = new Random(121341);
            byte[] data = new byte[length];
            
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)chars[rand.Next(0, chars.Length)]; // note: ASCII
            }
            return data;
        }

        private const int OperationsPerInvoke = 1024;

        [Params(10, 1000)]
        public int Length { get; set; }

        [Params(0, 5)]
        public int Offset { get; set; }

        private static readonly UTF8Encoding utf8 = new UTF8Encoding(false);

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke, Baseline = true)]
        public void GetStringArray()
        {
            var buffer = _rawPayload;
            var bytes = Length;
            var offset = Offset;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                _ = utf8.GetString(buffer, offset, bytes);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void GetStringSpanSlice()
        {
#if NETCOREAPP3_1_OR_GREATER
            ReadOnlySpan<byte> span = _rawPayload;
            var bytes = Length;
            var offset = Offset;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                _ = utf8.GetString(span.Slice(offset, bytes));
            }
#endif
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public unsafe void GetStringUnsafe()
        {
            var bytes = Length;
            var offset = Offset;
            fixed (byte* ptr = _rawPayload)
            {
                for (int i = 0; i < OperationsPerInvoke; i++)
                {
                    _ = utf8.GetString(ptr + offset, bytes);
                }
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public unsafe void StringNewOverwrite()
        {
            var bytes = Length;
            var offset = Offset;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                fixed (byte* bPtr = _rawPayload)
                {
                    int chars = utf8.GetCharCount(bPtr + offset, bytes);
                    string s = new string('\0', bytes);
                    fixed (char* cPtr = s)
                    {
                        utf8.GetChars(bPtr + offset, bytes, cPtr, chars);
                    }
                }
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public unsafe void StringCreateOverwriteUnsafe()
        {
#if NETCOREAPP3_1_OR_GREATER
            var bytes = Length;
            var offset = Offset;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                fixed (byte* bPtr = _rawPayload)
                {
                    int chars = utf8.GetCharCount(bPtr, bytes + offset);
                    string s = string.Create(chars, (object)null, static delegate { }); // don't actually initialize in the callback
                    fixed (char* cPtr = s)
                    {
                        utf8.GetChars(bPtr, bytes + offset, cPtr, chars);
                    }
                }
            }
#endif
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void StringCreateOverwriteSpans()
        {
#if NETCOREAPP3_1_OR_GREATER
            var bytes = Length;
            var offset = Offset;
            ReadOnlySpan<byte> span = _rawPayload;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var slice = span.Slice(offset, Length);
                int chars = utf8.GetCharCount(slice);
                var s = string.Create(chars, (object)null, static delegate { }); // don't actually initialize in the callback
                utf8.GetChars(slice, MemoryMarshal.AsMemory(s.AsMemory()).Span);
            }
#endif
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void StringCreate()
        {
#if NETCOREAPP3_1_OR_GREATER
            var bytes = Length;
            var offset = Offset;
            var buffer = _rawPayload;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                _ = string.Create(bytes, (buffer, offset, bytes),
                    static (chars, state) => utf8.GetChars(new ReadOnlySpan<byte>(state.buffer, state.offset, state.bytes), chars)
                );
            }
#endif
        }
    }
}
