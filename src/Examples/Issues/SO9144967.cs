using System.Linq;
using System.Net;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    
    public class SO9144967
    {
        [ProtoContract]
        public class HasBlobs
        {
            [ProtoMember(1)]
            public byte[] Foo { get; set; }
            [ProtoMember(2, OverwriteList = true)]
            public byte[] Bar{ get; set; }

            public HasBlobs()
            {
                Foo = new byte[] { 1, 2, 3};
                Bar = new byte[] { 4, 5, 6 };
            }
        }

        
        [Fact]
        public void Execute()
        {
            var obj = new HasBlobs {Foo = new byte[] {7, 8}, Bar = new byte[] {8, 9}};

            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;


            var clone = (HasBlobs)model.DeepClone(obj);
            Assert.True(clone.Foo.SequenceEqual(new byte[] { 1, 2, 3, 7, 8}), "Runtime Foo");
            Assert.True(clone.Bar.SequenceEqual(new byte[] { 8, 9 }), "Runtime Bar");
            model.CompileInPlace();
            clone = (HasBlobs)model.DeepClone(obj);
            Assert.True(clone.Foo.SequenceEqual(new byte[] { 1, 2, 3, 7, 8 }), "CompileInPlace Foo");
            Assert.True(clone.Bar.SequenceEqual(new byte[] { 8, 9 }), "CompileInPlace Bar");
            clone = (HasBlobs)model.Compile().DeepClone(obj);
            Assert.True(clone.Foo.SequenceEqual(new byte[] { 1, 2, 3, 7, 8 }), "Compile Foo");
            Assert.True(clone.Bar.SequenceEqual(new byte[] { 8, 9 }), "Compile Bar");
        }
}
}
