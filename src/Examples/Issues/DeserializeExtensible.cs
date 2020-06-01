using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    
    public class DeserializeExtensible
    {
        [Fact]
        public void Execute()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            ExecuteImpl(model, "Runtime");
            model.CompileInPlace();
            ExecuteImpl(model, "CompileInPlace");
            ExecuteImpl(model.Compile(), "Compile");
        }

#pragma warning disable IDE0060
        private void ExecuteImpl(TypeModel model, string caption)
#pragma warning restore IDE0060
        {
            var large = new LargeType { Foo = 1, Bar = "abc" };
            SmallType small;
            using(var ms = new MemoryStream())
            {
                model.Serialize(ms, large);
                ms.Position = 0;
#pragma warning disable CS0618
                small = (SmallType) model.Deserialize(ms, null, typeof(SmallType));
#pragma warning restore CS0618
            }
            Assert.NotNull(small); //, caption);
        }
        [ProtoContract]
        public class LargeType {
            [ProtoMember(1)]
            public int Foo {get;set;}

            [ProtoMember(2)]
            public string Bar {get;set;}
        }
        [ProtoContract]
        public class SmallType : Extensible {
            [ProtoMember(3)]
            public string Blab {get;set;}
        }
    }    
}
