using ProtoBuf.Meta;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace ProtoBuf.Tests
{
    public class CustomSerializers
    {
        [Fact]
        public void ArraySegmentStuffJustWorks()
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
                RuntimeTypeModel.Default.Serialize(ms, obj, ctx);
                ms.Position = 0;
                var clone = (HazArraySegments)RuntimeTypeModel.Default.Deserialize(ms, null, typeof(HazArraySegments));

                Assert.Equal(20, clone.Payload.Count);
                Assert.Equal(20, clone.Payload.Offset);
                Assert.Equal(40, bufferPool.Allocated);
                string t = Encoding.UTF8.GetString(clone.Payload.Array, clone.Payload.Offset, clone.Payload.Count);
                Assert.Equal(s, t);
            }
        }
    }

    [ProtoContract]
    class HazArraySegments
    {
        [ProtoMember(1)]
        public int Id { get; set; }
        [ProtoMember(2)]
        public ArraySegment<byte> Payload { get; set; }
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
    public class ArraySegmentBytesSerializer : ISerializer<ArraySegment<byte>>
    {
        public WireType WireType => WireType.String;
        public void Read(ProtoReader reader, SerializationContext context, ref ArraySegment<byte> oldValue)
        {
            int len = reader.ReadLengthPrefix();
            if (len == 0)
            {
                return; // note Read===Append, so: fine
            }

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

        public void Write(ProtoWriter writer, SerializationContext context, ref ArraySegment<byte> value)
        {
            // note that this *includes* the length prefix
            ProtoWriter.WriteBytes(value.Array, value.Offset, value.Count, writer);
        }
    }
}
