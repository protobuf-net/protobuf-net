using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.IO;

namespace Examples.Issues
{
    [TestFixture]
    public class SO14048958
    {
        [Flags]
        public enum Status : byte
        {
            None = 0,
            Alpha = 1,
            Beta = 8,
            Gamma = 16,
            Delta = 32,
            Epsilon = 64,
            Zeta = 132,
            All = 255,
        }

        [ProtoContract]
        public class Foo
        {
            [ProtoMember(1)]
            public Status Status { get; set; }
        }

        public void Execute()
        {
            var model = TypeModel.Create();
            model.AutoCompile = false;
            Execute(model, "Runtime");
            model.CompileInPlace();
            Execute(model, "CompileInPlace");
            Execute(model.Compile(), "Compile");
        }
        public void Execute(TypeModel model, string caption)
        {
            var obj = new Foo { Status = Status.All };
            using(var ms = new MemoryStream())
            {
                model.Serialize(ms, obj);
                ms.Position = 0;
                Assert.AreEqual(3, ms.Length);
                var clone = (Foo)model.Deserialize(ms, null, typeof(Foo));
                Assert.AreEqual(Status.All, clone.Status);
            }
        }
    }
}
