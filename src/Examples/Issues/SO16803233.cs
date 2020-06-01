using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Issues
{
    
    public class SO16803233
    {
        public enum Test
        {
            test1 = 0,
            test2
        };
        [Fact]
        public void Test_Vanilla()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            Execute_Vanilla(model, "Runtime");
            model.CompileInPlace();
            Execute_Vanilla(model, "CompileInPlace");
            Execute_Vanilla(model.Compile(), "Compile");
            model.Compile("SO16803233a", "SO16803233a.dll");
            PEVerify.AssertValid("SO16803233a.dll");
        }
        [Fact]
        public void Test_WithLengthPrefix()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            Execute_WithLengthPrefix(model, "Runtime");
            model.CompileInPlace();
            Execute_WithLengthPrefix(model, "CompileInPlace");
            Execute_WithLengthPrefix(model.Compile(), "Compile");
            model.Compile("SO16803233b", "SO16803233b.dll");
            PEVerify.AssertValid("SO16803233b.dll");
        }


#pragma warning disable IDE0060
        void Execute_Vanilla(TypeModel model, string caption)
#pragma warning restore IDE0060
        {
            const Test original = Test.test2;
            using MemoryStream ms = new MemoryStream();
            model.Serialize(ms, original);
            ms.Position = 0;
            Assert.Equal("08-01", BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length));
            Test obj;
#pragma warning disable CS0618
            obj = (Test)model.Deserialize(ms, null, typeof(Test));
#pragma warning restore CS0618

            Assert.Equal(original, obj);
        }

#pragma warning disable IDE0060
        void Execute_WithLengthPrefix(TypeModel model, string caption)
#pragma warning restore IDE0060
        {
            const Test original = Test.test2;
            using MemoryStream ms = new MemoryStream();
            model.SerializeWithLengthPrefix(ms, original, typeof(Test), PrefixStyle.Fixed32, 1);
            ms.Position = 0;
            Assert.Equal("02-00-00-00-08-01", BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length));
            Test obj;
            obj = (Test)model.DeserializeWithLengthPrefix(ms, null, typeof(Test), PrefixStyle.Fixed32, 1);

            Assert.Equal(original, obj);
        }
    }
}
