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
        [Test, ExpectedException(typeof(ArgumentException), ExpectedMessage = "IEnumerable[<T>] data cannot be used as a meta-type unless an Add method can be resolved")]
        public void CantCreateUnusableEnumerableMetaType()
        {
            var model = TypeModel.Create();
            model.Add(typeof(IEnumerable<int>), false);
            model.CompileInPlace();
        }
        [Test, ExpectedException(typeof(ArgumentException), ExpectedMessage = "Data of this type has inbuilt behaviour, and cannot be added to a model in this way: System.Decimal")]
        public void CantCreateMetaTypeForInbuilt()
        {
            var model = TypeModel.Create();
            model.Add(typeof(decimal), false);
            model.CompileInPlace();
        }
        [Test, ExpectedException(typeof(ArgumentException), ExpectedMessage = "Repeated data (a list, collection, etc) has inbuilt behaviour and cannot be subclassed")]
        public void CantSubclassLists()
        {
            var model = TypeModel.Create();
            model.Add(typeof(IList<int>), false).AddSubType(5, typeof(List<int>));
            model[typeof (IList<int>)].UseConstructor = false;
            model.CompileInPlace();
        }
        [Test, ExpectedException(typeof(ArgumentException), ExpectedMessage = "Repeated data (a list, collection, etc) has inbuilt behaviour and cannot be used as a subclass")]
        public void ListAsSubclass()
        {
            var m = TypeModel.Create();
            m.Add(typeof(IMobileObject), false).AddSubType(1, typeof(A)).AddSubType(2, typeof(MobileList<int>));
            m.CompileInPlace();
        }
        [Test, ExpectedException(typeof(ArgumentException), ExpectedMessage = "Repeated data (a list, collection, etc) has inbuilt behaviour and cannot use a surrogate")]
        public void CantSurrogateLists()
        {
            var model = TypeModel.Create();
            model.Add(typeof(IList<int>), false).SetSurrogate(typeof(InnocentType));
            model.CompileInPlace();
        }
        [Test, ExpectedException(typeof(ArgumentException), ExpectedMessage = "Repeated data (a list, collection, etc) has inbuilt behaviour and cannot be used as a surrogate")]
        public void ListAsSurrogate()
        {
            var model = TypeModel.Create();
            model.Add(typeof(IMobileObject), false).SetSurrogate(typeof(MobileList<int>));
            model.CompileInPlace();
        }


        public interface IMobileObject { }
        public class InnocentType // nothing that looks like a list
        {
            
        }
        public class MobileList<T> : List<T>, IMobileObject
        {
            public override bool Equals(object obj) { return this.SequenceEqual((IEnumerable<T>)obj); }
            public override int GetHashCode()
            {
                return 0; // not being used in a dictionary - to heck with it
            }
        }
        [ProtoContract]
        public class A : IMobileObject
        {
            [ProtoMember(1)]
            public int X { get; set; }
            public override bool Equals(object obj) { return ((A)obj).X == X; }
            public override int GetHashCode()
            {
                return 0; // not being used in a dictionary - to heck with it
            }
            public override string ToString()
            {
                return X.ToString();
            }
        }
        [ProtoContract]
        public class B
        {
            [ProtoMember(1)]
            public List<IMobileObject> Objects { get; set; }
        }


    }
}
