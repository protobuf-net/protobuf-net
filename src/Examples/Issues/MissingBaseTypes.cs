using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Examples.Issues
{
    
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

        [Fact]
        public void Execute()
        {

            var model = RuntimeTypeModel.Create();
            model.Add(typeof(A), true);
            model.Add(typeof(B), true);
            model.Add(typeof(C), true);
            model.Add(typeof(D), true);
            model.Add(typeof(TestCase), true);

            _ = model.GetSchema(null, ProtoSyntax.Default);

            Assert.Null(model[typeof(A)].BaseType); //, "A");
            Assert.Same(model[typeof(A)], model[typeof(B)].BaseType); //, "B");
            Assert.Same(model[typeof(A)], model[typeof(C)].BaseType); //, "C");
            Assert.Same(model[typeof(A)], model[typeof(D)].BaseType); //, "D");
            Assert.Null(model[typeof(TestCase)].BaseType); //, "TestCase");
        }
    }
}
