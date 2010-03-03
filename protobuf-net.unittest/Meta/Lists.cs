using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using System.Collections;
using ProtoBuf.Meta;

namespace ProtoBuf.unittest.Meta
{
    [TestFixture]
    public class PocoListTests {
        public class TypeWithLists
        {
            public List<string> ListString { get; set; }
            public IList<string> IListString { get; set; }

            public ArrayList ArrayListProp { get; set; }
            public IList IListProp { get; set; }
        }

        [Test]
        public void AddOnTypedListShouldResolveCorrectly()
        {
            var model = TypeModel.Create("AddOnTypedListShouldResolveCorrectly");
            model.Add(typeof(TypeWithLists), false).Add(1, "ListString");
            Assert.AreEqual(typeof(TypeWithLists), model[typeof(TypeWithLists)][1].ParentType, "ParentType");
            Assert.AreEqual(typeof(string), model[typeof(TypeWithLists)][1].ItemType, "ItemType");
            Assert.AreEqual(typeof(List<string>), model[typeof(TypeWithLists)][1].MemberType, "MemberType");
            Assert.AreEqual(typeof(List<string>), model[typeof(TypeWithLists)][1].DefaultType, "DefaultType");
        }

        [Test]
        public void AddOnTypedIListShouldResolveCorrectly()
        {
            var model = TypeModel.Create("AddOnTypedIListShouldResolveCorrectly");
            model.Add(typeof(TypeWithLists), false).Add(2, "IListString");
            Assert.AreEqual(typeof(TypeWithLists), model[typeof(TypeWithLists)][2].ParentType, "ParentType");
            Assert.AreEqual(typeof(string), model[typeof(TypeWithLists)][2].ItemType, "ItemType");
            Assert.AreEqual(typeof(IList<string>), model[typeof(TypeWithLists)][2].MemberType, "MemberType");
            Assert.AreEqual(typeof(List<string>), model[typeof(TypeWithLists)][2].DefaultType, "DefaultType");
        }

        [Test]
        public void RoundTripTypedList()
        {
            var model = TypeModel.Create("RoundTripTypedList");
            model.Add(typeof(TypeWithLists), false).Add(1, "ListString");
            TypeWithLists obj = new TypeWithLists();
            obj.ListString = new List<string>();
            obj.ListString.Add("abc");
            obj.ListString.Add("def");

            TypeWithLists clone = (TypeWithLists)model.DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.ListString);
            Assert.IsTrue(obj.ListString.SequenceEqual(clone.ListString));

            model.CompileInPlace();
            clone = (TypeWithLists)model.DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.ListString);
            Assert.IsTrue(obj.ListString.SequenceEqual(clone.ListString));

            clone = (TypeWithLists)model.Compile("RoundTripTypedList.dll").DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.ListString);
            Assert.IsTrue(obj.ListString.SequenceEqual(clone.ListString));
        }

        [Test]
        public void RoundTripTypedIList()
        {
            var model = TypeModel.Create("RoundTripTypedIList");
            model.Add(typeof(TypeWithLists), false).Add(2, "IListString");
            TypeWithLists obj = new TypeWithLists();
            obj.IListString = new List<string>();
            obj.IListString.Add("abc");
            obj.IListString.Add("def");

            TypeWithLists clone = (TypeWithLists)model.DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.IListString);
            Assert.IsTrue(obj.IListString.SequenceEqual(clone.IListString));

            model.CompileInPlace();
            clone = (TypeWithLists)model.DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.IListString);
            Assert.IsTrue(obj.IListString.SequenceEqual(clone.IListString));

            clone = (TypeWithLists)model.Compile().DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.IListString);
            Assert.IsTrue(obj.IListString.SequenceEqual(clone.IListString));
        }


        [Test]
        public void RoundTripArrayList()
        {
            var model = TypeModel.Create("RoundTripArrayList");
            model.Add(typeof(TypeWithLists), false).Add(3, "ArrayListProp", typeof(string), null);
            TypeWithLists obj = new TypeWithLists();
            obj.ArrayListProp  = new ArrayList();
            obj.ArrayListProp.Add("abc");
            obj.ArrayListProp.Add("def");

            TypeWithLists clone = (TypeWithLists)model.DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.ArrayListProp);
            Assert.IsTrue(obj.ArrayListProp.Cast<string>().SequenceEqual(clone.ArrayListProp.Cast<string>()));

            model.CompileInPlace();
            clone = (TypeWithLists)model.DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.ArrayListProp);
            Assert.IsTrue(obj.ArrayListProp.Cast<string>().SequenceEqual(clone.ArrayListProp.Cast<string>()));

            clone = (TypeWithLists)model.Compile().DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.ArrayListProp);
            Assert.IsTrue(obj.ArrayListProp.Cast<string>().SequenceEqual(clone.ArrayListProp.Cast<string>()));
        }

        [Test]
        public void RoundTripIList()
        {
            var model = TypeModel.Create("RoundTripIList");
            model.Add(typeof(TypeWithLists), false).Add(4, "IListProp", typeof(string), null);
            TypeWithLists obj = new TypeWithLists();
            obj.IListProp = new ArrayList();
            obj.IListProp.Add("abc");
            obj.IListProp.Add("def");

            TypeWithLists clone = (TypeWithLists)model.DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.IListProp);
            Assert.IsTrue(obj.IListProp.Cast<string>().SequenceEqual(clone.IListProp.Cast<string>()));

            model.CompileInPlace();
            clone = (TypeWithLists)model.DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.IListProp);
            Assert.IsTrue(obj.IListProp.Cast<string>().SequenceEqual(clone.IListProp.Cast<string>()));

            clone = (TypeWithLists)model.Compile().DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.IListProp);
            Assert.IsTrue(obj.IListProp.Cast<string>().SequenceEqual(clone.IListProp.Cast<string>()));
        }
}
}
