using System;

namespace ProtoBuf.unittest.Perf.Issue103Types
{
    partial class ContainedType
    {
        public override string ToString()
        {
            return this.param1;
        }
    }
}
namespace ProtoBuf.unittest.Perf
{
    using Issue103Types;
    using Xunit;
    using System.Diagnostics;
    using ProtoBuf.Meta;
    using System.IO;
    using Xunit.Abstractions;

    public class Issue103
    {
        private ITestOutputHelper Log { get; }
        public Issue103(ITestOutputHelper _log) => Log = _log;

        static RuntimeTypeModel CreateModel()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(TypeA), true);
            model.Add(typeof(TypeB), true);
            model.Add(typeof(ContainedType), true);
            return model;
        }
        [Fact]
        public void TestPerf()
        {
            TypeA typeA = new TypeA();
            for (int i = 0; i < 100; i++)
            {
                typeA.param1.Add("Item " + i.ToString());
                typeA.param2.Add(i);
                typeA.param3.Add(i % 2 == 0);
                typeA.param4.Add(i % 3 == 0);
            }
            TypeB typeB = new TypeB();
            for (int i = 0; i < 100; i++)
            {
                ContainedType inner = new ContainedType
                {
                    param1 = "Item " + i.ToString(),
                    param2 = i,
                    param3 = i % 2 == 0,
                    param4 = i % 3 == 0
                };
                typeB.containedType.Add(inner);
            }
            var model = CreateModel();
            RunTestIssue103(5000, typeA, typeB, model, "Runtime");
            model.CompileInPlace();
            RunTestIssue103(50000, typeA, typeB, model, "CompileInPlace");
            RunTestIssue103(50000, typeA, typeB, model.Compile(), "Compile");
        }

        private void RunTestIssue103(int loop, TypeA typeA, TypeB typeB, TypeModel model, string caption)
        {
            // for JIT and preallocation
            MemoryStream ms = new MemoryStream();
            ms.SetLength(0);
            model.Serialize(ms, typeA);
            ms.Position = 0;
#pragma warning disable CS0618
            model.Deserialize(ms, null, typeof(TypeA));
#pragma warning restore CS0618

            Stopwatch typeASer = Stopwatch.StartNew();
            for (int i = 0; i < loop; i++)
            {
                ms.SetLength(0);
                model.Serialize(ms, typeA);
            }
            typeASer.Stop();
            Stopwatch typeADeser = Stopwatch.StartNew();
            for (int i = 0; i < loop; i++)
            {
                ms.Position = 0;
#pragma warning disable CS0618
                model.Deserialize(ms, null, typeof(TypeA));
#pragma warning restore CS0618
            }
            typeADeser.Stop();

            ms.SetLength(0);
            model.Serialize(ms, typeB);
            ms.Position = 0;
#pragma warning disable CS0618
            TypeB clone = (TypeB)model.Deserialize(ms, null, typeof(TypeB));
#pragma warning restore CS0618
            Assert.Equal(typeB.containedType.Count, clone.containedType.Count);

            Stopwatch typeBSer = Stopwatch.StartNew();
            for (int i = 0; i < loop; i++)
            {
                ms.SetLength(0);
                model.Serialize(ms, typeB);
            }
            typeBSer.Stop();
            Stopwatch typeBDeser = Stopwatch.StartNew();
            for (int i = 0; i < loop; i++)
            {
                ms.Position = 0;
#pragma warning disable CS0618
                model.Deserialize(ms, null, typeof(TypeB));
#pragma warning restore CS0618
            }
            typeBDeser.Stop();

            Log.WriteLine(caption + " A/ser\t" + (typeASer.ElapsedMilliseconds * 1000 / loop) + " μs/item");
            Log.WriteLine(caption + " A/deser\t" + (typeADeser.ElapsedMilliseconds * 1000 / loop) + " μs/item");
            Log.WriteLine(caption + " B/ser\t" + (typeBSer.ElapsedMilliseconds * 1000 / loop) + " μs/item");
            Log.WriteLine(caption + " B/deser\t" + (typeBDeser.ElapsedMilliseconds * 1000 / loop) + " μs/item");
        }
    }
}