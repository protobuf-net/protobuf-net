using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using ProtoBuf;
using ProtoBuf.Meta;
using Xunit;

namespace Examples
{
    public sealed class DynamicIFormatter
#if !COREFX
        : IFormatter
#endif
    {
        private readonly TypeModel model;
        public DynamicIFormatter(TypeModel model)
        {
            this.model = model ?? RuntimeTypeModel.Default;
        }
        public DynamicIFormatter() : this(null){}
        public object Deserialize(Stream source)
        {
            var shell = (DynamicShell) model.Deserialize(source, null, typeof(DynamicShell), -1, Context);
            return shell.Value;
        }
        public void Serialize(Stream destination, object graph)
        {
            var shell = new DynamicShell { Value = graph };
            model.Serialize(destination, shell, Context);
        }
        [ProtoContract]
        class DynamicShell
        {
            [ProtoMember(1, DynamicType = true)]
            public object Value { get; set; }
        }
        public StreamingContext Context { get; set; }
#if !COREFX
        SerializationBinder IFormatter.Binder { get; set; }
        ISurrogateSelector IFormatter.SurrogateSelector { get; set; }
#endif
    }
    
    public class TestDynamicFormatter
    {
        [Fact]
        public void Execute()
        {
            var formatter = new DynamicIFormatter();
            using(var ms = new MemoryStream())
            {
                formatter.Serialize(ms, new Foo { Bar = 12345 });
                ms.Position = 0;
                Foo clone = (Foo) formatter.Deserialize(ms);
                Assert.Equal(12345, clone.Bar);
            }
        }
        [ProtoContract]
        class Foo
        {
            [ProtoMember(1)]
            public int Bar { get; set; }
        }
    }
}
