using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Examples.Issues
{
    [TestFixture]
    public class Issue184
    {
        [Test]
        public void CanCreateUsableEnumerableMetaType()
        {
            var model = TypeModel.Create();
            model.Add(typeof(IEnumerable<int>), false);
            model.CompileInPlace();
        }
        [Test]
        public void CantCreateMetaTypeForInbuilt()
        {
            Program.ExpectFailure<ArgumentException>(() =>
            {
                var model = TypeModel.Create();
                model.Add(typeof(decimal), false);
                model.CompileInPlace();
            }, "Data of this type has inbuilt behaviour, and cannot be added to a model in this way: System.Decimal");
        }
        [Test]
        public void CantSubclassLists()
        {
            Program.ExpectFailure<ArgumentException>(() =>
            {
                var model = TypeModel.Create();
                model.Add(typeof(IList<int>), false).AddSubType(5, typeof(List<int>));
                model[typeof(IList<int>)].UseConstructor = false;
                model.CompileInPlace();
            }, "Repeated data (a list, collection, etc) has inbuilt behaviour and cannot be subclassed");
        }
        [Test]
        public void ListAsSubclass()
        {
            Program.ExpectFailure<ArgumentException>(() =>
            {
                var m = TypeModel.Create();
                m.Add(typeof(IMobileObject), false).AddSubType(1, typeof(A)).AddSubType(2, typeof(MobileList<int>));
                m.CompileInPlace();
            }, "Repeated data (a list, collection, etc) has inbuilt behaviour and cannot be used as a subclass");
        }
        [Test]
        public void CantSurrogateLists()
        {
            Program.ExpectFailure<ArgumentException>(() =>
            {
                var model = TypeModel.Create();
                model.Add(typeof(IList<int>), false).SetSurrogate(typeof(InnocentType));
                model.CompileInPlace();
            }, "Repeated data (a list, collection, etc) has inbuilt behaviour and cannot use a surrogate");
        }
        [Test]
        public void ListAsSurrogate()
        {
            Program.ExpectFailure<ArgumentException>(() =>
            {
                var model = TypeModel.Create();
                model.Add(typeof(IMobileObject), false).SetSurrogate(typeof(MobileList<int>));
                model.CompileInPlace();
            }, "Repeated data (a list, collection, etc) has inbuilt behaviour and cannot be used as a surrogate");
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
