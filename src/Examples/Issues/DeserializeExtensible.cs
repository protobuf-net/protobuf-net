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
            var model = TypeModel.Create();
            model.AutoCompile = false;
            ExecuteImpl(model, "Runtime");
            model.CompileInPlace();
            ExecuteImpl(model, "CompileInPlace");
            ExecuteImpl(model.Compile(), "Compile");
        }
        private void ExecuteImpl(TypeModel model, string caption)
        {
            var large = new LargeType { Foo = 1, Bar = "abc" };
            SmallType small;
            using(var ms = new MemoryStream())
            {
                model.Serialize(ms, large);
                ms.Position = 0;
                small = (SmallType) model.Deserialize(ms, null, typeof(SmallType));
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
