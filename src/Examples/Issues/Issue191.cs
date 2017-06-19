using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf.Meta;
using ProtoBuf;

namespace Examples.Issues
{
    
    public class Issue191
    {
        [ProtoContract]
        public class Test
        {
            // Deserializes as null
            [ProtoMember(1)]
            public Dictionary<long, string> dict = new Dictionary<long, string>();


            // Deserializes correctly
            [ProtoMember(2)]
            public Dictionary<long, string> dict2 { get; set; }
            public Test()
            {
                this.dict2 = new Dictionary<long, string>();
            }
        }
        [Fact]
        public void Execute()
        {
            var model = TypeModel.Create();
            model.AutoCompile = false;
            model.Add(typeof(Test), true);

            RoundtripEmptyDictionaryShouldNotNullThem(model, "Runtime");
            model.CompileInPlace();
            RoundtripEmptyDictionaryShouldNotNullThem(model, "CompileInPlace");

            RoundtripEmptyDictionaryShouldNotNullThem(model.Compile(), "Compile");
        }
        public void RoundtripEmptyDictionaryShouldNotNullThem(TypeModel model, string scenario)
        {
            var orig = new Test();
            Assert.NotNull(orig.dict); //, scenario);
            Assert.Equal(0, orig.dict.Count); //, scenario);
            Assert.NotNull(orig.dict2); //, scenario);
            Assert.Equal(0, orig.dict2.Count); //, scenario);

            var clone = (Test)model.DeepClone(orig);
            Assert.NotNull(clone.dict); //, scenario);
            Assert.Equal(0, clone.dict.Count); //, scenario);
            Assert.NotNull(clone.dict2); //, scenario);
            Assert.Equal(0, clone.dict2.Count); //, scenario);

        }
    }
}
