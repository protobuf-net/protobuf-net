using ProtoBuf.Meta;
using ProtoBuf.unittest;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace ProtoBuf.Test
{
    public class NullCollections
    {
        public ITestOutputHelper Log { get; }

        public NullCollections(ITestOutputHelper log)
            => Log = log;

        [Theory]
        [InlineData(-1, "")]
        [InlineData(0, "22-00")] // guess, something like
        [InlineData(1, "22-04-0A-02-08-00")] // guess, something like
        [InlineData(10)]
        public void TestWithNullWrappedCollection(int count, string? hex = null) => Test<WithNullWrappedCollection>(count, true, hex);

        [Theory]
        [InlineData(-1, "")]
        [InlineData(0, "")]
        [InlineData(1, "22-02-08-00")]
        [InlineData(10, "22-02-08-00-22-02-08-01-22-02-08-02-22-02-08-03-22-02-08-04-22-02-08-05-22-02-08-06-22-02-08-07-22-02-08-08-22-02-08-09")]
        public void TestVanilla(int count, string? hex = null) => Test<Vanilla>(count, false, hex);

        private void Test<T>(int count, bool withAttrib, string? expectedHex) where T : class, ITestScenario, new()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add<T>();
            Test<T>(model, count, withAttrib, expectedHex);
            model.CompileInPlace();
            Test<T>(model, count, withAttrib, expectedHex);
            Test<T>(PEVerify.CompileAndVerify(model), count, withAttrib, expectedHex);
        }

        private void Test<T>(TypeModel model, int count, bool withAttrib, string? expectedHex) where T : class, ITestScenario, new()
        {
            T obj = new();
            if (count >= 0)
            {
                var list = obj.Foos = new List<Foo>();
                for (int i = 0; i < count; i++)
                {
                    list.Add(new(i));
                }
            }
            using var ms = new MemoryStream();
            model.Serialize<T>(ms, obj);
            if (!ms.TryGetBuffer(out var buffer)) buffer = new(ms.ToArray());
            var hex = BitConverter.ToString(buffer.Array!, buffer.Offset, buffer.Count);
            Log.WriteLine(hex);

            if (expectedHex is not null) Assert.Equal(expectedHex, hex);

            ms.Position = 0;
            var clone = model.Deserialize<T>(ms);

            if (count < 0 || (count == 0 && !withAttrib))
            {
                Assert.Null(clone.Foos);
            }
            else
            {
                var list = clone.Foos;
                Assert.NotNull(list);
                Assert.Equal(count, list.Count);
                for (int i = 0; i < count; i++)
                {
                    Assert.Equal(i, list[i].Id);
                }
            }
        }




        [ProtoContract]
        public class Vanilla : ITestScenario
        {
            [ProtoMember(4)]
            public List<Foo>? Foos { get; set; }
        }

        [ProtoContract]
        public class WithNullWrappedCollection : ITestScenario
        {
            [ProtoMember(4), NullWrappedCollection]
            public List<Foo>? Foos { get; set; }
        }

        public interface ITestScenario
        {
            List<Foo>? Foos { get; set; }
        }

        public readonly struct Foo
        {
            public Foo(int id) => Id = id; 
            public int Id { get; }
        }
    }
}
