using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    [TestFixture]
    public class SO9408133
    {
        [ProtoContract] public class Ship
        {
            [ProtoMember(1)]
            public int Foo { get; set; }
        }
        [ProtoContract] public class SomeType
        {
            [ProtoMember(1)]
            public string Bar { get; set; }
        }

        [ProtoContract]
        [ProtoInclude(1, typeof(SomeNodeType)), ProtoInclude(2, typeof(SomeOtherType))]
        [ProtoInclude(3, typeof(ResourceNode<Ship>)), ProtoInclude(4, typeof(ResourceNode<SomeType>))]
        public class Node { }
        [ProtoContract] public class SomeNodeType : Node { }
        [ProtoContract] public class SomeOtherType : Node { }

        [ProtoContract]
        [ProtoInclude(1, typeof(ShipResource)), ProtoInclude(1, typeof(SomeResource))]
        public class ResourceNode<T> : Node { }
        [ProtoContract] public class ShipResource : ResourceNode<Ship>
        {
            [ProtoMember(1)]
            public Ship Value { get; set; }
        }
        [ProtoContract] public class SomeResource : ResourceNode<SomeType>
        {
            [ProtoMember(1)]
            public SomeType Value { get; set; }
        }

        [Test]
        public void TestImplicitSetup()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;

            var obj1 = new ShipResource { Value = new Ship { Foo = 123 } };
            var obj2 = new SomeResource { Value = new SomeType { Bar = "abc" } };

            Test(model, obj1, obj2, "Runtime");

            model.Compile("SO9408133_TestImplicitSetup", "SO9408133_TestImplicitSetup.dll");
            PEVerify.AssertValid("SO9408133_TestImplicitSetup.dll");

            model.CompileInPlace();
            Test(model, obj1, obj2, "CompileInPlace");
            Test(model.Compile(), obj1, obj2, "Compile");
        }
        [Test, ExpectedException(typeof(ArgumentException), ExpectedMessage = "SomeType is not a valid sub-type of", MatchType = MessageMatch.Contains)]
        public void TestStupidSetup()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add(typeof(ResourceNode<Ship>), false).AddSubType(1, typeof(ShipResource));
            // I did this to myself... sigh
            model.Add(typeof(ResourceNode<SomeType>), false).AddSubType(1, typeof(SomeType));
        }
        [Test]
        public void TestExplicitSetup()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add(typeof (ResourceNode<Ship>), false).AddSubType(1, typeof (ShipResource));
            model.Add(typeof (ResourceNode<SomeType>), false).AddSubType(1, typeof (SomeResource));

            var obj1 = new ShipResource { Value = new Ship { Foo = 123} };
            var obj2 = new SomeResource { Value = new SomeType { Bar = "abc" } };

            Test(model, obj1, obj2, "Runtime");

            model.Compile("SO9408133_TestExplicitSetup", "SO9408133_TestExplicitSetup.dll");
            PEVerify.AssertValid("SO9408133_TestExplicitSetup.dll");

            model.CompileInPlace();
            Test(model, obj1, obj2, "CompileInPlace");
            Test(model.Compile(), obj1, obj2, "Compile");
        }

        private void Test(TypeModel model, ShipResource obj1, SomeResource obj2, string caption)
        {
            try
            {
                var clone1 = (ShipResource) model.DeepClone(obj1);
                var clone2 = (SomeResource) model.DeepClone(obj2);

                Assert.AreEqual(obj1.Value.Foo, clone1.Value.Foo, caption + ":Foo");
                Assert.AreEqual(obj2.Value.Bar, clone2.Value.Bar, caption + ":Bar");
            } catch(Exception ex)
            {
                throw new Exception(caption + ":" + ex.Message, ex);
            }
        }

    }
}
