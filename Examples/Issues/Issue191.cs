using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf.Meta;
using ProtoBuf;

namespace Examples.Issues
{
    [TestFixture]
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
        [Test]
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
            Assert.IsNotNull(orig.dict, scenario);
            Assert.AreEqual(0, orig.dict.Count, scenario);
            Assert.IsNotNull(orig.dict2, scenario);
            Assert.AreEqual(0, orig.dict2.Count, scenario);

            var clone = (Test)model.DeepClone(orig);
            Assert.IsNotNull(clone.dict, scenario);
            Assert.AreEqual(0, clone.dict.Count, scenario);
            Assert.IsNotNull(clone.dict2, scenario);
            Assert.AreEqual(0, clone.dict2.Count, scenario);

        }
    }
}
