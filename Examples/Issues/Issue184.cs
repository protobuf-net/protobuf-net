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


        public interface IMobileObject { }
        public class MobileList<T> : List<T>, IMobileObject
        {
            public override bool Equals(object obj) { return this.SequenceEqual((IEnumerable<T>)obj); }
        }
        [ProtoContract]
        public class A : IMobileObject
        {
            [ProtoMember(1)]
            public int X { get; set; }
            public override bool Equals(object obj) { return ((A)obj).X == X; }
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
        //[Test]
        //public void Execute()
        //{
        //    var m = TypeModel.Create();
        //    m.AutoCompile = false;                 
        //    m.Add(typeof(IMobileObject), false).AddSubType(1, typeof(A)).AddSubType(2, typeof(MobileList<int>));

        //    TestListAsSubclass(m, "Runtime");

        //    m.CompileInPlace();
        //    TestListAsSubclass(m, "CompileInPlace");

        //    TestListAsSubclass(m.Compile(), "Compile");

        //    m.Compile("Issue184", "Issue184.dll");
        //    PEVerify.AssertValid("Issue184.dll");
        //}

        //private static void TestListAsSubclass(TypeModel m, string caption)
        //{
        //    var b = new B { Objects = new List<IMobileObject> { new A { X = 3 }, new A { X = 17 }, new MobileList<int> { 3, 7 } } };
        //    using (var ms = new MemoryStream())
        //    {
        //        m.Serialize(ms, b);
        //        ms.Position = 0;
        //        var b2 = (B)m.Deserialize(ms, null, typeof(B));
        //        Assert.AreEqual(3, b2.Objects.Count, caption);
        //        Assert.AreEqual(b.Objects[0], b2.Objects[0], caption);
        //        Assert.AreEqual(b.Objects[1], b2.Objects[1], caption);
        //        Assert.AreEqual(b.Objects[2], b2.Objects[2], caption);
        //    }
        //}
    }
}
