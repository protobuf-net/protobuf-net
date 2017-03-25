using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Examples.Issues
{
    [TestFixture]
    public class MissingBaseTypes
    {
        [ProtoContract]
        [ProtoInclude(15, typeof(D))]
        [ProtoInclude(16, typeof(B))]
        [ProtoInclude(17, typeof(C))]

        public class A
        {
            [ProtoMember(1)]
            public int DataA { get; set; }
        }

        [ProtoContract]

        public class B : A
        {
        }

        [ProtoContract]
        public class C : A
        {
        }

        [ProtoContract]
        public class D : A
        {

            [ProtoMember(4)]
            public int DataD { get; set; }


            [ProtoMember(5)]
            public List<C> DataB { get; set; }
        }


        [ProtoContract]
        public class TestCase
        {
            [ProtoMember(10)]
            public D DataD;

            [ProtoMember(11)]
            public List<A> DataA;

        }

        [Test]
        public void Execute()
        {

            var model = TypeModel.Create();
            model.Add(typeof(A), true);
            model.Add(typeof(B), true);
            model.Add(typeof(C), true);
            model.Add(typeof(D), true);
            model.Add(typeof(TestCase), true);

            string s = model.GetSchema(null);

            Assert.IsNull(model[typeof(A)].BaseType, "A");
            Assert.AreSame(model[typeof(A)], model[typeof(B)].BaseType, "B");
            Assert.AreSame(model[typeof(A)], model[typeof(C)].BaseType, "C");
            Assert.AreSame(model[typeof(A)], model[typeof(D)].BaseType, "D");
            Assert.IsNull(model[typeof(TestCase)].BaseType, "TestCase");
        }
    }
}
