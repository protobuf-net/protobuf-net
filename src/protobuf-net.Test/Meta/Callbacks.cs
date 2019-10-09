using System.IO;
using System.Runtime.Serialization;
using Xunit;
using ProtoBuf.Meta;
using System;

namespace ProtoBuf.unittest.Meta
{
    
    public class Callbacks
    {
        [DataContract, KnownType(typeof(B))]
        public abstract class A {
            protected A() { TraceData += "A.ctor;"; }
            public void ResetTraceData() { TraceData = null; }
            public string TraceData {get;protected set;}
            private int aValue;
            [DataMember(Order=1)]public int AValue {
                get { TraceData += "get;"; return aValue; }
                set { TraceData += "set;"; aValue = value; }
            }
            [OnSerializing] public virtual void OnSerializing(StreamingContext ctx) { TraceData += "A.OnSerializing;";}
            [OnSerialized] public virtual void OnSerialized(StreamingContext ctx) { TraceData += "A.OnSerialized;";}
            [OnDeserializing] public virtual void OnDeserializing(StreamingContext ctx) { TraceData += "A.OnDeserializing;";}
            [OnDeserialized] public virtual void OnDeserialized(StreamingContext ctx) { TraceData += "A.OnDeserialized;";}
        }
        [DataContract, KnownType(typeof(C))]
        public class B : A {
            public B() { TraceData += "B.ctor;"; }
            private int bValue;
            [DataMember(Order = 1)]
            public int BValue {
                get { TraceData += "get;"; return bValue; }
                set { TraceData += "set;"; bValue = value; }
            }
            public override void OnSerializing(StreamingContext ctx) { TraceData += "B.OnSerializing;";}
            public override void OnSerialized(StreamingContext ctx) { TraceData += "B.OnSerialized;";}
            public override void OnDeserializing(StreamingContext ctx) { TraceData += "B.OnDeserializing;";}
            public override void OnDeserialized(StreamingContext ctx) { TraceData += "B.OnDeserialized;";}
        }
        [DataContract]
        public sealed class C : B {
            public C()
            {
                TraceData += "C.ctor;";
            }
            private int cValue;
            [DataMember(Order = 1)]public int CValue {
                get { TraceData += "get;"; return cValue; }
                set { TraceData += "set;"; cValue = value; }
            }
            public override void OnSerializing(StreamingContext ctx) { TraceData += "C.OnSerializing;";}
            public override void OnSerialized(StreamingContext ctx) { TraceData += "C.OnSerialized;";}
            public override void OnDeserializing(StreamingContext ctx) { TraceData += "C.OnDeserializing;";}
            public override void OnDeserialized(StreamingContext ctx) { TraceData += "C.OnDeserialized;";}
        }
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CanCompileModel(bool useCtor)
        {
            var model = BuildModel(useCtor);
            model.CompileInPlace();

            var path = useCtor ? "Callbacks_Ctor.dll" : "Callbacks_NoCtor.dll";
            model.Compile("Callbacks", path);
            PEVerify.Verify(path);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestCallbacksAtMultipleInheritanceLevels(bool useCtor)
        {
            // C dcsOrig, dcsClone;
            C pbClone, pbOrig;
            //DataContractSerializer ser = new DataContractSerializer(typeof(B));
            //using (MemoryStream ms = new MemoryStream()) {
            //    ser.WriteObject(ms, (dcsOrig = CreateC()));
            //    ms.Position = 0;
            //    dcsClone = (C)ser.ReadObject(ms);
            //}
            //Assert.Equal(dcsOrig.AValue, dcsClone.AValue);
            //Assert.Equal(dcsOrig.BValue, dcsClone.BValue);
            //Assert.Equal(dcsOrig.CValue, dcsClone.CValue);

            var expectedDeserTrace = (useCtor ? "A.ctor;B.ctor;C.ctor;" : "") + "C.OnDeserializing;set;set;set;C.OnDeserialized;get;get;get;";
            var model = BuildModel(useCtor);
            pbClone = (C) model.DeepClone((pbOrig = CreateC()));
            Assert.Equal(pbOrig.AValue, pbClone.AValue); //, "Runtime");
            Assert.Equal(pbOrig.BValue, pbClone.BValue); //, "Runtime");
            Assert.Equal(pbOrig.CValue, pbClone.CValue); //, "Runtime");
            Assert.Equal("C.OnSerializing;get;get;get;C.OnSerialized;get;get;get;", pbOrig.TraceData); //, "Runtime");
            Assert.Equal(expectedDeserTrace, pbClone.TraceData); //, "Runtime");

            model.CompileInPlace();
            pbClone = (C)model.DeepClone((pbOrig = CreateC()));
            Assert.Equal(pbOrig.AValue, pbClone.AValue); //, "CompileInPlace");
            Assert.Equal(pbOrig.BValue, pbClone.BValue); //, "CompileInPlace");
            Assert.Equal(pbOrig.CValue, pbClone.CValue); //, "CompileInPlace");
            Assert.Equal("C.OnSerializing;get;get;get;C.OnSerialized;get;get;get;", pbOrig.TraceData); //, "Runtime");
            Assert.Equal(expectedDeserTrace, pbClone.TraceData); //, "Runtime");

            pbClone = (C)model.Compile().DeepClone((pbOrig = CreateC()));
            Assert.Equal(pbOrig.AValue, pbClone.AValue); //, "CompileFully");
            Assert.Equal(pbOrig.BValue, pbClone.BValue); //, "CompileFully");
            Assert.Equal(pbOrig.CValue, pbClone.CValue); //, "CompileFully");
            Assert.Equal("C.OnSerializing;get;get;get;C.OnSerialized;get;get;get;", pbOrig.TraceData); //, "Runtime");
            Assert.Equal(expectedDeserTrace, pbClone.TraceData); //, "Runtime");
        }

        static C CreateC() {
            C c = new C { AValue = 123, BValue = 456, CValue = 789 };

            c.ResetTraceData();
            return c;}
        private static RuntimeTypeModel BuildModel(bool useCtor)
        {
            //DataContractSerializer ser = new DataContractSerializer(typeof(B));
            //bool useCtor;
            //using (var ms = new MemoryStream())
            //{
            //    ser.WriteObject(ms, new B());
            //    ms.Position = 0;
            //    B b = (B)ser.ReadObject(ms);
            //    useCtor = b.TraceData.StartsWith("A.ctor;B.ctor;");
            //}

            var model = RuntimeTypeModel.Create();
            model.Add(typeof(A), false).Add(2, "AValue").SetCallbacks("OnSerializing", "OnSerialized", "OnDeserializing", "OnDeserialized").UseConstructor = useCtor;
            model.Add(typeof(B), false).Add(2, "BValue")
                //.SetCallbacks("OnSerializing", "OnSerialized", "OnDeserializing", "OnDeserialized")
                .UseConstructor = useCtor;
            model.Add(typeof(C), false).Add(2, "CValue")
                //.SetCallbacks("OnSerializing", "OnSerialized", "OnDeserializing", "OnDeserialized")
                .UseConstructor = useCtor;
            model[typeof(A)].AddSubType(1, typeof(B));
            model[typeof(B)].AddSubType(1, typeof(C));
            return model;
        }
    }
}
