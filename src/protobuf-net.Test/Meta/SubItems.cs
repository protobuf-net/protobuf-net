using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf.Meta;
using System.IO;

namespace ProtoBuf.unittest.Meta
{
    
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

        [Fact]
        public void BuildModel()
        {
            Assert.NotNull(CreateModel());
        }

        [Fact]
        public void TestCanDeserialierAllFromEmptyStream()
        {
            var model = CreateModel();
            Assert.IsType<OuterRef>(model.Deserialize(Stream.Null, null, typeof(OuterRef)));
            Assert.IsType<OuterVal>(model.Deserialize(Stream.Null, null, typeof(OuterVal)));
            Assert.IsType<InnerRef>(model.Deserialize(Stream.Null, null, typeof(InnerRef)));
            Assert.IsType<InnerVal>(model.Deserialize(Stream.Null, null, typeof(InnerVal)));

            model.CompileInPlace();
            Assert.IsType<OuterRef>(model.Deserialize(Stream.Null, null, typeof(OuterRef)));
            Assert.IsType<OuterVal>(model.Deserialize(Stream.Null, null, typeof(OuterVal)));
            Assert.IsType<InnerRef>(model.Deserialize(Stream.Null, null, typeof(InnerRef)));
            Assert.IsType<InnerVal>(model.Deserialize(Stream.Null, null, typeof(InnerVal)));

            var compiled = model.Compile("SubItems","SubItems.dll");
            PEVerify.Verify("SubItems.dll");
            Assert.IsType<OuterRef>(compiled.Deserialize(Stream.Null, null, typeof(OuterRef)));
            Assert.IsType<OuterVal>(compiled.Deserialize(Stream.Null, null, typeof(OuterVal)));
            Assert.IsType<InnerRef>(compiled.Deserialize(Stream.Null, null, typeof(InnerRef)));
            Assert.IsType<InnerVal>(compiled.Deserialize(Stream.Null, null, typeof(InnerVal)));
        }



        [Fact]
        public void TestRoundTripOuterRef()
        {
            OuterRef outer = new OuterRef
            {
                InnerRef = new InnerRef { Int32 = 123, String = "abc" },
                InnerVal = new InnerVal { Int32 = 456, String = "def" }
            }, clone;
            
            var model = CreateModel();
            clone = (OuterRef)model.DeepClone(outer);
            Assert.NotSame(outer, clone);
            Assert.Equal(123, clone.InnerRef.Int32);
            Assert.Equal("abc", clone.InnerRef.String);
            Assert.Equal(456, clone.InnerVal.Int32);
            Assert.Equal("def", clone.InnerVal.String);

            model.CompileInPlace();
            clone = (OuterRef)model.DeepClone(outer);
            Assert.NotSame(outer, clone);
            Assert.Equal(123, clone.InnerRef.Int32);
            Assert.Equal("abc", clone.InnerRef.String);
            Assert.Equal(456, clone.InnerVal.Int32);
            Assert.Equal("def", clone.InnerVal.String);

            clone = (OuterRef)model.Compile().DeepClone(outer);
            Assert.NotSame(outer, clone);
            Assert.Equal(123, clone.InnerRef.Int32);
            Assert.Equal("abc", clone.InnerRef.String);
            Assert.Equal(456, clone.InnerVal.Int32);
            Assert.Equal("def", clone.InnerVal.String);
        }

        [Fact]
        public void TestRoundTripOuterVal()
        {
            OuterVal outer = new OuterVal
            {
                InnerRef = new InnerRef { Int32 = 123, String = "abc" },
                InnerVal = new InnerVal { Int32 = 456, String = "def" }
            }, clone;

            var model = CreateModel();
            clone = (OuterVal)model.DeepClone(outer);
            Assert.Equal(123, clone.InnerRef.Int32);
            Assert.Equal("abc", clone.InnerRef.String);
            Assert.Equal(456, clone.InnerVal.Int32);
            Assert.Equal("def", clone.InnerVal.String);
            
            model.CompileInPlace();
            clone = (OuterVal)model.DeepClone(outer);
            
            Assert.Equal(123, clone.InnerRef.Int32);
            Assert.Equal("abc", clone.InnerRef.String);
            Assert.Equal(456, clone.InnerVal.Int32);
            Assert.Equal("def", clone.InnerVal.String);
            
            clone = (OuterVal)model.Compile().DeepClone(outer);
            Assert.Equal(123, clone.InnerRef.Int32);
            Assert.Equal("abc", clone.InnerRef.String);
            Assert.Equal(456, clone.InnerVal.Int32);
            Assert.Equal("def", clone.InnerVal.String);
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

        [Fact]
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
            Assert.Equal(123, clone1.First);
            Assert.Equal(456.789M, clone1.Second);

            Assert.Equal(123, clone2.First);
            Assert.Equal(456.789M, clone2.Second);

            Assert.Equal(123, clone3.First);
            Assert.Equal(456.789M, clone3.Second);
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
