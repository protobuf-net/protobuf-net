using NUnit.Framework;
using System.Collections.Generic;
using ProtoBuf;
using ProtoBuf.Meta;


namespace Examples.Issues
{
    [TestFixture]
    public class SO9575661
    {
 
        [ProtoContract(IgnoreListHandling=true)]
        class FileTree : List<MyFileInfo>
        {
            [ProtoMember(1)]
            public string Bar { get; set; }
        }
        [ProtoContract]
        class MyFileInfo
        {
            private MyFileInfo(int evil) {}
            public static MyFileInfo Create() { return new MyFileInfo(1);}
        }

        [ProtoContract]
        class MyRandomStuff
        {
            [ProtoMember(1)]
            public FileTree Foo { get; set; }
        }

        [Test]
        public void Execute()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;

            RunTest(model, "Runtime");
            model.CompileInPlace();
            RunTest(model, "CompileInPlace");
            RunTest(model.Compile(), "Compile");
        }

        private void RunTest(TypeModel model, string mode)
        {
            var obj = new MyRandomStuff { Foo = new FileTree { Bar = "abc" } };
            obj.Foo.Add(MyFileInfo.Create()); // whack something in the list too, but don't expect this to serialize
            var clone = Serializer.DeepClone(obj);
            Assert.AreEqual(0, clone.Foo.Count, mode);
            Assert.AreEqual("abc", clone.Foo.Bar, mode);
        }
    }
}
