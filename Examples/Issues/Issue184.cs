using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using ProtoBuf.Meta;
using System.IO;
using ProtoBuf;
using NUnit.Framework;

namespace Examples.Issues
{
    [TestFixture]
    public class Issue184
    {
        [Test, ExpectedException(typeof(ArgumentException), ExpectedMessage="IEnumerable[<T>] data cannot be used as a meta-type unless an Add method can be resolved")]
        public void CantCreateUnusableEnumerableMetaType()
        {
            var model = TypeModel.Create();
            model.Add(typeof(IEnumerable<int>), false);
        }
        [Test, ExpectedException(typeof(ArgumentException), ExpectedMessage = "Data of this type has inbuilt behaviour, and cannot be added to a model in this way: System.Decimal")]
        public void CantCreateMetaTypeForInbuilt()
        {
            var model = TypeModel.Create();
            model.Add(typeof(decimal), false);
        }
        [Test, ExpectedException(typeof(ArgumentException), ExpectedMessage = "Repeated data (a list, collection, etc) has inbuilt behaviour and cannot be subclassed")]
        public void CantSubclassLists()
        {
            var model = TypeModel.Create();
            model.Add(typeof(IList<int>), false).AddSubType(5, typeof(List<int>));
        }        
    }
}
