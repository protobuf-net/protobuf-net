using System;
using System.IO;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples
{
    
    public class AutoTuple
    {
        [Fact]
        public void TestHasTuplesWrapped()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;

            var obj = new HasTuples {Value = new BasicTuple(123, "abc")};

            CheckBytes(model, obj, "0A 07 08 7B 12 03 61 62 63", "runtime");
            var clone = (HasTuples) model.DeepClone(obj);
            Assert.Equal(123, clone.Value.Foo); //, "runtime");
            Assert.Equal("abc", clone.Value.Bar); //, "runtime");

            model.Compile("TestHasTuplesWrapped", "TestHasTuplesWrapped.dll");
            PEVerify.AssertValid("TestHasTuplesWrapped.dll");

            model.CompileInPlace();
            CheckBytes(model, obj, "0A 07 08 7B 12 03 61 62 63", "CompileInPlace");
            clone = (HasTuples) model.DeepClone(obj);
            Assert.Equal(123, clone.Value.Foo); //, "CompileInPlace");
            Assert.Equal("abc", clone.Value.Bar); //, "CompileInPlace");

            var compiled = model.Compile();
            CheckBytes(compiled, obj, "0A 07 08 7B 12 03 61 62 63", "Compile");
            clone = (HasTuples)compiled.DeepClone(obj);
            Assert.Equal(123, clone.Value.Foo); //, "Compile");
            Assert.Equal("abc", clone.Value.Bar); //, "Compile");
        }
#pragma warning disable IDE0060
        void CheckBytes(TypeModel model, object obj, string expected, string message)
#pragma warning restore IDE0060
        {
            using var ms = new MemoryStream();
#pragma warning disable CS0618
            model.Serialize(ms, obj);
#pragma warning restore CS0618
            Assert.Equal(expected, Program.GetByteString(ms.ToArray())); //, message);
        }
        [Fact]
        public void TestHasTuplesNaked()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;

            var obj = new BasicTuple(123, "abc");

            CheckBytes(model, obj, "08 7B 12 03 61 62 63", "runtime");
            var clone = (BasicTuple)model.DeepClone(obj);
            Assert.Equal(123, clone.Foo); //, "runtime");
            Assert.Equal("abc", clone.Bar); //, "runtime");

            model.Compile("TestHasTuplesNaked", "TestHasTuplesNaked.dll");
            PEVerify.AssertValid("TestHasTuplesNaked.dll");

            model.CompileInPlace();
            CheckBytes(model, obj, "08 7B 12 03 61 62 63", "CompileInPlace");
            clone = (BasicTuple)model.DeepClone(obj);
            Assert.Equal(123, clone.Foo); //, "CompileInPlace");
            Assert.Equal("abc", clone.Bar); //, "CompileInPlace");

            var compiled = model.Compile();
            CheckBytes(compiled, obj, "08 7B 12 03 61 62 63", "Compile");
            clone = (BasicTuple)compiled.DeepClone(obj);
            Assert.Equal(123, clone.Foo); //, "Compile");
            Assert.Equal("abc", clone.Bar); //, "Compile");
        }
        [Fact]
        public void TestHasTuplesReversedOrderNaked()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;

            var obj = new BasicTupleReversedOrder("abc", 123);

            CheckBytes(model, obj, "0A 03 61 62 63 10 7B", "runtime");
            var clone = (BasicTupleReversedOrder)model.DeepClone(obj);
            Assert.Equal(123, clone.Foo); //, "runtime");
            Assert.Equal("abc", clone.Bar); //, "runtime");

            model.Compile("BasicTupleReversedOrder", "BasicTupleReversedOrder.dll");
            PEVerify.AssertValid("BasicTupleReversedOrder.dll");

            model.CompileInPlace();
            CheckBytes(model, obj, "0A 03 61 62 63 10 7B", "CompileInPlace");
            clone = (BasicTupleReversedOrder)model.DeepClone(obj);
            Assert.Equal(123, clone.Foo); //, "CompileInPlace");
            Assert.Equal("abc", clone.Bar); //, "CompileInPlace");

            var compiled = model.Compile();
            CheckBytes(compiled, obj, "0A 03 61 62 63 10 7B", "Compile");
            clone = (BasicTupleReversedOrder)compiled.DeepClone(obj);
            Assert.Equal(123, clone.Foo); //, "Compile");
            Assert.Equal("abc", clone.Bar); //, "Compile");
        }


        [Fact]
        public void TestInbuiltTupleNaked()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;

            var obj = Tuple.Create(123, "abc");

            CheckBytes(model, obj, "08 7B 12 03 61 62 63", "runtime");
            var clone = (Tuple<int,string>)model.DeepClone(obj);
            Assert.Equal(123, clone.Item1); //, "runtime");
            Assert.Equal("abc", clone.Item2); //, "runtime");

            model.Compile("TestInbuiltTupleNaked", "TestInbuiltTupleNaked.dll");
            PEVerify.AssertValid("TestInbuiltTupleNaked.dll");

            model.CompileInPlace();
            CheckBytes(model, obj, "08 7B 12 03 61 62 63", "CompileInPlace");
            clone = (Tuple<int, string>)model.DeepClone(obj);
            Assert.Equal(123, clone.Item1); //, "CompileInPlace");
            Assert.Equal("abc", clone.Item2); //, "CompileInPlace");

            var compiled = model.Compile();
            CheckBytes(compiled, obj, "08 7B 12 03 61 62 63", "Compile");
            clone = (Tuple<int, string>)compiled.DeepClone(obj);
            Assert.Equal(123, clone.Item1); //, "Compile");
            Assert.Equal("abc", clone.Item2); //, "Compile");
        }


        [Fact]
        public void TestAnonTypeAsTuple()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;

            var obj = new {Foo = 123, Bar = "abc"};

            CheckBytes(model, obj, "08 7B 12 03 61 62 63", "runtime");
            dynamic clone = model.DeepClone(obj);
            Assert.Equal(123, clone.Foo); //, "runtime");
            Assert.Equal("abc", clone.Bar); //, "runtime");

            model.CompileInPlace();
            CheckBytes(model, obj, "08 7B 12 03 61 62 63", "CompileInPlace");
            clone = model.DeepClone(obj);
            Assert.Equal(123, clone.Foo); //, "CompileInPlace");
            Assert.Equal("abc", clone.Bar); //, "CompileInPlace");

            // note: Compile() won't work, as anon-types are internal
        }

        [ProtoContract]
        public class HasTuples
        {
            [ProtoMember(1)]
            public BasicTuple Value { get; set; }
        }

        public struct BasicTuple
        {
#pragma warning disable IDE0044 // Add readonly modifier
            private int foo;
            private string bar;
#pragma warning restore IDE0044 // Add readonly modifier
            public BasicTuple(int foo, string bar)
            {
                this.foo = foo;
                this.bar = bar;
            }
            public readonly int Foo { get { return foo; } }
            public readonly string Bar { get { return bar; } }
        }

        public class BasicTupleReversedOrder
        {
#pragma warning disable IDE0044 // Add readonly modifier
            private int foo;
            private string bar;
#pragma warning restore IDE0044 // Add readonly modifier
            public BasicTupleReversedOrder(string bar, int foo)
            {
                this.foo = foo;
                this.bar = bar;
            }
            public int Foo { get { return foo; } }
            public string Bar { get { return bar; } }
        }
    }
}
