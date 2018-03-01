using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    
    public class Issue218
    {
        [ProtoContract]
        public class Test
        {
            [ProtoMember(1)]
            public byte[] BackgroundImageToUpload { get; set; }

            [ProtoMember(2)]
            public string Title { get; set; }
        }
        [Fact]
        public void Execute()
        {

            var typeModel = TypeModel.Create();
            typeModel.AutoCompile = true;
            var obj = new Test() {Title = "MyTitle", BackgroundImageToUpload = new byte[0]};

            var clone = (Test)typeModel.DeepClone(obj);
            Assert.NotNull(clone.BackgroundImageToUpload); //, "Runtime");
            Assert.Empty(clone.BackgroundImageToUpload); //, "Runtime");
            Assert.Equal("MyTitle", clone.Title); //, "Runtime");

            typeModel.CompileInPlace();
            clone = (Test)typeModel.DeepClone(obj);
            Assert.NotNull(clone.BackgroundImageToUpload); //, "CompileInPlace");
            Assert.Empty(clone.BackgroundImageToUpload); //, "CompileInPlace");
            Assert.Equal("MyTitle", clone.Title); //, "CompileInPlace");

            clone = (Test)typeModel.Compile().DeepClone(obj);
            Assert.NotNull(clone.BackgroundImageToUpload); //, "Compile");
            Assert.Empty(clone.BackgroundImageToUpload); //, "Compile");
            Assert.Equal("MyTitle", clone.Title); //, "Compile");

        }
    }
}
