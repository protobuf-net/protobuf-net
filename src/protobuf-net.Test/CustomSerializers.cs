using ProtoBuf.Meta;
using ProtoBuf.unittest;
using System;
using System.IO;
using System.Text;
using Xunit;

// note: this can be used to import serializers from external libs
// [assembly: ProtoRegisterAll("protobuf-net.Test")]

namespace ProtoBuf.Tests
{
    public class CustomSerializers
    {
        [Fact]
        public void AssemblyProbeIdentifiesSerializers()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(HazArraySegments), true);
            var serializer = model[typeof(ArraySegment<byte>)].CustomSerializer;
            Assert.NotNull(serializer);
            Assert.Equal(typeof(ArraySegmentBytesSerializer), serializer.Type);
            Assert.False(serializer.IsMessage);

            serializer = model[typeof(TypeWithCustomSerializer)].CustomSerializer;
            Assert.NotNull(serializer);
            Assert.Equal(typeof(HandWrittenSerializer), serializer.Type);
            Assert.True(serializer.IsMessage);

        }
        [Fact]
        public void SerializeNakedArraySegment()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(HazArraySegments), true);
            SerializeNakedArraySegment(model, "Runtime");
            model.CompileInPlace();
            SerializeNakedArraySegment(model, "CompileInPlace");
            SerializeNakedArraySegment(model.Compile(), "Compile");

            model.Compile("HazArraySegments", "HazArraySegments.dll");
            PEVerify.Verify("HazArraySegments.dll");
        }
        private void SerializeNakedArraySegment(TypeModel model, string mode)
        {
            var obj = new ArraySegment<byte>(new byte[] { 0, 1, 2, 3, 4 });
            using (var ms = new MemoryStream())
            {
                model.Serialize(ms, obj);
                var hex = BitConverter.ToString(ms.ToArray());
                Assert.Equal("0A-05-00-01-02-03-04", hex);

                ms.Position = 0;
                var clone = (ArraySegment<byte>)model.Deserialize(ms, null, typeof(ArraySegment<byte>));
                Assert.Equal("00-01-02-03-04", BitConverter.ToString(clone.Array, clone.Offset, clone.Count));

            }
        }
        [Fact]
        public void ArraySegmentStuffJustWorks()
        {
            var model = RuntimeTypeModel.Create();
            ArraySegmentStuffJustWorks(model, "Runtime");
            model.CompileInPlace();
            ArraySegmentStuffJustWorks(model, "CompileInPlace");
            ArraySegmentStuffJustWorks(model.Compile(), "Compile");

            model.Compile("ArraySegmentByte", "ArraySegmentByte.dll");
            PEVerify.Verify("ArraySegmentByte.dll");
        }
        private void ArraySegmentStuffJustWorks(TypeModel model, string mode)
        {
            var bufferPool = new ArraySegmentBufferPool(1024 << 3);

            string s = "these are some words";
            var obj = new HazArraySegments { Id = 123, Payload = bufferPool.Allocate(Encoding.UTF8.GetByteCount(s)) };
            Encoding.UTF8.GetBytes(s, 0, s.Length, obj.Payload.Array, obj.Payload.Offset);
            Assert.Equal(20, obj.Payload.Count);
            Assert.Equal(0, obj.Payload.Offset);
            Assert.Equal(20, bufferPool.Allocated);
            using (var ms = new MemoryStream())
            {
                var ctx = new SerializationContext();
                ctx.SetAllocator(bufferPool);
                model.Serialize(ms, obj, ctx);
                ms.Position = 0;
                var clone = (HazArraySegments)model.Deserialize(ms, null, typeof(HazArraySegments), ctx);

                Assert.Equal(20, clone.Payload.Count);
                Assert.Equal(20, clone.Payload.Offset);
                Assert.Equal(40, bufferPool.Allocated);
                string t = Encoding.UTF8.GetString(clone.Payload.Array, clone.Payload.Offset, clone.Payload.Count);
                Assert.Equal(s, t);
            }
        }

        [Fact]
        public void CustomSerializerWorksTheSame()
        {
            string expected;
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, new TypeWithoutCustomSerializer { X = 123, Y = "abc" });
                expected = BitConverter.ToString(ms.ToArray());

            }
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, new TypeWithCustomSerializer { X = 123, Y = "abc" });
                var actual = BitConverter.ToString(ms.ToArray());
                Assert.Equal(expected, actual);
                ms.Position = 0;
                var clone = Serializer.Deserialize<TypeWithCustomSerializer>(ms);
                Assert.Equal(123, clone.X);
                Assert.Equal("abc", clone.Y);
            }
        }
    }

    [ProtoContract]
    public class HazArraySegments
    {
        [ProtoMember(1)]
        public int Id { get; set; }
        [ProtoMember(2)]
        public ArraySegment<byte> Payload { get; set; }
    }
    public class TypeWithCustomSerializer
    {
        public int X { get; set; }
        public string Y { get; set; }
    }
    [ProtoContract]
    public class TypeWithoutCustomSerializer
    {
        [ProtoMember(1)]
        public int X { get; set; }
        [ProtoMember(2)]
        public string Y { get; set; }
    }
    [ProtoSerializer]
    public class HandWrittenSerializer : ISerializer<TypeWithCustomSerializer>
    {
        public void Read(ProtoReader reader, ref TypeWithCustomSerializer value)
        {
            int field;
            int x = value?.X ?? 0;
            string y = value?.Y;
            while ((field = reader.ReadFieldHeader()) > 0)
            {
                switch(field)
                {
                    case 1: x = reader.ReadInt32(); break;
                    case 2: y = reader.ReadString(); break;
                }
            }
            if (value == null) value = new TypeWithCustomSerializer();
            value.X = x;
            value.Y = y;
        }

        public void Write(ProtoWriter writer, ref TypeWithCustomSerializer value)
        {
            ProtoWriter.WriteFieldHeader(1, WireType.Variant, writer);
            ProtoWriter.WriteInt32(value.X, writer);
            ProtoWriter.WriteFieldHeader(2, WireType.String, writer);
            ProtoWriter.WriteString(value.Y, writer);

        }
    }

    /// <summary>
    /// concept: the *allocator* is not auto-discovered and is tied to the serialization-context;
    /// as such, the backing store can either per per-instance on the allocator, or can be
    /// via the .Context property
    /// </summary>
    class ArraySegmentBufferPool : IAllocator<ArraySegment<byte>>
    {   // absurdly simplified
        public int Allocated => offset;

        byte[] page;
        int offset;
        public ArraySegmentBufferPool(int length)
        {
            page = new byte[length];
            offset = 0;
        }

        public ArraySegment<byte> Allocate(int length)
        {
            if ((offset + length) > page.Length) throw new OutOfMemoryException();
            var result = new ArraySegment<byte>(page, offset, length);
            offset += length;
            return result;
        }
        ArraySegment<byte> IAllocator<ArraySegment<byte>>.Allocate(SerializationContext context, int length)
            => Allocate(length);

        void IAllocator<ArraySegment<byte>>.Release(SerializationContext context, ArraySegment<byte> value) { }
    }

    /// <summary>
    /// concept: the *serializer* is discovered automagically, or via assistance attributes (in this case,
    /// it should probably be purely automatic registration, based on probing the assembly that defines HazArraySegments)
    /// </summary>
    [ProtoSerializer(false)]
    public class ArraySegmentBytesSerializer : ISerializer<ArraySegment<byte>>
    {
        public WireType WireType => WireType.String;
        public void Read(ProtoReader reader, ref ArraySegment<byte> oldValue)
        {
            int len = reader.ReadLengthPrefix();
            if (len == 0)
            {
                return; // note Read===Append, so: fine
            }

            var context = reader.Context;
            var allocator = context.GetAllocator<ArraySegment<byte>>();
            var newValue = default(ArraySegment<byte>);
            try
            {
                // if no allocator, use naked arrays
                newValue = allocator == null ? new ArraySegment<byte>(new byte[len]) : allocator.Allocate(context, len);

                bool adjacent = false;
                if (oldValue.Count != 0)
                {
                    adjacent = newValue.Array == oldValue.Array &&
                        newValue.Offset == (oldValue.Offset + oldValue.Count);

                    if (!adjacent)
                    {
                        // copy any pre-existing data into the new piece
                        Buffer.BlockCopy(oldValue.Array, oldValue.Offset, newValue.Array, newValue.Offset, oldValue.Count);
                    }
                }

                // read the new data
                reader.ReadBytes(newValue.Array, newValue.Offset + oldValue.Count, len, true);
                if (adjacent)
                {
                    // compact the two buffers into one
                    oldValue = new ArraySegment<byte>(oldValue.Array, oldValue.Offset, oldValue.Count + newValue.Count);
                }
                else
                {
                    // release the old data
                    if (oldValue.Count != 0)
                    {
                        allocator?.Release(context, oldValue);
                    }
                    // update the ref to signal the change
                    oldValue = newValue;
                }
            }
            catch
            {
                // failed while reading? release anything that we allocated
                if (newValue.Count != 0)
                {
                    allocator?.Release(context, newValue);
                }
                throw;
            }
        }

        public void Write(ProtoWriter writer, ref ArraySegment<byte> value)
        {
            // note that this *includes* the length prefix
            ProtoWriter.WriteBytes(value.Array, value.Offset, value.Count, writer);
        }
    }
}
