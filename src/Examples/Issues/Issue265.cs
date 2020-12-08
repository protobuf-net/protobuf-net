
using System;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;
using System.Linq;
using System.Collections.Generic;

namespace Examples.Issues
{
    
    public class Issue265
    {
        public enum E
        {
            [ProtoEnum]
            V0 = 0,
            [ProtoEnum]
            V1 = 1,
            [ProtoEnum]
            V2 = 2,
        }
        [ProtoContract]
        public class Foo
        {
            [ProtoMember(3)]
            public E[] Bar { get; set; }
        }

        [Fact]
        public void ShouldSerializeEnumArrayMember()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            TestMember(model);
            model.Compile("ShouldSerializeEnumArrayMember", "ShouldSerializeEnumArrayMember.dll");
            PEVerify.AssertValid("ShouldSerializeEnumArrayMember.dll");
            model.CompileInPlace();
            TestMember(model);
            TestMember(model.Compile());
        }

        private static void TestMember(TypeModel model)
        {
            var value = new Foo {Bar = new E[] {E.V0, E.V1, E.V2}};

            Assert.True(Program.CheckBytes(value, model, 0x18, 0x00, 0x18, 0x01, 0x18, 0x02));
            var clone = (Foo) model.DeepClone(value);
            Assert.Equal("V0,V1,V2", string.Join(",", clone.Bar)); //, "clone");
        }

        [Fact]
        public void VerifyThatIntsAreHandledAppropriatelyForComparison()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            var orig = new int[] {3, 4, 5};
            Program.CheckBytes(orig, model, "08-03-08-04-08-05");
            var clone = (int[])model.DeepClone(orig);
            Assert.Equal("3,4,5", string.Join(",", clone)); //, "clone");
        }

        [Fact]
        public void ShouldSerializeIndividualEnum()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            TestIndividual(model);
            model.Compile("ShouldSerializeIndividualEnum", "ShouldSerializeIndividualEnum.dll");
            PEVerify.AssertValid("ShouldSerializeIndividualEnum.dll");
            model.CompileInPlace();
            TestIndividual(model);
            TestIndividual(model.Compile());
        }

        private static void TestIndividual(TypeModel model)
        {
            var value = E.V1;
            Assert.True(Program.CheckBytes(value, model, 0x08, 0x01));
            Assert.Equal(value, model.DeepClone(value));
        }

        [Fact]
        public void ShouldSerializeArrayOfEnums()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add(typeof(E));
            TestArray(model);
            model.Compile("ShouldSerializeArrayOfEnums", "ShouldSerializeArrayOfEnums.dll");
            PEVerify.AssertValid("ShouldSerializeArrayOfEnums.dll");
            model.CompileInPlace();
            TestArray(model);
            TestArray(model.Compile());

            var schema = model.GetSchema(typeof(E[]));
            Assert.Equal(@"syntax = ""proto3"";
package Examples.Issues;

message Array_E {
   repeated E items = 1;
}
enum E {
   V0 = 0;
   V1 = 1;
   V2 = 2;
}
", schema, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void ShouldSerializeListOfEnums()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add(typeof(E));
            TestList(model);
            model.Compile("ShouldSerializeListOfEnums", "ShouldSerializeListOfEnums.dll");
            PEVerify.AssertValid("ShouldSerializeListOfEnums.dll");
            model.CompileInPlace();
            TestList(model);
            TestList(model.Compile());

            var schema = model.GetSchema(typeof(List<E>));
            Assert.Equal(@"syntax = ""proto3"";
package Examples.Issues;

enum E {
   V0 = 0;
   V1 = 1;
   V2 = 2;
}
message List_E {
   repeated E items = 1;
}
", schema, ignoreLineEndingDifferences: true);
        }

        private static void TestArray(TypeModel model)
        {
            var value = new[] {E.V0, E.V1, E.V2};
            Assert.Equal("V0,V1,V2", string.Join(",", value)); //, "original");
            Program.CheckBytes(value, model, "08-00-08-01-08-02");
            var clone = (E[]) model.DeepClone(value);
            Assert.Equal("V0,V1,V2", string.Join(",", clone)); //, "clone");
            Assert.True(value.SequenceEqual(clone));
        }
        private static void TestList(TypeModel model)
        {
            var value = new List<E> { E.V0, E.V1, E.V2 };
            Assert.Equal("V0,V1,V2", string.Join(",", value)); //, "original");
            Program.CheckBytes(value, model, "08-00-08-01-08-02");
            var clone = (List<E>)model.DeepClone(value);
            Assert.Equal("V0,V1,V2", string.Join(",", clone)); //, "clone");
            Assert.True(value.SequenceEqual(clone));
        }
    }
}
