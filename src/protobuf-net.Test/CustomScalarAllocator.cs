using Pipelines.Sockets.Unofficial.Arenas;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using ProtoBuf.unittest;
using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test
{
    public class CustomScalarAllocator
    {

        [Fact]
        public void CustomScalarIL()
        {
            var model = RuntimeTypeModel.Create();
            model.Add<HazRegularString>();
            //model.Add<HazBlobish>();
            model.Add<HazMemoryBlobish>();
            model.CompileAndVerify();
        }

        private readonly ITestOutputHelper _log;
        public CustomScalarAllocator(ITestOutputHelper log)
            => _log = log;

        [Fact]
        public void CustomScalarSchema()
        {
            var model = RuntimeTypeModel.Create();
            model.Add<HazRegularString>();
            model.Add<HazBlobish>();
            model.Add<HazMemoryBlobish>();
            var schema = model.GetSchema(null, ProtoSyntax.Default);
            _log?.WriteLine(schema);
            Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Test;

message HazBlobish {
   bytes Value = 1;
}
message HazMemoryBlobish {
   bytes Value = 1;
}
message HazRegularString {
   string Value = 1;
}
", schema, ignoreLineEndingDifferences: true);
        }

        [Fact(Skip = "ROS not implemented cleanly yet")]
        public void CustomBlobLikeReader()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;

            // runtime
            TestCustomBlobLikeReader(model);

            // compiled-in-place
            model.CompileInPlace();
            TestCustomBlobLikeReader(model);

            // in-memory
            TestCustomBlobLikeReader(model.Compile());

            // dll
            TestCustomBlobLikeReader(model.CompileAndVerify());
        }

        private static void TestCustomBlobLikeReader(TypeModel model)
        {
            using var ms = new MemoryStream();
            var s = "a ☁ ☂ bc ☃ ☄";
            model.Serialize(ms, new HazRegularString { Value = s });
            var expected = Encoding.UTF8.GetBytes(s);
            Assert.Equal(20, expected.Length);
            ms.Position = 0;

            using var arena = new Arena<byte>();
            var ctx = new MyState(arena);
            ctx.Reset();
            var blobish = model.Deserialize<HazBlobish>(ms, userState: ctx);
            Assert.True(blobish.Value.Payload.ToArray().SequenceEqual(expected));
            Assert.Equal(20, ctx.TotalAllocatedSequence);
            Assert.Equal(0, ctx.TotalAllocatedMemory);

            // check we can write the right bytes
            ms.Position = 0;
            ms.SetLength(0); // wipe
            model.Serialize<HazBlobish>(ms, blobish, userState: ctx);
            // expect: "field 1, length prefixed, 20 bytes" + the UTF8 payload
            Assert.Equal("0A-14-" + BitConverter.ToString(expected), BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length));
        }

        [Fact]
        public void CustomMemoryBlobLikeReader()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;

            // runtime
            TestCustomMemoryBlobLikeReader(model);

            // compiled-in-place
            model.CompileInPlace();
            TestCustomMemoryBlobLikeReader(model);

            // in-memory
            TestCustomMemoryBlobLikeReader(model.Compile());

            // dll
            TestCustomMemoryBlobLikeReader(model.CompileAndVerify());
        }

        private static void TestCustomMemoryBlobLikeReader(TypeModel model)
        {
            using var ms = new MemoryStream();
            var s = "a ☁ ☂ bc ☃ ☄";
            model.Serialize(ms, new HazRegularString { Value = s });
            var expected = Encoding.UTF8.GetBytes(s);
            Assert.Equal(20, expected.Length);
            ms.Position = 0;

            using var arena = new Arena<byte>();
            var ctx = new MyState(arena);
            ctx.Reset();
            var blobish = model.Deserialize<HazMemoryBlobish>(ms, userState: ctx);
            Assert.True(blobish.Value.Payload.ToArray().SequenceEqual(expected));
            Assert.Equal(0, ctx.TotalAllocatedSequence);
            Assert.Equal(20, ctx.TotalAllocatedMemory);

            // check we can write the right bytes
            ms.Position = 0;
            ms.SetLength(0); // wipe
            model.Serialize<HazMemoryBlobish>(ms, blobish, userState: ctx);
            // expect: "field 1, length prefixed, 20 bytes" + the UTF8 payload
            Assert.Equal("0A-14-" + BitConverter.ToString(expected), BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length));
        }

        // this is a byte-equivalent type used to test
        // serialization, etc
        [ProtoContract]
        public class HazRegularString
        {
            [ProtoMember(1)]
            public string Value { get; set; }
        }

        // this is a type that *uses* our custm scalar
        [ProtoContract]
        public class HazBlobish
        {
            [ProtoMember(1)]
            public Blobish Value { get; set; }
        }

        // this is a type that *uses* our custm scalar
        [ProtoContract]
        public class HazMemoryBlobish
        {
            [ProtoMember(1)]
            public MemoryBlobish Value { get; set; }
        }

        // this is our custom scalar; it has a custom serializer that
        // grabs chunks of memory from a custom allocator
        [ProtoContract(Serializer = typeof(CustomSerializer), Name = "bytes")]
        public readonly struct Blobish
        {
            public ReadOnlySequence<byte> Payload { get; }
            public Blobish(ReadOnlySequence<byte> payload) => Payload = payload;

            public static Blobish Empty => default;
            public bool IsEmpty => Payload.IsEmpty;
        }

        // this is our custom scalar; it has a custom serializer that
        // grabs chunks of memory from a custom allocator
        [ProtoContract(Serializer = typeof(CustomSerializer), Name = "bytes")]
        public readonly struct MemoryBlobish
        {
            public Memory<byte> Payload { get; }
            public MemoryBlobish(Memory<byte> payload) => Payload = payload;

            public static MemoryBlobish Empty => default;
            public bool IsEmpty => Payload.IsEmpty;
        }

        // here's our custom serializer
        public sealed class CustomSerializer
            : ISerializer<MemoryBlobish>, IMemoryConverter<MemoryBlobish, byte>
            // : ISerializer<Blobish>
        {
            SerializerFeatures ISerializer<MemoryBlobish>.Features =>
                SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeString;

            Memory<byte> IMemoryConverter<MemoryBlobish, byte>.Expand(ISerializationContext context, ref MemoryBlobish value, int additionalCapacity)
            {
                var oldLength = value.Payload.Length;
                var newLength = oldLength + additionalCapacity;
                var newData = context.UserState is IBlobAllocator allocator ? allocator.AllocateMemory(newLength).Slice(0, newLength) : new byte[newLength];
                value.Payload.CopyTo(newData);
                value = new MemoryBlobish(newData);
                return newData.Slice(oldLength);
            }

            int IMemoryConverter<MemoryBlobish, byte>.GetLength(in MemoryBlobish value) => value.Payload.Length;

            Memory<byte> IMemoryConverter<MemoryBlobish, byte>.GetMemory(in MemoryBlobish value) => value.Payload;

            MemoryBlobish IMemoryConverter<MemoryBlobish, byte>.NonNull(in MemoryBlobish value) => value;

            MemoryBlobish ISerializer<MemoryBlobish>.Read(ref ProtoReader.State state, MemoryBlobish value)
                => state.AppendBytes(value, this);

            void ISerializer<MemoryBlobish>.Write(ref ProtoWriter.State state, MemoryBlobish value)
                => state.WriteBytes(value, this);


            //SerializerFeatures ISerializer<Blobish>.Features =>
            //    SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeString;

            //static readonly Func<ISerializationContext, int, ReadOnlySequence<byte>>
            //    SequenceAllocator = (ctx, length) =>
            //    {
            //        var allocator = ctx.Context as IBlobAllocator;
            //        return allocator == null ? new ReadOnlySequence<byte>(new byte[length])
            //            : allocator.AllocateSequence(length);
            //    };
            //static readonly Func<ISerializationContext, int, Memory<byte>>
            //    MemoryAllocator = (ctx, length) =>
            //    {
            //        var allocator = ctx.Context as IBlobAllocator;
            //        return allocator == null ? new Memory<byte>(new byte[length])
            //            : allocator.AllocateMemory(length);
            //    };

            //Blobish ISerializer<Blobish>.Read(ref ProtoReader.State state, Blobish value)
            //    => new Blobish(state.AppendBytes(value.Payload, SequenceAllocator));

            //void ISerializer<Blobish>.Write(ref ProtoWriter.State state, Blobish value)
            //    => state.WriteBytes(value.Payload);


        }

        // just describes "something that can allocate", for use from
        // our custom serializer
        interface IBlobAllocator
        {
            ReadOnlySequence<byte> AllocateSequence(int length);
            Memory<byte> AllocateMemory(int length);
        }

        // this is our custom context; we're using it to
        // make the 'arena' available to custom serializer,
        // by implementing the abstraction above
        class MyState : IBlobAllocator
        {
            private readonly Arena<byte> _arena;
            public MyState(Arena<byte> arena)
                => _arena = arena;

            public long TotalAllocatedSequence => Volatile.Read(ref _totalAllocatedSequence);
            public long TotalAllocatedMemory => Volatile.Read(ref _totalAllocatedMemory);
            public void Reset()
            {
                Volatile.Write(ref _totalAllocatedSequence, 0);
                Volatile.Write(ref _totalAllocatedMemory, 0);
            }
            private static long _totalAllocatedSequence = 0, _totalAllocatedMemory = 0;
            ReadOnlySequence<byte> IBlobAllocator.AllocateSequence(int length)
            {
                Interlocked.Add(ref _totalAllocatedSequence, length);
                return _arena.Allocate(length);
            }

            Memory<byte> IBlobAllocator.AllocateMemory(int length)
            {
                Interlocked.Add(ref _totalAllocatedMemory, length);
                return ArrayPool<byte>.Shared.Rent(length); // note this is silly unless we have a mechanism to return them!
            }
        }


    }
}
