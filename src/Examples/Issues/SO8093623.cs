using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf;
using System.IO;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    
    public class SO8093623
    {
        [ProtoContract]
        public class A_generated
        {
            [ProtoMember(1)]
            public int Age;

            [ProtoMember(10)]
            public B_generated b;
        }

        [ProtoContract]
        public class B_generated
        {
            [ProtoMember(2)]
            public int Balls;
        }
        [ProtoContract, ProtoInclude(10, typeof(B))]
        public class A
        {
            [ProtoMember(1)]
            public int Age;
        }
        [ProtoContract]
        public class B : A
        {
            [ProtoMember(2)]
            public int Balls;
        }

        [Fact]
        public void TestExpectedResultFromGeneratedTypes()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add(typeof(A_generated), true);
            model.Add(typeof(B_generated), true);

            TestGeneratedModel(model, "Runtime");
            model.CompileInPlace();
            TestGeneratedModel(model, "CompileInPlace");
            TestGeneratedModel(model.Compile(), "Compile");
        }
        private static void TestGeneratedModel(TypeModel model, string message)
        {
            var a = new A_generated() { Age = 10, b = new B_generated { Balls = 23 } };
            using var ms = new MemoryStream();
            model.Serialize(ms, a);
            Assert.True(ms.ToArray().SequenceEqual(new byte[] { 08, 10, 82, 2, 16, 23 }), message);
            ms.Position = 0;
#pragma warning disable CS0618
            var clone = (A_generated)model.Deserialize(ms, null, typeof(A_generated));
#pragma warning restore CS0618
            Assert.Equal(10, clone.Age); //, message);
            Assert.Equal(23, clone.b.Balls); //, message);
        }
        [Fact]
        public void TestSubclassDeserializes()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add(typeof (A), true);
            model.Add(typeof (B), true);

            TestInheritanceModel(model, "Runtime");
            model.CompileInPlace();
            TestInheritanceModel(model, "CompileInPlace");
            TestInheritanceModel(model.Compile(), "Compile");
        }

#pragma warning disable IDE0060
        private static void TestInheritanceModel(TypeModel model, string message)
#pragma warning restore IDE0060
        {
            using var ms = new MemoryStream(new byte[] { 08, 10, 82, 2, 16, 23 });
#pragma warning disable CS0618
            var clone = (A)model.Deserialize(ms, null, typeof(A));
#pragma warning restore CS0618
            Assert.Equal(10, clone.Age); //, message);
            B b = (B)clone;
            Assert.Equal(23, b.Balls); //, message);
        }
    }
}
