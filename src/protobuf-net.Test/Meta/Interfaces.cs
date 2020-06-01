using Xunit;
using ProtoBuf.Meta;

namespace ProtoBuf.unittest.Meta
{
    
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
        [Fact]
        public void ExposeInterfaceWithDefaultImplementation()
        {
            var model = RuntimeTypeModel.Create();
            // note the following sets for the ConstructType for the ISomeInferface, not specifically for Foo
            model.Add(typeof(ISomeInterface), false).Add("Foo").ConstructType = typeof(SomeClass2);
            model.Add(typeof(SomeClass), false).Add("SomeProperty");

            var orig = new SomeClass
            {
                SomeProperty = new SomeClass2()
            };
            orig.SomeProperty.Foo = 123;
            var clone = (SomeClass)model.DeepClone(orig);
            Assert.Equal(123, clone.SomeProperty.Foo);
        }
       

    }

}
