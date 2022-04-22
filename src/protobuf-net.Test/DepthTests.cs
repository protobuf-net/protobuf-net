using ProtoBuf.Meta;
using System;
using System.IO;
using Xunit;

namespace ProtoBuf.Test
{
    public class DepthTests
    {
        [ProtoContract]
        public class RecursiveModel
        {
            [ProtoMember(1)]
            public RecursiveModel Tail { get; set; }

            public int TotalDepth()
            {
                var depth = 1;
                var tail = Tail;
                while (tail is not null)
                {
                    depth++;
                    tail = tail.Tail;
                }
                return depth;
            }
        }

        [Theory]
        // valid scenarios
        [InlineData(5, 512, true)]
        [InlineData(510, 512, true)]
        [InlineData(511, 512, true)]
        [InlineData(512, 512, true)]
        // invalid scenarios
        [InlineData(2, 1, false)] // for debugging
        [InlineData(513, 512, false)]
        [InlineData(514, 512, false)]
        // now with increased capacity
        [InlineData(513, 520, true)]
        [InlineData(514, 520, true)]
        public void TestSerialize(int depth, int maxDepth, bool success)
        {
            var oldDepth = RuntimeTypeModel.Default.MaxDepth;
            try
            {
                var obj = new RecursiveModel();
                for (int i = 1; i < depth; i++)
                {
                    obj = new RecursiveModel { Tail = obj };
                }
                Assert.Equal(depth, obj.TotalDepth());
                var ms = new MemoryStream();
                RuntimeTypeModel.Default.MaxDepth = maxDepth;

                if (success)
                {
                    Serializer.Serialize(ms, obj);
                    ms.Position = 0;
                    obj = Serializer.Deserialize<RecursiveModel>(ms);
                    Assert.Equal(depth, obj.TotalDepth());
                }
                else
                {
                    var ex = Assert.Throws<InvalidOperationException>(() => Serializer.Serialize(ms, obj));
                    Assert.Equal($"Maximum model depth exceeded (see TypeModel.MaxDepth): {maxDepth}", ex.Message);
                }
            }
            finally
            {
                RuntimeTypeModel.Default.MaxDepth = oldDepth;
            }
        }

        [Theory]
        // valid scenarios
        [InlineData(5, 512, true)]
        [InlineData(510, 512, true)]
        [InlineData(511, 512, true)]
        [InlineData(512, 512, true)]
        // invalid scenarios
        [InlineData(513, 512, false)]
        [InlineData(514, 512, false)]
        // now with increased capacity
        [InlineData(513, 520, true)]
        [InlineData(514, 520, true)]
        public void TestDeserialize(int depth, int maxDepth, bool success)
        {
            var oldDepth = RuntimeTypeModel.Default.MaxDepth;
            try
            {
                var obj = new RecursiveModel();
                for (int i = 1; i < depth; i++)
                {
                    obj = new RecursiveModel { Tail = obj };
                }
                Assert.Equal(depth, obj.TotalDepth());
                var ms = new MemoryStream();
                RuntimeTypeModel.Default.MaxDepth = depth + 10;
                Serializer.Serialize(ms, obj);
                ms.Position = 0;
                RuntimeTypeModel.Default.MaxDepth = maxDepth;
                if (success)
                {
                    obj = Serializer.Deserialize<RecursiveModel>(ms);
                    Assert.Equal(depth, obj.TotalDepth());
                }
                else
                {
                    var ex = Assert.Throws<InvalidOperationException>(() => Serializer.Deserialize<RecursiveModel>(ms));
                    Assert.Equal($"Maximum model depth exceeded (see TypeModel.MaxDepth): {maxDepth}", ex.Message);
                }
            }
            finally
            {
                RuntimeTypeModel.Default.MaxDepth = oldDepth;
            }
        }
    }
}
