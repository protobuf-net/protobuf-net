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
    using NUnit.Framework;
    using System.Diagnostics;
using ProtoBuf.Meta;
    using System.IO;
    [TestFixture]
    public class Issue103
    {
        static RuntimeTypeModel CreateModel()
        {
            var model = TypeModel.Create();
            model.Add(typeof(TypeA), true);
            model.Add(typeof(TypeB), true);
            model.Add(typeof(ContainedType), true);
            return model;
        }
        [Test]
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
                ContainedType inner = new ContainedType();
                inner.param1 = "Item " + i.ToString();
                inner.param2 = i;
                inner.param3 = i % 2 == 0;
                inner.param4 = i % 3 == 0;
                typeB.containedType.Add(inner);
            }
            var model = CreateModel();
            RunTestIssue103(5000, typeA, typeB, model, "Runtime");
            model.CompileInPlace();
            RunTestIssue103(50000, typeA, typeB, model, "CompileInPlace");
            RunTestIssue103(50000, typeA, typeB, model.Compile(), "Compile");
            

        }

        private static void RunTestIssue103(int loop, TypeA typeA, TypeB typeB, TypeModel model, string caption)
        {
            // for JIT and preallocation
            MemoryStream ms = new MemoryStream();
            ms.SetLength(0);
            model.Serialize(ms, typeA);
            ms.Position = 0;
            model.Deserialize(ms, null, typeof(TypeA));

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
                model.Deserialize(ms, null, typeof(TypeA));
            }
            typeADeser.Stop();

            ms.SetLength(0);
            model.Serialize(ms, typeB);
            ms.Position = 0;
            TypeB clone = (TypeB)model.Deserialize(ms, null, typeof(TypeB));
            Assert.AreEqual(typeB.containedType.Count, clone.containedType.Count);

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
                model.Deserialize(ms, null, typeof(TypeB));
            }
            typeBDeser.Stop();

            Console.WriteLine(caption + " A/ser\t" + (typeASer.ElapsedMilliseconds * 1000 / loop) + " μs/item");
            Console.WriteLine(caption + " A/deser\t" + (typeADeser.ElapsedMilliseconds * 1000 / loop) + " μs/item");
            Console.WriteLine(caption + " B/ser\t" + (typeBSer.ElapsedMilliseconds * 1000 / loop) + " μs/item");
            Console.WriteLine(caption + " B/deser\t" + (typeBDeser.ElapsedMilliseconds * 1000 / loop) + " μs/item");
        }
    }
}
