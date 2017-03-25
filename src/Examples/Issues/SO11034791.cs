using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    [TestFixture]
    public class SO11034791
    {
        [Test]
        public void Execute()
        {
            RuntimeTypeModel model = RuntimeTypeModel.Create();
         
            var original = new Custom<string> { "C#" };
            var clone = (Custom<string>)model.DeepClone(original);
            Assert.AreEqual(1, clone.Count);
            Assert.AreEqual("C#", clone.Single());
        }
        public class Custom<T> : List<T> { }
    }

}
