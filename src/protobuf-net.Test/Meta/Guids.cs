using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf.Meta;
using System.Diagnostics;

namespace ProtoBuf.unittest.Meta
{
    
    public class Guids
    {
        public class Data {
            public Guid Value {get;set;}
            public char SomeTailData { get; set; }
        }
        static RuntimeTypeModel BuildModel() {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(Data), false)
                .Add(1, "Value")
                .Add(2, "SomeTailData");
            return model;
        }
        [Fact]
        public void TestRoundTripEmpty()
        {
            var model = BuildModel();
            Data data = new Data { Value = Guid.Empty, SomeTailData = 'D' };

            TestGuid(model, data);
            model.CompileInPlace();
            TestGuid(model, data);
            TestGuid(model.Compile(), data);
        }

        
        [Fact]
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
        [Fact]
        public void TestRoundTripRandomRuntime()
        {
            var model = BuildModel();
            TestGuids(model, 1000);
        }
        [Fact]
        public void TestRoundTripRandomCompileInPlace()
        {
            var model = BuildModel();
            model.CompileInPlace();
            TestGuids(model, 1000);
        }

        [Fact]
        public void TestRoundTripRandomCompile()
        {
            var model = BuildModel().Compile("TestRoundTripRandomCompile", "TestRoundTripRandomCompile.dll");
            PEVerify.Verify("TestRoundTripRandomCompile.dll");
            TestGuids(model, 1000);
        }

        static void TestGuids(TypeModel model, int count)
        {
            Random rand = new Random();
            byte[] buffer = new byte[16];
            Stopwatch watch = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                rand.NextBytes(buffer);
                Data data = new Data { Value = new Guid(buffer), SomeTailData = (char)rand.Next(1, ushort.MaxValue) };
                Data clone = (Data)model.DeepClone(data);
                Assert.NotNull(clone);
                Assert.Equal(data.Value, clone.Value);
                Assert.Equal(data.SomeTailData, clone.SomeTailData);
            }
            watch.Stop();
#if !COREFX
            Trace.WriteLine(watch.ElapsedMilliseconds);
#endif
        }
        static void TestGuid(TypeModel model, Data data)
        {
            Data clone = (Data)model.DeepClone(data);
            Assert.Equal(data.Value, clone.Value);
            Assert.Equal(data.SomeTailData, clone.SomeTailData);
        }
    }
}

