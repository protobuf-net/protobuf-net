using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf.Meta;
using System.IO;

namespace ProtoBuf.unittest.Meta
{
    [TestFixture]
    public class SubItems
    {
        static RuntimeTypeModel CreateModel()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(OuterRef), false)
                .Add(1, "Int32")
                .Add(2, "String")
                .Add(3, "InnerVal")
                .Add(4, "InnerRef");
            model.Add(typeof(InnerRef), false)
                .Add(1, "Int32")
                .Add(2, "String");
            model.Add(typeof(OuterVal), false)
                .Add(1, "Int32")
                .Add(2, "String")
                .Add(3, "InnerVal")
                .Add(4, "InnerRef");
            model.Add(typeof(InnerVal), false)
                .Add(1, "Int32")
                .Add(2, "String");
            return model;
        }

        [Test]
        public void BuildModel()
        {
            Assert.IsNotNull(CreateModel());
        }

        [Test]
        public void TestCanDeserialierAllFromEmptyStream()
        {
            var model = CreateModel();
            Assert.IsInstanceOfType(typeof(OuterRef), model.Deserialize(Stream.Null, null, typeof(OuterRef)));
            Assert.IsInstanceOfType(typeof(OuterVal), model.Deserialize(Stream.Null, null, typeof(OuterVal)));
            Assert.IsInstanceOfType(typeof(InnerRef), model.Deserialize(Stream.Null, null, typeof(InnerRef)));
            Assert.IsInstanceOfType(typeof(InnerVal), model.Deserialize(Stream.Null, null, typeof(InnerVal)));

            model.CompileInPlace();
            Assert.IsInstanceOfType(typeof(OuterRef), model.Deserialize(Stream.Null, null, typeof(OuterRef)));
            Assert.IsInstanceOfType(typeof(OuterVal), model.Deserialize(Stream.Null, null, typeof(OuterVal)));
            Assert.IsInstanceOfType(typeof(InnerRef), model.Deserialize(Stream.Null, null, typeof(InnerRef)));
            Assert.IsInstanceOfType(typeof(InnerVal), model.Deserialize(Stream.Null, null, typeof(InnerVal)));

            var compiled = model.Compile("SubItems","SubItems.dll");
            PEVerify.Verify("SubItems.dll");
            Assert.IsInstanceOfType(typeof(OuterRef), compiled.Deserialize(Stream.Null, null, typeof(OuterRef)));
            Assert.IsInstanceOfType(typeof(OuterVal), compiled.Deserialize(Stream.Null, null, typeof(OuterVal)));
            Assert.IsInstanceOfType(typeof(InnerRef), compiled.Deserialize(Stream.Null, null, typeof(InnerRef)));
            Assert.IsInstanceOfType(typeof(InnerVal), compiled.Deserialize(Stream.Null, null, typeof(InnerVal)));

        }



        [Test]
        public void TestRoundTripOuterRef()
        {
            OuterRef outer = new OuterRef
            {
                InnerRef = new InnerRef { Int32 = 123, String = "abc" },
                InnerVal = new InnerVal { Int32 = 456, String = "def" }
            }, clone;
            
            var model = CreateModel();
            clone = (OuterRef)model.DeepClone(outer);
            Assert.AreNotSame(outer, clone);
            Assert.AreEqual(123, clone.InnerRef.Int32);
            Assert.AreEqual("abc", clone.InnerRef.String);
            Assert.AreEqual(456, clone.InnerVal.Int32);
            Assert.AreEqual("def", clone.InnerVal.String);

            model.CompileInPlace();
            clone = (OuterRef)model.DeepClone(outer);
            Assert.AreNotSame(outer, clone);
            Assert.AreEqual(123, clone.InnerRef.Int32);
            Assert.AreEqual("abc", clone.InnerRef.String);
            Assert.AreEqual(456, clone.InnerVal.Int32);
            Assert.AreEqual("def", clone.InnerVal.String);

            clone = (OuterRef)model.Compile().DeepClone(outer);
            Assert.AreNotSame(outer, clone);
            Assert.AreEqual(123, clone.InnerRef.Int32);
            Assert.AreEqual("abc", clone.InnerRef.String);
            Assert.AreEqual(456, clone.InnerVal.Int32);
            Assert.AreEqual("def", clone.InnerVal.String);
        }

        [Test]
        public void TestRoundTripOuterVal()
        {
            OuterVal outer = new OuterVal
            {
                InnerRef = new InnerRef { Int32 = 123, String = "abc" },
                InnerVal = new InnerVal { Int32 = 456, String = "def" }
            }, clone;

            var model = CreateModel();
            clone = (OuterVal)model.DeepClone(outer);
            Assert.AreNotSame(outer, clone);
            Assert.AreEqual(123, clone.InnerRef.Int32);
            Assert.AreEqual("abc", clone.InnerRef.String);
            Assert.AreEqual(456, clone.InnerVal.Int32);
            Assert.AreEqual("def", clone.InnerVal.String);
            
            model.CompileInPlace();
            clone = (OuterVal)model.DeepClone(outer);
            
            Assert.AreNotSame(outer, clone);
            Assert.AreEqual(123, clone.InnerRef.Int32);
            Assert.AreEqual("abc", clone.InnerRef.String);
            Assert.AreEqual(456, clone.InnerVal.Int32);
            Assert.AreEqual("def", clone.InnerVal.String);
            
            clone = (OuterVal)model.Compile().DeepClone(outer);
            Assert.AreNotSame(outer, clone);
            Assert.AreEqual(123, clone.InnerRef.Int32);
            Assert.AreEqual("abc", clone.InnerRef.String);
            Assert.AreEqual(456, clone.InnerVal.Int32);
            Assert.AreEqual("def", clone.InnerVal.String);
        }

        public class OuterRef
        {
            public int Int32 { get; set; }
            public string String{ get; set; }
            public InnerRef InnerRef { get; set; }
            public InnerVal InnerVal { get; set; }
        }
        public class InnerRef
        {
            public int Int32 { get; set; }
            public string String { get; set; }
        }

        public struct OuterVal
        {
            public int Int32 { get; set; }
            public string String { get; set; }
            public InnerRef InnerRef { get; set; }
            public InnerVal InnerVal { get; set; }
        }
        public struct InnerVal
        {
            public int Int32 { get; set; }
            public string String { get; set; }
        }

        [Test]
        public void TestTypeWithNullableProps()
        {
            var model = TypeModel.Create();
            TypeWithNulls obj = new TypeWithNulls { First = 123, Second = 456.789M };
            
            var clone1 = (TypeWithNulls)model.DeepClone(obj);
            
            model.CompileInPlace();
            var clone2 = (TypeWithNulls)model.DeepClone(obj);

            
            TypeModel compiled = model.Compile("TestTypeWithNullableProps", "TestTypeWithNullableProps.dll");
            PEVerify.Verify("TestTypeWithNullableProps.dll");
            var clone3 = (TypeWithNulls)compiled.DeepClone(obj);
            Assert.AreEqual(123, clone1.First);
            Assert.AreEqual(456.789, clone1.Second);

            Assert.AreEqual(123, clone2.First);
            Assert.AreEqual(456.789, clone2.Second);

            Assert.AreEqual(123, clone3.First);
            Assert.AreEqual(456.789, clone3.Second);

        }

        [ProtoContract]
        public class TypeWithNulls
        {
            [ProtoMember(1)]
            public int? First { get; set; }

            [ProtoMember(2)]
            public decimal? Second { get; set; }
        }

    }
}
