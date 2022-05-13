using ProtoBuf.Meta;
using System;
using System.IO;
using Xunit;

namespace ProtoBuf.Test
{
    public class DepthTests
    {
        private readonly RuntimeTypeModel _model = RuntimeTypeModel.Create();

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
            var oldDepth = _model.MaxDepth;
            try
            {
                var obj = new RecursiveModel();
                for (int i = 1; i < depth; i++)
                {
                    obj = new RecursiveModel { Tail = obj };
                }
                Assert.Equal(depth, obj.TotalDepth());
                var ms = new MemoryStream();
                _model.MaxDepth = maxDepth;

                if (success)
                {
                    _model.Serialize(ms, obj);
                    ms.Position = 0;
                    obj = _model.Deserialize<RecursiveModel>(ms);
                    Assert.Equal(depth, obj.TotalDepth());
                }
                else
                {
                    var ex = Assert.Throws<InvalidOperationException>(() => _model.Serialize(ms, obj));
                    Assert.Equal($"Maximum model depth exceeded (see TypeModel.MaxDepth): {maxDepth}", ex.Message);
                }
            }
            finally
            {
                _model.MaxDepth = oldDepth;
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
            var oldDepth = _model.MaxDepth;
            try
            {
                var obj = new RecursiveModel();
                for (int i = 1; i < depth; i++)
                {
                    obj = new RecursiveModel { Tail = obj };
                }
                Assert.Equal(depth, obj.TotalDepth());
                var ms = new MemoryStream();
                _model.MaxDepth = depth + 10;
                _model.Serialize(ms, obj);
                ms.Position = 0;
                _model.MaxDepth = maxDepth;
                if (success)
                {
                    obj = _model.Deserialize<RecursiveModel>(ms);
                    Assert.Equal(depth, obj.TotalDepth());
                }
                else
                {
                    var ex = Assert.Throws<InvalidOperationException>(() => _model.Deserialize<RecursiveModel>(ms));
                    Assert.Equal($"Maximum model depth exceeded (see TypeModel.MaxDepth): {maxDepth}", ex.Message);
                }
            }
            finally
            {
                _model.MaxDepth = oldDepth;
            }
        }
    }
}
