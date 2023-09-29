using ProtoBuf.Meta;
using ProtoBuf.unittest;
using System;
using System.IO;
using Xunit;
using Xunit.Sdk;

namespace ProtoBuf.Test.Issues
{

    public class Issue1084 // https://github.com/protobuf-net/protobuf-net/issues/1084
    {
        [Fact]
        public void Execute()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add(typeof(WithInternalSetter));
            Verify(model, "Runtime");
            model.CompileInPlace();
            Verify(model, "CompileInPlace");
            // Verify(model.Compile(), "Compile");
            // Verify(PEVerify.CompileAndVerify(model), "Full Compile");

            static void Verify(TypeModel model, string label)
            {
                try
                {
                    using var ms = new MemoryStream();
                    model.Serialize(ms, new WithInternalSetter { Id = 42 });
                    if (!ms.TryGetBuffer(out var segment)) segment = new(ms.ToArray());
                    Assert.Equal("08-2A", BitConverter.ToString(segment.Array, segment.Offset, segment.Count));
                    ms.Position = 0;
                    Assert.Equal(42, model.Deserialize<WithInternalSetter>(ms).Id);

                }
                catch (Exception ex) when (ex is not XunitException)
                {
                    Assert.Fail(label + ":" + ex.Message);
                }
            }
        }



        [ProtoContract]
        public class WithInternalSetter
        {
            [ProtoMember(1)]
            public int Id { get; internal set; }
        }
    }
}