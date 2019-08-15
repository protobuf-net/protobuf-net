#if PLAT_SPANS

using Pipelines.Sockets.Unofficial.Buffers;
using ProtoBuf.Meta;
using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf
{
    public class BufferWriterTests
    {
        private readonly ITestOutputHelper Log;
        public BufferWriterTests(ITestOutputHelper log)
            => Log = log;

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
            public A Deserialize(ProtoReader reader, ref ProtoReader.State state, A value)
            {
                int fieldHeader;
                if (value == null) value = new A();
                while((fieldHeader = reader.ReadFieldHeader(ref state)) > 0)
                {
                    switch(fieldHeader)
                    {
                        case 1:
                            value.Level = reader.ReadInt32(ref state);
                            break;
                        case 2:
                            value.Inner = reader.ReadSubItem<A>(ref state, value.Inner, this);
                            break;
                        default:
                            reader.SkipField(ref state);
                            break;
                    }
                }
                return value;
            }

            public void Serialize(ProtoWriter writer, ref ProtoWriter.State state, A value)
            {
                Log?.WriteLine($"Writing field 1, value: {value.Level}");
                ProtoWriter.WriteFieldHeader(1, WireType.Variant, writer, ref state);
                ProtoWriter.WriteInt32(value.Level, writer, ref state);
                Log?.WriteLine($"Wrote field 1...");

                var obj = value.Inner;
                if (obj != null)
                {
                    Log?.WriteLine($"Writing field 2...");
                    ProtoWriter.WriteFieldHeader(2, WireType.String, writer, ref state);
                    ProtoWriter.WriteSubItem<A>(obj, writer, ref state, this);
                    Log?.WriteLine($"Wrote field 2...");
                }
            }
        }

        private static A CreateModel(int depth)
        {
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
            using (var bw = BufferWriter<byte>.Create())
            {
                var model = RuntimeTypeModel.Default;
                using (var writer = ProtoWriter.Create(out var state, bw.Writer, model, null))
                {
                    A obj = CreateModel(depth);
                    var ser = new ASerializer(Log);
                    ser.Serialize(writer, ref state, obj);
                    writer.Close(ref state);
                }
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(5)]
        public void WriteWithCustomSerializer_Stream(int depth)
        {
            using (var ms = new MemoryStream())
            {
                var model = RuntimeTypeModel.Default;
                using (var writer = ProtoWriter.Create(out var state, ms, model, null))
                {
                    A obj = CreateModel(depth);
                    var ser = new ASerializer(Log);
                    ser.Serialize(writer, ref state, obj);
                    writer.Close(ref state);
                }
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

                using (var buffer = bw.Flush())
                {
                    var clone = Serializer.Deserialize<A>(buffer.Value);
                    Assert.NotSame(obj, clone);
                    obj = clone;
                }
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
            int level = 0;
            A obj = null;
            while (level < depth)
            {
                obj = new A { Inner = obj, Level = ++level };
            }

            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, obj);
                ms.Position = 0;

                var clone = Serializer.Deserialize<A>(ms);
                Assert.NotSame(obj, clone);
                obj = clone;
            }

            level = 0;
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