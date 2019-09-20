#if PLAT_SPANS

using Pipelines.Sockets.Unofficial.Buffers;
using ProtoBuf.Meta;
using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Tests
{
    public class BufferWriterTests
    {
        private readonly ITestOutputHelper Log;
        public BufferWriterTests(ITestOutputHelper log)
            => Log = log;

        [Fact]
        public void ManualWriter_Stream()
        {
            using var ms = new MemoryStream();
            using var w = ProtoWriter.Create(out var state, ms, RuntimeTypeModel.Default, null);
            ManualWriter(w, ref state);
        }

        [Fact]
        public void ManualWriter_Null()
        {
            using var w = ProtoWriter.CreateNull(out var state, RuntimeTypeModel.Default, null);
            ManualWriter(w, ref state);
        }

        [Fact]
        public void ManualWriter_Buffer()
        {
            using var bw = BufferWriter<byte>.Create();
            using var w = ProtoWriter.Create(out var state, bw, RuntimeTypeModel.Default, null);
            ManualWriter(w, ref state);
        }

        class Foo
        {
            private Foo() { }

            public static IProtoSerializer<Foo> Serializer => FooSerializer.Instance;
            sealed class FooSerializer : IProtoSerializer<Foo>
            {
                public static FooSerializer Instance { get; } = new FooSerializer();
                private FooSerializer() { }
                public Foo Read(ref ProtoReader.State state, Foo value) => value;

                public void Write(ProtoWriter writer, ref ProtoWriter.State state, Foo value) { }
            }
        }

        private void ManualWriter(ProtoWriter w, ref ProtoWriter.State state)
        {
            try
            {
                state.WriteFieldHeader(1, WireType.Varint);
                Assert.Equal(1, w.GetPosition(ref state));
                state.WriteInt32(42);
                Assert.Equal(2, w.GetPosition(ref state));

                state.WriteFieldHeader(2, WireType.String);
                Assert.Equal(3, w.GetPosition(ref state));
                state.WriteString("abcdefghijklmnop");
                Assert.Equal(20, w.GetPosition(ref state));

                state.WriteFieldHeader(3, WireType.StartGroup);
                Assert.Equal(21, w.GetPosition(ref state));
                ProtoWriter.WriteSubItem<Foo>(null, w, ref state, Foo.Serializer);
                Assert.Equal(22, w.GetPosition(ref state));

                state.WriteFieldHeader(4, WireType.String);
                Assert.Equal(23, w.GetPosition(ref state));
                ProtoWriter.WriteSubItem<Foo>(null, w, ref state, Foo.Serializer);
                Assert.Equal(24, w.GetPosition(ref state));

                w.Close(ref state);
            }
            catch
            {
                w.Abandon();
                throw;
            }
        }

        [ProtoContract]
        public class A
        {
            [ProtoMember(1)]
            public int Level { get; set; }

            [ProtoMember(2)]
            public A Inner { get; set; }
        }

        class ASerializer : IProtoSerializer<A>
        {
            public ASerializer(ITestOutputHelper log) => Log = log;
            public ITestOutputHelper Log { get; }
            public A Read(ref ProtoReader.State state, A value)
            {
                int fieldHeader;
                if (value == null) value = new A();
                while ((fieldHeader = state.ReadFieldHeader()) > 0)
                {
                    switch (fieldHeader)
                    {
                        case 1:
                            value.Level = state.ReadInt32();
                            break;
                        case 2:
                            value.Inner = state.ReadSubItem<A>(value.Inner, this);
                            break;
                        default:
                            state.SkipField();
                            break;
                    }
                }
                return value;
            }

            public void Write(ProtoWriter writer, ref ProtoWriter.State state, A value)
            {
#pragma warning disable CS0618
                Log?.WriteLine($"Writing to {writer.GetType().Name}");
                Log?.WriteLine($"Writing field 1, value: {value.Level}; pos: {writer.GetPosition(ref state)}");
                state.WriteFieldHeader(1, WireType.Varint);
                state.WriteInt32(value.Level);
                Log?.WriteLine($"Wrote field 1... pos: {writer.GetPosition(ref state)}");

                var obj = value.Inner;
                if (obj != null)
                {
                    Log?.WriteLine($"Writing field 2...; pos: {writer.GetPosition(ref state)}");
                    state.WriteFieldHeader(2, WireType.String);
                    ProtoWriter.WriteSubItem<A>(obj, writer, ref state, this);
                    Log?.WriteLine($"Wrote field 2...; pos: {writer.GetPosition(ref state)}");
                }
#pragma warning restore CS0618
            }
        }

        private A CreateModel(int depth)
        {
            Log?.WriteLine($"Creating model with depth {depth}");
            int level = 0;
            A obj = null;
            while (level < depth)
            {
                obj = new A { Inner = obj, Level = ++level };
            }
            return obj;
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(5)]
        public void WriteWithCustomSerializer_Buffer(int depth)
        {
            using var bw = BufferWriter<byte>.Create();
            var model = RuntimeTypeModel.Default;
            using var writer = ProtoWriter.Create(out var state, bw.Writer, model, null);
            A obj = CreateModel(depth);
            var ser = new ASerializer(Log);
            ser.Write(writer, ref state, obj);
            writer.Close(ref state);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(5)]
        public void WriteWithCustomSerializer_Stream(int depth)
        {
            using var ms = new MemoryStream();
            var model = RuntimeTypeModel.Default;
            using var writer = ProtoWriter.Create(out var state, ms, model, null);
            A obj = CreateModel(depth);
            var ser = new ASerializer(Log);
            ser.Write(writer, ref state, obj);
            writer.Close(ref state);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(5)]
        public void RoundTripBasicModel_Buffer(int depth)
        {
            var obj = CreateModel(depth);

            using (var bw = BufferWriter<byte>.Create())
            {
                Serializer.Serialize(bw, obj);

                using var buffer = bw.Flush();
                var clone = Serializer.Deserialize<A>(buffer.Value);
                Assert.NotSame(obj, clone);
                obj = clone;
            }

            var level = 0;
            while (obj != null)
            {
                Assert.Equal(depth - level, obj.Level);
                level++;
                obj = obj.Inner;
            }
            Assert.Equal(depth, level);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(5)]
        public void RoundTripBasicModel_Stream(int depth)
        {
            var obj = CreateModel(depth);

            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, obj);
                ms.Position = 0;

                var clone = Serializer.Deserialize<A>(ms);
                Assert.NotSame(obj, clone);
                obj = clone;
            }

            var level = 0;
            while (obj != null)
            {
                Assert.Equal(depth - level, obj.Level);
                level++;
                obj = obj.Inner;
            }
            Assert.Equal(depth, level);
        }
    }
}

#endif