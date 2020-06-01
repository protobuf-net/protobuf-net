#if PLAT_SPANS

using Pipelines.Sockets.Unofficial.Buffers;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;
using System.Buffers;
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
            var state = ProtoWriter.State.Create(ms, RuntimeTypeModel.Default, null);
            try
            {
                ManualWriter(ref state);
            }
            finally
            {
                state.Dispose();
            }
            var hex = BitConverter.ToString(ms.ToArray());
            Assert.Equal("08-2A-12-10-61-62-63-64-65-66-67-68-69-6A-6B-6C-6D-6E-6F-70-1B-1C-22-00", hex);
        }

        [Fact]
        public void ManualWriter_Null()
        {
            var state = ProtoWriter.CreateNull(RuntimeTypeModel.Default, null, -1);
            try
            {
                ManualWriter(ref state);
            }
            finally
            {
                state.Dispose();
            }
        }

        [Fact]
        public void ManualWriter_Buffer()
        {
            using var bw = BufferWriter<byte>.Create();
            var state = ProtoWriter.State.Create(bw, RuntimeTypeModel.Default, null);
            try
            {
                ManualWriter(ref state);
            }
            finally
            {
                state.Dispose();
            }
            using var segment = bw.Flush();
            var hex = BitConverter.ToString(segment.Value.ToArray());
            Assert.Equal("08-2A-12-10-61-62-63-64-65-66-67-68-69-6A-6B-6C-6D-6E-6F-70-1B-1C-22-00", hex);
        }

        class Foo
        {
            private Foo() { }

            public static ISerializer<Foo> Serializer => FooSerializer.Instance;
            sealed class FooSerializer : ISerializer<Foo>
            {
                SerializerFeatures ISerializer<Foo>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
                public static FooSerializer Instance { get; } = new FooSerializer();
                private FooSerializer() { }
                public Foo Read(ref ProtoReader.State state, Foo value) => value;

                public void Write(ref ProtoWriter.State state, Foo value) { }
            }
        }

        private void ManualWriter(ref ProtoWriter.State state)
        {
            try
            {
                state.WriteFieldHeader(1, WireType.Varint);
                Assert.Equal(1, state.GetPosition());
                state.WriteInt32(42);
                Assert.Equal(2, state.GetPosition());

                state.WriteFieldHeader(2, WireType.String);
                Assert.Equal(3, state.GetPosition());
                state.WriteString("abcdefghijklmnop");
                Assert.Equal(20, state.GetPosition());

                state.WriteFieldHeader(3, WireType.StartGroup);
                Assert.Equal(21, state.GetPosition());
                state.WriteMessage<Foo>(default, null, Foo.Serializer);
                Assert.Equal(22, state.GetPosition());

                state.WriteFieldHeader(4, WireType.String);
                Assert.Equal(23, state.GetPosition());
                state.WriteMessage<Foo>(default, null, Foo.Serializer);
                Assert.Equal(24, state.GetPosition());

                state.Close();
            }
            catch
            {
                state.Abandon();
                throw;
            }

            Assert.Equal(24, state.GetPosition());
        }

        [ProtoContract]
        public class A
        {
            [ProtoMember(1)]
            public int Level { get; set; }

            [ProtoMember(2)]
            public A Inner { get; set; }
        }

        class ASerializer : ISerializer<A>
        {
            SerializerFeatures ISerializer<A>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
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
                            value.Inner = state.ReadMessage<A>(default, value.Inner, this);
                            break;
                        default:
                            state.SkipField();
                            break;
                    }
                }
                return value;
            }

            public void Write(ref ProtoWriter.State state, A value)
            {
#pragma warning disable CS0618
                Log?.WriteLine($"Writing to {state.GetWriter().GetType().Name}");
                Log?.WriteLine($"Writing field 1, value: {value.Level}; pos: {state.GetPosition()}");
                state.WriteFieldHeader(1, WireType.Varint);
                state.WriteInt32(value.Level);
                Log?.WriteLine($"Wrote field 1... pos: {state.GetPosition()}");

                var obj = value.Inner;
                if (obj != null)
                {
                    Log?.WriteLine($"Writing field 2...; pos: {state.GetPosition()}");
                    state.WriteFieldHeader(2, WireType.String);
                    state.WriteMessage<A>(default, obj, this);
                    Log?.WriteLine($"Wrote field 2...; pos: {state.GetPosition()}");
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
            var state = ProtoWriter.State.Create(bw.Writer, model, null);
            try
            {
                A obj = CreateModel(depth);
                var ser = new ASerializer(Log);
                ser.Write(ref state, obj);
                state.Close();
            }
            finally
            {
                state.Dispose();
            }
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
            var state = ProtoWriter.State.Create(ms, model, null);
            try
            {
                A obj = CreateModel(depth);
                var ser = new ASerializer(Log);
                ser.Write(ref state, obj);
                state.Close();
            }
            finally
            {
                state.Dispose();
            }
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