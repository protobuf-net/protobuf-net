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

namespace ProtoBuf
{
    public class CustomScalarAllocator
    {

        [Fact]
        public void CustomScalarIL()
        {
            var model = RuntimeTypeModel.Create();
            model.Add<HazRegularString>();
            model.Add<HazBlobish>();
            model.CompileAndVerify(deleteOnSuccess: false);
        }

        [Fact]
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
            var ctx = new MyCustomContext(arena);
            ctx.Reset();
            var blobish = model.Deserialize<HazBlobish>(ms, context: ctx);
            Assert.True(blobish.Value.Payload.ToArray().SequenceEqual(expected));
            Assert.Equal(20, ctx.TotalAllocated);

            // check we can write the right bytes
            ms.Position = 0;
            ms.SetLength(0); // wipe
            model.Serialize<HazBlobish>(ms, blobish, context: ctx);
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

        // this is our custom scalar; it has a custom serializer that
        // grabs chunks of memory from a custom allocator
        [ProtoContract(Serializer = typeof(Blobish.Serializer))]
        public readonly struct Blobish
        {
            public ReadOnlySequence<byte> Payload { get; }
            public Blobish(ReadOnlySequence<byte> payload) => Payload = payload;

            public static Blobish Empty => default;
            public bool IsEmpty => Payload.IsEmpty;

            // here's our custom serializer
            public sealed class Serializer : ISerializer<Blobish>
            {
                SerializerFeatures ISerializer<Blobish>.Features =>
                    SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeString;

                static readonly Func<ISerializationContext, int, ReadOnlySequence<byte>>
                    CustomAllocator = (ctx, length) =>
                    {
                        var allocator = ctx.Context as IBlobAllocator;
                        return allocator == null ? new ReadOnlySequence<byte>(new byte[length])
                            : allocator.Allocate(length);
                    };

                Blobish ISerializer<Blobish>.Read(ref ProtoReader.State state, Blobish value)
                    => new Blobish(state.AppendBytes(value.Payload, CustomAllocator));

                void ISerializer<Blobish>.Write(ref ProtoWriter.State state, Blobish value)
                    => state.WriteBytes(value.Payload);
            }
        }

        // just describes "something that can allocate", for use from
        // our custom serializer
        interface IBlobAllocator
        {
            ReadOnlySequence<byte> Allocate(int length);
        }

        // this is our custom context; we're using it to
        // make the 'arena' available to custom serializer,
        // by implementing the abstraction above
        class MyCustomContext : SerializationContext, IBlobAllocator
        {
            private readonly Arena<byte> _arena;
            public MyCustomContext(Arena<byte> arena)
                => _arena = arena;

            public long TotalAllocated => Volatile.Read(ref _totalAllocated);
            public void Reset() => Volatile.Write(ref _totalAllocated, 0);
            private static long _totalAllocated = 0;
            ReadOnlySequence<byte> IBlobAllocator.Allocate(int length)
            {
                Interlocked.Add(ref _totalAllocated, length);
                return _arena.Allocate(length);
            }
        }


    }
}
