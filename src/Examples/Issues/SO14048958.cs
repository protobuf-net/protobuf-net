using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.IO;

namespace Examples.Issues
{
    
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
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            Execute(model, "Runtime");
            model.CompileInPlace();
            Execute(model, "CompileInPlace");
            Execute(model.Compile(), "Compile");
        }

#pragma warning disable IDE0060
        public void Execute(TypeModel model, string caption)
#pragma warning restore IDE0060
        {
            var obj = new Foo { Status = Status.All };
            using var ms = new MemoryStream();
            model.Serialize(ms, obj);
            ms.Position = 0;
            Assert.Equal(3, ms.Length);
#pragma warning disable CS0618
            var clone = (Foo)model.Deserialize(ms, null, typeof(Foo));
#pragma warning restore CS0618
            Assert.Equal(Status.All, clone.Status);
        }
    }
}
