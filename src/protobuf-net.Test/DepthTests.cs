using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
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

        [ProtoContract]
        public class RecursiveListModel
        {
            [ProtoMember(1)]
            public List<RecursiveListModel> Children { get; set; }
        }

        /// <summary>
        /// Recursion through a repeated member, rather than a single one, re-enters the repeated
        /// read once per level; reporting the depth limit from the bottom of that must unwind just
        /// as cleanly as decoding to the same depth does. Each case first decodes successfully to
        /// just under the limit on the same stack, so a failure means the error path is costlier
        /// than the decode, rather than that the stack was too small to begin with.
        /// </summary>
        [Theory]
        [InlineData(100)]
        [InlineData(200)]
        [InlineData(300)]
        public void TestDeserializeRepeatedPastMaxDepth(int maxDepth)
        {
            var model = RuntimeTypeModel.Create();
            model.MaxDepth = maxDepth;

            var nested = NestedChildren(maxDepth - 1); // the deepest the limit allows
            var obj = RunWithFixedStack(() => model.Deserialize<RecursiveListModel>(new MemoryStream(nested)));
            int depth = 0;
            while (obj.Children is { Count: > 0 } children)
            {
                obj = children[0];
                depth++;
            }
            Assert.Equal(maxDepth - 1, depth);

            var tooDeep = NestedChildren(maxDepth + 1);
            var ex = Assert.Throws<InvalidOperationException>(
                () => RunWithFixedStack(() => model.Deserialize<RecursiveListModel>(new MemoryStream(tooDeep))));
            Assert.Equal($"Maximum model depth exceeded (see TypeModel.MaxDepth): {maxDepth}", ex.Message);
        }

        /// <summary>Field 1, length-prefixed, nested <paramref name="levels"/> deep.</summary>
        private static byte[] NestedChildren(int levels)
        {
            byte[] payload = Array.Empty<byte>();
            for (int i = 0; i < levels; i++)
            {
                var next = new List<byte> { 0x0A };
                var len = (uint)payload.Length;
                while (len > 127)
                {
                    next.Add((byte)(len | 0x80));
                    len >>= 7;
                }
                next.Add((byte)len);
                next.AddRange(payload);
                payload = next.ToArray();
            }
            return payload;
        }

        /// <summary>
        /// Runs on a thread with a known stack size, so the outcome does not depend on how the
        /// test host sizes its threads; exceptions are marshalled back to the caller.
        /// </summary>
        private static T RunWithFixedStack<T>(Func<T> func)
        {
            T result = default;
            ExceptionDispatchInfo error = null;
            var thread = new Thread(() =>
            {
                try { result = func(); }
                catch (Exception ex) { error = ExceptionDispatchInfo.Capture(ex); }
            }, maxStackSize: 1024 * 1024);
            thread.Start();
            thread.Join();
            error?.Throw();
            return result;
        }
    }
}
