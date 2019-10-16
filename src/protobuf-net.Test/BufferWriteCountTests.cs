using ProtoBuf.Meta;
using System;
using System.Buffers;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test
{
    // we want to verify that we're counting correctly in the buffer writer
    public class BufferWriteCountTests
    {
        public BufferWriteCountTests(ITestOutputHelper log)
            => _log = log;

        private readonly ITestOutputHelper _log;
        private void Log(string message) => _log?.WriteLine(message);

        [Fact]
        public void WriteAllTheThings()
        {
            using var cw = new CountingWriter();
            
            var state = ProtoWriter.State.Create(cw, RuntimeTypeModel.Default);
            try
            {
                var rand = new Random(12345);
                const int ITER_COUNT = 1024;
                const int MAXLEN = 2048;
                int GetField() => rand.Next(1, 2048);
                int GetInt32() => rand.Next(int.MinValue, int.MaxValue);
                long GetInt64()
                {
                    long x = GetInt32(), y = GetInt32();
                    return (x << 32) | y;
                }
                unsafe string GetString()
                {
                    const string alphabet = "abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ 0123456789 ";
                    var len = rand.Next(MAXLEN);
                    string s = new string('\0', len);
                    fixed (char* c = s)
                    {
                        for (int i = 0; i < len; i++)
                            c[i] = alphabet[rand.Next(alphabet.Length)];
                    }
                    return s;
                }
                ArraySegment<byte> GetBytes(byte[] array)
                {
                    rand.NextBytes(array);
                    var len = rand.Next(array.Length);
                    return new ArraySegment<byte>(array, 0, len);
                }

                for (int i = 0; i < ITER_COUNT; i++)
                {
                    state.WriteFieldHeader(GetField(), WireType.Varint);
                    state.WriteInt32(GetInt32());
                }
                state.Flush();
                Log($"After {ITER_COUNT}x{nameof(state.WriteInt32)}/{WireType.Varint}: {cw.TotalBytes}");
                Assert.Equal(cw.TotalBytes, state.GetPosition());

                for (int i = 0; i < ITER_COUNT; i++)
                {
                    state.WriteFieldHeader(GetField(), WireType.SignedVarint);
                    state.WriteInt32(GetInt32());
                }
                state.Flush();
                Log($"After {ITER_COUNT}x{nameof(state.WriteInt32)}/{WireType.SignedVarint}: {cw.TotalBytes}");
                Assert.Equal(cw.TotalBytes, state.GetPosition());

                for (int i = 0; i < ITER_COUNT; i++)
                {
                    state.WriteFieldHeader(GetField(), WireType.Fixed32);
                    state.WriteInt32(GetInt32());
                }
                state.Flush();
                Log($"After {ITER_COUNT}x{nameof(state.WriteInt32)}/{WireType.Fixed32}: {cw.TotalBytes}");
                Assert.Equal(cw.TotalBytes, state.GetPosition());

                for (int i = 0; i < ITER_COUNT; i++)
                {
                    state.WriteFieldHeader(GetField(), WireType.Fixed64);
                    state.WriteInt32(GetInt32());
                }
                state.Flush();
                Log($"After {ITER_COUNT}x{nameof(state.WriteInt32)}/{WireType.Fixed64}: {cw.TotalBytes}");
                Assert.Equal(cw.TotalBytes, state.GetPosition());

                for (int i = 0; i < ITER_COUNT; i++)
                {
                    state.WriteFieldHeader(GetField(), WireType.Varint);
                    state.WriteInt64(GetInt64());
                }
                state.Flush();
                Log($"After {ITER_COUNT}x{nameof(state.WriteInt64)}/{WireType.Varint}: {cw.TotalBytes}");
                Assert.Equal(cw.TotalBytes, state.GetPosition());

                for (int i = 0; i < ITER_COUNT; i++)
                {
                    state.WriteFieldHeader(GetField(), WireType.SignedVarint);
                    state.WriteInt64(GetInt64());
                }
                state.Flush();
                Log($"After {ITER_COUNT}x{nameof(state.WriteInt64)}/{WireType.SignedVarint}: {cw.TotalBytes}");
                Assert.Equal(cw.TotalBytes, state.GetPosition());

                for (int i = 0; i < ITER_COUNT; i++)
                {
                    state.WriteFieldHeader(GetField(), WireType.Fixed64);
                    state.WriteInt64(GetInt64());
                }
                state.Flush();
                Log($"After {ITER_COUNT}x{nameof(state.WriteInt64)}/{WireType.Fixed64}: {cw.TotalBytes}");
                Assert.Equal(cw.TotalBytes, state.GetPosition());

                for (int i = 0; i < ITER_COUNT; i++)
                {
                    state.WriteFieldHeader(GetField(), WireType.String);
                    state.WriteString(GetString());
                }
                state.Flush();
                Log($"After {ITER_COUNT}x{nameof(state.WriteString)}/{WireType.String}: {cw.TotalBytes}");
                Assert.Equal(cw.TotalBytes, state.GetPosition());


                var arr = ArrayPool<byte>.Shared.Rent(MAXLEN);
                for (int i = 0; i < ITER_COUNT; i++)
                {
                    state.WriteFieldHeader(GetField(), WireType.String);
                    state.WriteBytes(GetBytes(arr));
                }
                ArrayPool<byte>.Shared.Return(arr);
                state.Flush();
                Log($"After {ITER_COUNT}x{nameof(state.WriteBytes)}/{WireType.String}: {cw.TotalBytes}");
                Assert.Equal(cw.TotalBytes, state.GetPosition());
            }
            catch
            {
                state.Abandon();
                throw;
            }
            finally
            {
                state.Dispose();
            }
        }

        sealed class CountingWriter : IBufferWriter<byte>, IDisposable
        {
            private byte[] _buffer = Array.Empty<byte>();
            private bool _haveValidBuffer; // you're only allowed to call Advance *once* per fetch

            public void Advance(int count)
            {
                if (!_haveValidBuffer) throw new InvalidOperationException(
                        $"{nameof(Advance)} was called with {count}, but the buffer was not valid");
                if (count < 0) throw new ArgumentOutOfRangeException(
                    nameof(count), $"Invalid count: {count}");
                _haveValidBuffer = false;
                TotalBytes += count;
            }

            public long TotalBytes { get; private set; }

            private byte[] Prepare(int sizeHint)
            {
                if (sizeHint < 0) throw new ArgumentOutOfRangeException(nameof(sizeHint));
                if (sizeHint > _buffer.Length) Expand(sizeHint);
                _haveValidBuffer = true;
                return _buffer;
            }
            public Memory<byte> GetMemory(int sizeHint = 0) => Prepare(sizeHint);

            public Span<byte> GetSpan(int sizeHint = 0) => Prepare(sizeHint);

            public CountingWriter() => Expand(16);

            private void Expand(int size)
            {
                Recycle(ref _buffer);
                _buffer = ArrayPool<byte>.Shared.Rent(size);
            }
            
            private static void Recycle(ref byte[] array)
            {
                var tmp = array;
                array = Array.Empty<byte>();
                if (tmp != null && tmp.Length != 0)
                    ArrayPool<byte>.Shared.Return(tmp);
            }

            public void Dispose() => Recycle(ref _buffer);
        }
    }
}
