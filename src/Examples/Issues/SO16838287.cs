using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.IO;

namespace Examples.Issues
{
    
    public class SO16838287
    {
        [Fact]
        public void ExecuteRuntime()
        {
            var model = GetModel();
            Program.ExpectFailure<NotSupportedException>(() =>
            {
                Execute(model, 20, 0, 20, "Runtime");
                Execute(model, 1, 0, 18, "Runtime");
            }); // ICollection<T>.Add(T)
        }
        [Fact]
        public void ExecuteCompileInPlace()
        {

            var model = GetModel();
            model.CompileInPlace();
            Program.ExpectFailure<NotSupportedException>(() =>
            {
                Execute(model, 20, 0, 20, "CompileInPlace");
                Execute(model, 1, 0, 18, "CompileInPlace");
            }); // ICollection<T>.Add(T)
        }
        [Fact]
        public void ExecuteCompile()
        {
            var model = GetModel();
            var compiled = model.Compile();
            Program.ExpectFailure<NotSupportedException>(() =>
            {
                Execute(compiled, 20, 0, 20, "Compile");
                Execute(compiled, 1, 0, 18, "Compile");
            }); // ICollection<T>.Add(T)
        }
        
        [Fact]
        public void VerifyCompile()
        {
            var model = GetModel();
            model.Compile("SO16838287", "SO16838287.dll");
            PEVerify.AssertValid("SO16838287.dll");
        }

        static void Execute(TypeModel model, int size, int offset, int count, string caption)
        {
            byte[] data = new byte[size];
            new Random(1234).NextBytes(data);
            var obj = new Foo { Data = new ArraySegment<byte>(data, offset, count) };
            var clone = (Foo) model.DeepClone(obj);
            var seg2 = clone.Data;
            var data2 = seg2.Array;

            Assert.Equal(offset, seg2.Offset); //, caption);
            Assert.Equal(count, seg2.Count); //, caption);
            Assert.Equal(data.Length, data2.Length); //, caption);
            Assert.Equal(BitConverter.ToString(data), BitConverter.ToString(data2)); //, caption);
        }
        static RuntimeTypeModel GetModel()
        {
            var model = TypeModel.Create();
            model.AutoCompile = false;
            model.Add(typeof(Foo), true);
            return model;
        }
        [ProtoContract]
        public class Foo
        {
            [ProtoMember(1)]
            public ArraySegment<byte> Data { get; set; }
        }
    }
}
