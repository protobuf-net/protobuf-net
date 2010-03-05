using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf.Meta;
using System.Diagnostics;

namespace ProtoBuf.unittest.Meta
{
    [TestFixture]
    public class Guids
    {
        public class Data {
            public Guid Value {get;set;}
            public char SomeTailData { get; set; }
        }
        static RuntimeTypeModel BuildModel() {
            var model = TypeModel.Create();
            model.Add(typeof(Data), false)
                .Add(1, "Value")
                .Add(2, "SomeTailData");
            return model;
        }
        [Test]
        public void TestRoundTripEmpty()
        {
            var model = BuildModel();
            Data data = new Data { Value = Guid.Empty, SomeTailData = 'D' };

            TestGuid(model, data);
            model.CompileInPlace();
            TestGuid(model, data);
            TestGuid(model.Compile(), data);
        }

        
        [Test]
        public void TestRoundTripObviousBytes()
        {
            var expected = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
            var model = BuildModel();
            Data data = new Data { Value = new Guid(expected), SomeTailData = 'F' };
            
            TestGuid(model, data); 
            model.CompileInPlace();
            TestGuid(model, data); 
            TestGuid(model.Compile(), data);
        }
        [Test]
        public void TestRoundTripRandom()
        {
            var model = BuildModel();
            TestGuids(model, 1000);
            model.CompileInPlace();
            TestGuids(model, 1000);
            TestGuids(model.Compile(), 1000);
        }
        static void TestGuids(TypeModel model, int count)
        {
            Random rand = new Random();
            byte[] buffer = new byte[16];
            Stopwatch watch = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                rand.NextBytes(buffer);
                Data data = new Data { Value = new Guid(buffer), SomeTailData = (char)rand.Next(0, ushort.MaxValue) };
                Data clone = (Data)model.DeepClone(data);
                Assert.AreEqual(data.Value, clone.Value);
                Assert.AreEqual(data.SomeTailData, clone.SomeTailData);
            }
            watch.Stop();
            Trace.WriteLine(watch.ElapsedMilliseconds);
        }
        static void TestGuid(TypeModel model, Data data)
        {
            Data clone = (Data)model.DeepClone(data);
            Assert.AreEqual(data.Value, clone.Value);
            Assert.AreEqual(data.SomeTailData, clone.SomeTailData);
        }
    }
}

