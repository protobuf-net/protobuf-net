using System.Net;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples
{
    [ProtoContract]
    public class WithIP
    {
        [ProtoMember(1)]
        public IPAddress Address { get; set; }
    }


    public class Parseable
    {
        static RuntimeTypeModel CreateModel()
        {
            var model = RuntimeTypeModel.Create();
            model.AllowParseableTypes = true;
            model.AutoCompile = false;
            model.Add(typeof(WithIP));
            return model;
        }
        [Fact]
        public void TestIPAddess_Runtime()
            => Test(CreateModel());

        [Fact]
        public void TestIPAddess_CompileInPlace()
        {
            var model = CreateModel();
            model.CompileInPlace();
            Test(model);
        }

        [Fact]
        public void TestIPAddess_Compile()
            => Test(CreateModel().Compile());

        [Fact]
        public void TestIPAddess_FullCompile()
        {
            var model = CreateModel();
            model.Compile("TestIPAddess_FullCompile", "TestIPAddess_FullCompile.dll");
            PEVerify.AssertValid("TestIPAddess_FullCompile.dll");
            Test(model);
        }

        static void Test(TypeModel model)
        {
            WithIP obj = new WithIP { Address = IPAddress.Parse("100.90.80.100") },
            clone = (WithIP)model.DeepClone(obj);

            Assert.Equal(obj.Address, clone.Address);

            obj.Address = null;
            clone = (WithIP)model.DeepClone(obj);

            Assert.Null(obj.Address); //, "obj");
            Assert.Null(clone.Address); //, "clone");
        }
    }
}
