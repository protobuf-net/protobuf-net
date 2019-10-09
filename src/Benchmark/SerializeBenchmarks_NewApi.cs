#if NEW_API
using BenchmarkDotNet.Attributes;
using Pipelines.Sockets.Unofficial.Buffers;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Buffers;
using System.IO;

namespace Benchmark
{
    partial class SerializeBenchmarks
    {
        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void BufferWriter_CIP() => WriteBufferWriter(_cip);

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void BufferWriter_C() => WriteBufferWriter(_c);

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void BufferWriter_Auto() => WriteBufferWriter(_auto);

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void FakeBufferWriter_CIP() => WriteFakeBufferWriter(_cip);

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void FakeBufferWriter_C() => WriteFakeBufferWriter(_c);

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void FakeBufferWriter_Auto() => WriteFakeBufferWriter(_auto);

        private void WriteBufferWriter(TypeModel model)
        {
            using var buffer = BufferWriter<byte>.Create(64 * 1024);
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var state = ProtoWriter.State.Create(buffer, model);
                try
                {
                    state.SerializeRoot(_database);
                    state.Close();
                }
                finally
                {
                    state.Dispose();
                }
                AssertLength(buffer.Length);
                buffer.Flush().Dispose();
            }
        }

        private void WriteFakeBufferWriter(TypeModel model)
        {
            using var buffer = new FakeBufferWriter();
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var state = ProtoWriter.State.Create(buffer, model);
                try
                {
                    state.SerializeRoot(_database);
                    state.Close();
                }
                finally
                {
                    state.Dispose();
                }
                AssertLength(buffer.Reset());
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void MemoryStream_Auto() => MemoryStream_ViaState(_auto);

        private void MemoryStream_ViaState(TypeModel model)
        {
            using var buffer = new MemoryStream();
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var state = ProtoWriter.State.Create(buffer, model);
                try
                {
                    state.SerializeRoot(_database);
                    state.Close();
                }
                finally
                {
                    state.Dispose();
                }
                AssertLength(buffer.Length);
                buffer.Position = 0;
                buffer.SetLength(0);
            }
        }

        private sealed class FakeBufferWriter : IBufferWriter<byte>, IDisposable
        {
            public long Length { get; private set; }
            public long Reset()
            {
                var len = Length;
                Length = 0;
                return len;
            }
            private byte[] _arr;

            public FakeBufferWriter(int initialSize = 1024)
                => Resize(initialSize);

            public void Advance(int count) => Length += count;

            public Memory<byte> GetMemory(int sizeHint = 0)
            {
                if (_arr.Length < sizeHint) Resize(sizeHint);
                return _arr;
            }

            public Span<byte> GetSpan(int sizeHint = 0)
            {
                if (_arr.Length < sizeHint) Resize(sizeHint);
                return _arr;
            }

            private void Resize(int sizeHint)
            {
                var arr = _arr;
                _arr = null;
                if (arr != null) ArrayPool<byte>.Shared.Return(arr);

                if (sizeHint > 0)
                    _arr = ArrayPool<byte>.Shared.Rent(sizeHint);
            }

            public void Dispose() => Resize(0);
        }
    }
}

#endif