using ProtoBuf;
using ProtoBuf.Meta;
using System.Runtime.Serialization;
using Xunit;

namespace Examples.SimpleStream
{

    public class NullTypes
    {
        [DataContract]
        public class TypeWithNulls
        {
            [DataMember(Order = 1)]
            public int? Foo { get; set; }
        }
        [Fact]
        public void TestNull()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;

            TypeWithNulls twn = new TypeWithNulls { Foo = null },
                clone = model.DeepClone(twn);
            Assert.Null(twn.Foo);
            Assert.True(Program.CheckBytes(twn, model, new byte[0]));
            Assert.Null(clone.Foo);
            Assert.True(Program.CheckBytes(clone, model, new byte[0]));

            var compiled = model.Compile("TestNull", "TestNull.dll");
            PEVerify.AssertValid("TestNull.dll");
            clone = compiled.DeepClone(twn);
            Assert.True(Program.CheckBytes(twn, compiled, new byte[0]));
            Assert.True(Program.CheckBytes(clone, compiled, new byte[0]));

            model.CompileInPlace();
            clone = model.DeepClone(twn);
            Assert.True(Program.CheckBytes(twn, model, new byte[0]));
            Assert.True(Program.CheckBytes(clone, model, new byte[0]));

            compiled = model.Compile();
            clone = compiled.DeepClone(twn);
            Assert.True(Program.CheckBytes(twn, compiled, new byte[0]));
            Assert.True(Program.CheckBytes(clone, compiled, new byte[0]));
        }
        [Fact]
        public void TestNotNull()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;

            TypeWithNulls twn = new TypeWithNulls { Foo = 150 },
                clone = model.DeepClone(twn);
            Assert.NotNull(twn.Foo);
            Program.CheckBytes(twn, model, "08-96-01");
            Assert.NotNull(clone.Foo);
            Program.CheckBytes(clone, model, "08-96-01");

            var compiled = model.Compile("TestNotNull", "TestNotNull.dll");
            PEVerify.AssertValid("TestNotNull.dll");
            clone = compiled.DeepClone(twn);
            Program.CheckBytes(twn, compiled, "08-96-01");
            Program.CheckBytes(clone, compiled, "08-96-01");

            model.CompileInPlace();
            clone = model.DeepClone(twn);
            Program.CheckBytes(twn, model, "08-96-01");
            Program.CheckBytes(clone, model, "08-96-01");

            compiled = model.Compile();
            clone = compiled.DeepClone(twn);
            Program.CheckBytes(twn, compiled, "08-96-01");
            Program.CheckBytes(clone, compiled, "08-96-01");

        }
    }
}
