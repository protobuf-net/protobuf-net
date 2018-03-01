using System.Collections.Generic;
using System.Linq;
using Xunit;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    
    public class SO11034791
    {
        [Fact]
        public void Execute()
        {
            RuntimeTypeModel model = RuntimeTypeModel.Create();
         
            var original = new Custom<string> { "C#" };
            var clone = (Custom<string>)model.DeepClone(original);
            Assert.Single(clone);
            Assert.Equal("C#", clone.Single());
        }
        public class Custom<T> : List<T> { }
    }

}
