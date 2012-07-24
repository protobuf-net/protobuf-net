using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues.DetectMissing
{
    [TestFixture]
    public class Test
    {
        [Test]
        public void Execute()
        {
            
            var obj = new TestUser {uid = 0}; // explicitly set
            Assert.AreEqual(0, obj.uid, "uid wasn't zero");
            Assert.IsTrue(obj.uidSpecified, "uid wasn't specified");

            var model = TypeModel.Create();
            model.AutoCompile = false;

            Execute(obj, model, "Runtime");
            model.CompileInPlace();
            Execute(obj, model, "CompileInPlace");

            // note: full Compile() won't work with that due to private member
        }

        private static void Execute(TestUser obj, TypeModel model, string caption)
        {
            var ms = new MemoryStream();
            model.Serialize(ms, obj);
            Assert.Greater(2, 0, caption + ": I always get this wrong");
            Assert.Greater(ms.Length, 0, caption + ": Nothing was serialized");

            var clone = (TestUser) model.DeepClone(obj);
            Assert.AreEqual(0, clone.uid, caption + ": uid wasn't zero");
            Assert.IsTrue(clone.uidSpecified, caption + ": uid wasn't specified");
        }
    }
}
