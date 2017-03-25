using NUnit.Framework;
using ProtoBuf.Meta;

namespace ProtoBuf.unittest.Meta
{
    [TestFixture]
    public class Interfaces
    {
        public class SomeClass
        {
            public ISomeInterface SomeProperty { get; set; }
        }
        public interface ISomeInterface
        {
            int Foo { get; set; }
        }
        public class SomeClass2 : ISomeInterface
        {
            private int foo;
            int ISomeInterface.Foo
            {
                get { return foo; }
                set { foo = value; }
            }
        }
        [Test]
        public void ExposeInterfaceWithDefaultImplementation()
        {
            var model = TypeModel.Create();
            // note the following sets for the ConstructType for the ISomeInferface, not specifically for Foo
            model.Add(typeof(ISomeInterface), false).Add("Foo").ConstructType = typeof(SomeClass2);
            model.Add(typeof(SomeClass), false).Add("SomeProperty");

            var orig = new SomeClass();
            orig.SomeProperty = new SomeClass2();
            orig.SomeProperty.Foo = 123;
            var clone = (SomeClass)model.DeepClone(orig);
            Assert.AreEqual(123, clone.SomeProperty.Foo);
        }
       

    }

}
