using ProtoBuf.Meta;
using ProtoBuf.unittest;
using System;
using System.Collections.Generic;
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
            // the backing buffer would be long lived and shared between contexts
            var pool = new SingleSlabBufferPool(1024, 8); // 8k
            
            for (int i = 0; i < 5; i++)
            {
                Assert.Equal(8 * 1024, pool.CountBytesAvailable());

                // the allocator also defines the lifetime of the pooled memory
                using (var allocator = new SingleSlabBufferPoolAllocator(pool))
                {
                    Assert.Equal(0, allocator.CountBytesHeld());

                    string s = "these are some words";
                    var obj = new HazArraySegments { Id = 123, Payload = allocator.Allocate(Encoding.UTF8.GetByteCount(s)) };
                    Encoding.UTF8.GetBytes(s, 0, s.Length, obj.Payload.Array, obj.Payload.Offset);
                    Assert.Equal(20, obj.Payload.Count);
                    Assert.Equal(0, obj.Payload.Offset);
                    Assert.Equal(20, allocator.Allocated);
                    using (var ms = new MemoryStream())
                    {
                        var ctx = new SerializationContext(); // we could re-use this ser-ctx between many seralize/deserialize
                                                              // calls that share a lifetime, if we want
                        ctx.SetAllocator(allocator);
                        model.Serialize(ms, obj, ctx);
                        ms.Position = 0;
                        var clone = (HazArraySegments)model.Deserialize(ms, null, typeof(HazArraySegments), ctx);

                        Assert.Equal(20, clone.Payload.Count);
                        Assert.Equal(20, clone.Payload.Offset);
                        Assert.Equal(40, allocator.Allocated);
                        string t = Encoding.UTF8.GetString(clone.Payload.Array, clone.Payload.Offset, clone.Payload.Count);
                        Assert.Equal(s, t);
                    }

                    Assert.Equal(1024, allocator.CountBytesHeld()); // allocator is taking a page
                    Assert.Equal(7168, pool.CountBytesAvailable());

                }
                Assert.Equal(8 * 1024, pool.CountBytesAvailable()); // page is back in the pool
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
    /// Allocates large pages of data and hands out pieces to consumers; this is an
    /// absurdly over-simplified example of a much more complex problem
    /// </summary>
    class SingleSlabBufferPool
    {
        Queue<ArraySegment<byte>> freePages; // a bad way of implementing it!
        public int CountBytesAvailable()
        {
            int free = 0;
            lock(freePages)
            {
                foreach (var item in freePages)
                    free += item.Count;
            }
            return free;
        }
        public SingleSlabBufferPool(int pageSize, int pages)
        {
            PageSize = pageSize;
            var slab = new byte[pageSize * pages]; // only one slab in this; yeah, it is a stupid example
            freePages = new Queue<ArraySegment<byte>>(pages);
            int offset = 0;
            for (int i = 0; i < pages; i++)
            {
                freePages.Enqueue(new ArraySegment<byte>(slab, offset, pageSize));
                offset += pageSize;
            }
        }
        public int PageSize { get; }
        public ArraySegment<byte> GetPage()
        {
            lock (freePages)
            {
                if (freePages.Count == 0) throw new OutOfMemoryException();
                return freePages.Dequeue();
            }
        }
        public void ReturnPage(ArraySegment<byte> page)
        {
            // we'll just blindly trust that this was ours, meh
            lock(freePages)
            {
                freePages.Enqueue(page);
            }
        }
    }
    /// <summary>
    /// concept: the *allocator* is not auto-discovered and is tied to the serialization-context;
    /// as such, the backing store can either per per-instance on the allocator, or can be
    /// via the .Context property
    /// </summary>
    class SingleSlabBufferPoolAllocator : IAllocator<ArraySegment<byte>>
    {   // absurdly simplified
        private SingleSlabBufferPool pool;
        int bytesLeftInCurrentPage;
        List<ArraySegment<byte>> pages;

        public SingleSlabBufferPoolAllocator(SingleSlabBufferPool pool)
        {
            this.pool = pool;
        }
        public int Allocated { get; private set; }
        public int CountBytesHeld()
        {
            var pages = this.pages;
            int held = 0;
            if(pages != null)
            {
                foreach(var page in pages) held += page.Count;
            }
            return held;
        }

        public ArraySegment<byte> Allocate(int length)
        {
            if (length <= 0) return default(ArraySegment<byte>);
            var pool = this.pool;
            if (pool == null) throw new ObjectDisposedException(nameof(SingleSlabBufferPoolAllocator));

            if (length > pool.PageSize) throw new OutOfMemoryException(); // TODO: consider options here
            if (bytesLeftInCurrentPage < length)
            {
                // going to need a new page
                if (pages == null) pages = new List<ArraySegment<byte>>();
                var newPage = pool.GetPage();
                bytesLeftInCurrentPage = newPage.Count;
                pages.Add(newPage);
            }

            var page = pages[pages.Count - 1];
            var offset = page.Count - bytesLeftInCurrentPage;
            var result = new ArraySegment<byte>(page.Array, offset, length);
            bytesLeftInCurrentPage -= length;
            Allocated += length;
            return result;
        }
        ArraySegment<byte> IAllocator<ArraySegment<byte>>.Allocate(SerializationContext context, int length)
            => Allocate(length);

        void IAllocator<ArraySegment<byte>>.Release(SerializationContext context, ArraySegment<byte> value)
        {
            // ignore; we're fine
        }
        void IDisposable.Dispose()
        {
            var pages = this.pages;
            var pool = this.pool;
            this.pool = null; // detach from the pool
            if (pages != null && pool != null)
            {
                foreach (var page in pages)
                {
                    pool.ReturnPage(page);
                }
            }
        }
    }

    /// <summary>
    /// concept: the *serializer* is discovered automagically, or via assistance attributes (in this case,
    /// it should probably be purely automatic registration, based on probing the assembly that defines HazArraySegments)
    /// </summary>
    [ProtoSerializer(false)]
    public class ArraySegmentBytesSerializer : ISerializer<ArraySegment<byte>>
    {
        public WireType WireType => WireType.String;
        public void Read(ProtoReader reader, ref ArraySegment<byte> value)
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
                // use the allocator if present, else use naked arrays
                newValue = allocator == null ? new ArraySegment<byte>(new byte[len + value.Count]) : allocator.Allocate(context, len + value.Count);

                if (value.Count != 0)
                {
                    // copy any pre-existing data into the new piece
                    Buffer.BlockCopy(value.Array, value.Offset, newValue.Array, newValue.Offset, value.Count);
                }

                // read the new data
                reader.ReadBytes(newValue.Array, newValue.Offset + value.Count, len, true);

                // release the old data
                if (value.Count != 0)
                {
                    allocator?.Release(context, value);
                }
                // update the ref to signal the change
                value = newValue;
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
