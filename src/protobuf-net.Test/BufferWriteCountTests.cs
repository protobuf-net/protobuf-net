using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;
using System.Buffers;
using System.Collections.Generic;
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


        class MyMessage
        {
            public int X { get; set; }
            public string Y { get; set; }
        }

        class MyTypeModel : TypeModel
        {
            public static MyTypeModel Instance { get; } = new MyTypeModel();
            private MyTypeModel() { }

            protected override ISerializer<T> GetSerializer<T>()
                => GetSerializer<MyServices, T>();

            class MyServices : ISerializer<MyMessage>
            {
                public SerializerFeatures Features => SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString;

                public MyMessage Read(ref ProtoReader.State state, MyMessage value)
                {
                    value ??= new MyMessage();
                    int field;
                    while ((field = state.ReadFieldHeader()) > 0)
                    {
                        switch(field)
                        {
                            case 12: value.Y = state.ReadString(); break;
                            case 42: value.X = state.ReadInt32(); break;
                            default: state.SkipField(); break;
                        }
                    }
                    return value;
                }

                public void Write(ref ProtoWriter.State state, MyMessage value)
                {
                    state.WriteString(12, value.Y);
                    state.WriteInt32Varint(42, value.X);
                }
            }
        }

        [Fact]
        public void WriteAllTheThings()
        {
            using var cw = new CountingWriter();

            var state = ProtoWriter.State.Create(cw, MyTypeModel.Instance);
            try
            {
                var rand = new Random(12345);
                const int ITER_COUNT = 1024, SMALL_ITER_COUNT = 128, MAXLEN = 2048;
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
                var valuesInt32 = new List<int>();
                List<int> GetValuesInt32()
                {
                    valuesInt32.Clear();
                    var len = rand.Next(MAXLEN);
                    for (int i = 0; i < len; i++)
                        valuesInt32.Add(GetInt32());
                    return valuesInt32;
                }
                var valuesString = new List<string>();
                List<string> GetValuesString()
                {
                    valuesString.Clear();
                    var len = rand.Next(MAXLEN);
                    for (int i = 0; i < len; i++)
                        valuesString.Add(GetString());
                    return valuesString;
                }

                MyMessage GetMessage()
                {
                    return new MyMessage { X = GetInt32(), Y = GetString() };
                }
                var valuesMessage = new List<MyMessage>();
                List<MyMessage> GetValuesMessage()
                {
                    valuesMessage.Clear();
                    var len = rand.Next(MAXLEN);
                    for (int i = 0; i < len; i++)
                        valuesMessage.Add(GetMessage());
                    return valuesMessage;
                }

                //////////////////////////////////////

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

                for (int i = 0; i < ITER_COUNT; i++)
                {
                    state.WriteFieldHeader(GetField(), WireType.String);
                    state.WriteMessage(default, GetMessage());
                }
                state.Flush();
                Log($"After {ITER_COUNT}x{nameof(state.WriteMessage)}/{WireType.String}: {cw.TotalBytes}");

                for (int i = 0; i < ITER_COUNT; i++)
                {
                    state.WriteFieldHeader(GetField(), WireType.StartGroup);
                    state.WriteMessage(default, GetMessage());
                }
                state.Flush();
                Log($"After {ITER_COUNT}x{nameof(state.WriteMessage)}/{WireType.StartGroup}: {cw.TotalBytes}");

                var repeatedInt32 = RepeatedSerializer.CreateList<int>();

                for (int i = 0; i < ITER_COUNT; i++)
                {
                    repeatedInt32.WriteRepeated(ref state, GetField(), SerializerFeatures.WireTypeVarint, GetValuesInt32());
                }
                state.Flush();
                Log($"After {ITER_COUNT}x{nameof(repeatedInt32.WriteRepeated)}/{SerializerFeatures.WireTypeVarint}: {cw.TotalBytes}");

                for (int i = 0; i < ITER_COUNT; i++)
                {
                    repeatedInt32.WriteRepeated(ref state, GetField(), SerializerFeatures.WireTypeSignedVarint, GetValuesInt32());
                }
                state.Flush();
                Log($"After {ITER_COUNT}x{nameof(repeatedInt32.WriteRepeated)}/{SerializerFeatures.WireTypeSignedVarint}: {cw.TotalBytes}");

                for (int i = 0; i < ITER_COUNT; i++)
                {
                    repeatedInt32.WriteRepeated(ref state, GetField(), SerializerFeatures.WireTypeFixed32, GetValuesInt32());
                }
                state.Flush();
                Log($"After {ITER_COUNT}x{nameof(repeatedInt32.WriteRepeated)}/{SerializerFeatures.WireTypeFixed32}: {cw.TotalBytes}");

                for (int i = 0; i < ITER_COUNT; i++)
                {
                    repeatedInt32.WriteRepeated(ref state, GetField(), SerializerFeatures.WireTypeFixed64, GetValuesInt32());
                }
                state.Flush();
                Log($"After {ITER_COUNT}x{nameof(repeatedInt32.WriteRepeated)}/{SerializerFeatures.WireTypeFixed64}: {cw.TotalBytes}");

                for (int i = 0; i < ITER_COUNT; i++)
                {
                    repeatedInt32.WriteRepeated(ref state, GetField(), SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, GetValuesInt32());
                }
                state.Flush();
                Log($"After {ITER_COUNT}x{nameof(repeatedInt32.WriteRepeated)}/{SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled}: {cw.TotalBytes}");

                for (int i = 0; i < ITER_COUNT; i++)
                {
                    repeatedInt32.WriteRepeated(ref state, GetField(), SerializerFeatures.WireTypeSignedVarint | SerializerFeatures.OptionPackedDisabled, GetValuesInt32());
                }
                state.Flush();
                Log($"After {ITER_COUNT}x{nameof(repeatedInt32.WriteRepeated)}/{SerializerFeatures.WireTypeSignedVarint | SerializerFeatures.OptionPackedDisabled}: {cw.TotalBytes}");

                for (int i = 0; i < ITER_COUNT; i++)
                {
                    repeatedInt32.WriteRepeated(ref state, GetField(), SerializerFeatures.WireTypeFixed32 | SerializerFeatures.OptionPackedDisabled, GetValuesInt32());
                }
                state.Flush();
                Log($"After {ITER_COUNT}x{nameof(repeatedInt32.WriteRepeated)}/{SerializerFeatures.WireTypeFixed32 | SerializerFeatures.OptionPackedDisabled}: {cw.TotalBytes}");

                for (int i = 0; i < ITER_COUNT; i++)
                {
                    repeatedInt32.WriteRepeated(ref state, GetField(), SerializerFeatures.WireTypeFixed64 | SerializerFeatures.OptionPackedDisabled, GetValuesInt32());
                }
                state.Flush();
                Log($"After {ITER_COUNT}x{nameof(repeatedInt32.WriteRepeated)}/{SerializerFeatures.WireTypeFixed64 | SerializerFeatures.OptionPackedDisabled}: {cw.TotalBytes}");

                var repeatedString = RepeatedSerializer.CreateList<string>();
                for (int i = 0; i < SMALL_ITER_COUNT; i++)
                {
                    repeatedString.WriteRepeated(ref state, GetField(), SerializerFeatures.WireTypeString, GetValuesString());
                }
                state.Flush();
                Log($"After {SMALL_ITER_COUNT}x{nameof(repeatedString.WriteRepeated)} ({nameof(String)})/{SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled}: {cw.TotalBytes}");

                var repeatedMessage = RepeatedSerializer.CreateList<MyMessage>();
                for (int i = 0; i < SMALL_ITER_COUNT; i++)
                {
                    repeatedMessage.WriteRepeated(ref state, GetField(), SerializerFeatures.WireTypeString, GetValuesMessage());
                }
                state.Flush();
                Log($"After {SMALL_ITER_COUNT}x{nameof(repeatedString.WriteRepeated)} ({nameof(MyMessage)})/{SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled}: {cw.TotalBytes}");

                for (int i = 0; i < SMALL_ITER_COUNT; i++)
                {
                    repeatedMessage.WriteRepeated(ref state, GetField(), SerializerFeatures.WireTypeStartGroup, GetValuesMessage());
                }
                state.Flush();
                Log($"After {SMALL_ITER_COUNT}x{nameof(repeatedString.WriteRepeated)} ({nameof(MyMessage)})/{SerializerFeatures.WireTypeStartGroup | SerializerFeatures.OptionPackedDisabled}: {cw.TotalBytes}");
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
