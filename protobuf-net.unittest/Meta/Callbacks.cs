using System.IO;
using System.Runtime.Serialization;
using NUnit.Framework;
using ProtoBuf.Meta;
using System;

namespace ProtoBuf.unittest.Meta
{
    [TestFixture]
    public class Callbacks
    {
        [DataContract, KnownType(typeof(B))]
        public abstract class A {
            protected A() { TraceData += "ctor;"; }
            public void ResetTraceData() { TraceData = null; }
            public string TraceData {get;protected set;}
            private int aValue;
            [DataMember(Order=1)]public int AValue {
                get { TraceData += "get;"; return aValue; }
                set { TraceData += "set;"; aValue = value; }
            }
            [OnSerializing] public void OnSerializing(StreamingContext ctx) { TraceData += "A.OnSerializing;";}
            [OnSerialized] public void OnSerialized(StreamingContext ctx) { TraceData += "A.OnSerialized;";}
            [OnDeserializing] public void OnDeserializing(StreamingContext ctx) { TraceData += "A.OnDeserializing;";}
            [OnDeserialized] public void OnDeserialized(StreamingContext ctx) { TraceData += "A.OnDeserialized;";}
        }
        [DataContract, KnownType(typeof(C))]
        public class B : A {
            private int bValue;
            [DataMember(Order = 1)]
            public int BValue {
                get { TraceData += "get;"; return bValue; }
                set { TraceData += "set;"; bValue = value; }
            }
            [OnSerializing] public new void OnSerializing(StreamingContext ctx) { TraceData += "B.OnSerializing;";}
            [OnSerialized] public new void OnSerialized(StreamingContext ctx) { TraceData += "B.OnSerialized;";}
            [OnDeserializing] public new void OnDeserializing(StreamingContext ctx) { TraceData += "B.OnDeserializing;";}
            [OnDeserialized] public new void OnDeserialized(StreamingContext ctx) { TraceData += "B.OnDeserialized;";}
        }
        [DataContract]
        public sealed class C : B {
            private int cValue;
            [DataMember(Order = 1)]public int CValue {
                get { TraceData += "get;"; return cValue; }
                set { TraceData += "set;"; cValue = value; }
            }
            [OnSerializing] public new void OnSerializing(StreamingContext ctx) { TraceData += "C.OnSerializing;";}
            [OnSerialized] public new void OnSerialized(StreamingContext ctx) { TraceData += "C.OnSerialized;";}
            [OnDeserializing] public new void OnDeserializing(StreamingContext ctx) { TraceData += "C.OnDeserializing;";}
            [OnDeserialized] public new void OnDeserialized(StreamingContext ctx) { TraceData += "C.OnDeserialized;";}
        }
        [Test]
        public void CanCompileModel()
        {
            var model = BuildModel();
            model.CompileInPlace();
            model.Compile("Callbacks", "Callbacks.dll");
            PEVerify.Verify("Callbacks.dll");
        }
        [Test]
        public void TestCallbacksAtMultipleInheritanceLevels()
        {
            C dcsOrig, dcsClone, pbClone, pbOrig;
            DataContractSerializer ser = new DataContractSerializer(typeof(B));
            using (MemoryStream ms = new MemoryStream()) {
                ser.WriteObject(ms, (dcsOrig = CreateC()));
                ms.Position = 0;
                dcsClone = (C)ser.ReadObject(ms);
            }
            Assert.AreEqual(dcsOrig.AValue, dcsClone.AValue);
            Assert.AreEqual(dcsOrig.BValue, dcsClone.BValue);
            Assert.AreEqual(dcsOrig.CValue, dcsClone.CValue);

            var model = BuildModel();
            pbClone = (C) model.DeepClone((pbOrig = CreateC()));
            Assert.AreEqual(pbOrig.AValue, pbClone.AValue, "Runtime");
            Assert.AreEqual(pbOrig.BValue, pbClone.BValue, "Runtime");
            Assert.AreEqual(pbOrig.CValue, pbClone.CValue, "Runtime");
            Assert.AreEqual(dcsOrig.TraceData, pbOrig.TraceData, "Runtime");
            Assert.AreEqual(dcsClone.TraceData, pbClone.TraceData, "Runtime");

            model.CompileInPlace();
            pbClone = (C)model.DeepClone((pbOrig = CreateC()));
            Assert.AreEqual(pbOrig.AValue, pbClone.AValue, "CompileInPlace");
            Assert.AreEqual(pbOrig.BValue, pbClone.BValue, "CompileInPlace");
            Assert.AreEqual(pbOrig.CValue, pbClone.CValue, "CompileInPlace");
            Assert.AreEqual(dcsOrig.TraceData, pbOrig.TraceData, "CompileInPlace");
            Assert.AreEqual(dcsClone.TraceData, pbClone.TraceData, "CompileInPlace");

            pbClone = (C)model.Compile().DeepClone((pbOrig = CreateC()));
            Assert.AreEqual(pbOrig.AValue, pbClone.AValue, "CompileFully");
            Assert.AreEqual(pbOrig.BValue, pbClone.BValue, "CompileFully");
            Assert.AreEqual(pbOrig.CValue, pbClone.CValue, "CompileFully");
            Assert.AreEqual(dcsOrig.TraceData, pbOrig.TraceData, "CompileFully");
            Assert.AreEqual(dcsClone.TraceData, pbClone.TraceData, "CompileFully");
        }

        static C CreateC() {
            C c = new C { AValue = 123, BValue = 456, CValue = 789 };

            c.ResetTraceData();
            return c;}
        private static RuntimeTypeModel BuildModel()
        {
            var model = TypeModel.Create();
            model.Add(typeof(A), false).Add(2, "AValue").SetCallbacks("OnSerializing", "OnSerialized", "OnDeserializing", "OnDeserialized").UseConstructor = false;
            model.Add(typeof(B), false).Add(2, "BValue").SetCallbacks("OnSerializing", "OnSerialized", "OnDeserializing", "OnDeserialized").UseConstructor = false;
            model.Add(typeof(C), false).Add(2, "CValue").SetCallbacks("OnSerializing", "OnSerialized", "OnDeserializing", "OnDeserialized").UseConstructor = false;
            model[typeof(A)].AddSubType(1, typeof(B));
            model[typeof(B)].AddSubType(1, typeof(C));
            return model;
        }
    }
}
