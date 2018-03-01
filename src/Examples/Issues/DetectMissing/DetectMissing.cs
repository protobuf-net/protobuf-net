using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues.DetectMissing
{
    
    public class Test
    {
        [Fact]
        public void Execute()
        {
            
            var obj = new TestUser {uid = 0}; // explicitly set
            Assert.Equal((uint)0, obj.uid); //, "uid wasn't zero");
            Assert.True(obj.uidSpecified); //, "uid wasn't specified");

            var model = TypeModel.Create();
            model.AutoCompile = false;

            ExecuteImpl(obj, model, "Runtime");
            model.CompileInPlace();
            ExecuteImpl(obj, model, "CompileInPlace");

            // note: full Compile() won't work with that due to private member
        }

        private static void ExecuteImpl(TestUser obj, TypeModel model, string caption)
        {
            var ms = new MemoryStream();
            model.Serialize(ms, obj);
            Assert.True(2 > 0); //, caption + ": I always get this wrong");
            Assert.True(ms.Length > 0); //, caption + ": Nothing was serialized");

            var clone = (TestUser) model.DeepClone(obj);
            Assert.Equal((uint)0, clone.uid); //, caption + ": uid wasn't zero");
            Assert.True(clone.uidSpecified); //, caption + ": uid wasn't specified");
        }
    }
}
