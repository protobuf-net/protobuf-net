using NUnit.Framework;
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
    [TestFixture]
    public class SO16803233
    {
        public enum Test
        {
            test1 = 0,
            test2
        };
        [Test]
        public void Test_Vanilla()
        {
            var model = TypeModel.Create();
            model.AutoCompile = false;
            Execute_Vanilla(model, "Runtime");
            model.CompileInPlace();
            Execute_Vanilla(model, "CompileInPlace");
            Execute_Vanilla(model.Compile(), "Compile");
            model.Compile("SO16803233a", "SO16803233a.dll");
            PEVerify.AssertValid("SO16803233a.dll");
        }
        [Test]
        public void Test_WithLengthPrefix()
        {
            var model = TypeModel.Create();
            model.AutoCompile = false;
            Execute_WithLengthPrefix(model, "Runtime");
            model.CompileInPlace();
            Execute_WithLengthPrefix(model, "CompileInPlace");
            Execute_WithLengthPrefix(model.Compile(), "Compile");
            model.Compile("SO16803233b", "SO16803233b.dll");
            PEVerify.AssertValid("SO16803233b.dll");
        }



        void Execute_Vanilla(TypeModel model, string caption)
        {
            const Test original = Test.test2;
            using (MemoryStream ms = new MemoryStream())
            {
                model.Serialize(ms, original);
                ms.Position = 0;
                Assert.AreEqual("08-01", BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length));
                Test obj;
                obj = (Test)model.Deserialize(ms, null, typeof(Test));

                Assert.AreEqual(original, obj);
            }
        }
        void Execute_WithLengthPrefix(TypeModel model, string caption)
        {
            const Test original = Test.test2;
            using (MemoryStream ms = new MemoryStream())
            {
                model.SerializeWithLengthPrefix(ms, original, typeof(Test), PrefixStyle.Fixed32, 1);
                ms.Position = 0;
                Assert.AreEqual("02-00-00-00-08-01", BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length));
                Test obj;
                obj = (Test)model.DeserializeWithLengthPrefix(ms, null, typeof(Test), PrefixStyle.Fixed32, 1);

                Assert.AreEqual(original, obj);
            }
        }
    }
}
