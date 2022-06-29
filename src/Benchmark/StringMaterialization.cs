using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;
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

        [Params(1,10,100,1000)]
        public int Length { get; set; }

        private static readonly UTF8Encoding utf8 = new UTF8Encoding(false);

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke, Baseline = true)]
        public void GetStringArray()
        {
            var buffer = _rawPayload;
            var bytes = Length;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                _ = utf8.GetString(buffer, 0, bytes);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void GetStringSpan()
        {
#if NETCOREAPP3_1_OR_GREATER
            var span = new ReadOnlySpan<byte>(_rawPayload, 0, Length);
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                _ = utf8.GetString(span);
            }
#else
            throw new PlatformNotSupportedException();
#endif
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public unsafe void GetStringUnsafe()
        {
            var bytes = Length;
            fixed (byte* ptr = _rawPayload)
            {
                for (int i = 0; i < OperationsPerInvoke; i++)
                {
                    _ = utf8.GetString(ptr, bytes);
                }
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public unsafe void StringNewOverwrite()
        {
            var bytes = Length;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                fixed (byte* bPtr = _rawPayload)
                {
                    int chars = utf8.GetCharCount(bPtr, bytes);
                    string s = new string('\0', bytes);
                    fixed (char* cPtr = s)
                    {
                        utf8.GetChars(bPtr, bytes, cPtr, chars);
                    }
                }
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public unsafe void StringCreateOverwrite()
        {
#if NETCOREAPP3_1_OR_GREATER
            var bytes = Length;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                fixed (byte* bPtr = _rawPayload)
                {
                    int chars = utf8.GetCharCount(bPtr, bytes);
                    string s = string.Create(chars, (object)null, static delegate { }); // don't actually initialize in the callback
                    fixed (char* cPtr = s)
                    {
                        utf8.GetChars(bPtr, bytes, cPtr, chars);
                    }
                }
            }
#else
            throw new PlatformNotSupportedException();
#endif
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void StringCreate()
        {
#if NETCOREAPP3_1_OR_GREATER
            var bytes = Length;
            var buffer = _rawPayload;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                _ = string.Create(bytes, (bytes, buffer),
                    static (chars, state) => utf8.GetChars(new ReadOnlySpan<byte>(state.buffer, 0, state.bytes), chars)
                );
            }
#else
            throw new PlatformNotSupportedException();
#endif
        }
    }
}
