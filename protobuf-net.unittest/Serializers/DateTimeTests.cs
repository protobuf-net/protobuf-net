using System;
using NUnit.Framework;
using ProtoBuf.Meta;
using System.IO;
using ProtoBuf.unittest.Meta;

namespace ProtoBuf.unittest.Serializers
{

    [TestFixture]
    public class DateTimeTests
    {
        public class TypeWithDateTime
        {
            public DateTime When { get; set; }
        }
        [Test]
        public void TestDateTimeRuntime()
        {
            var model = CreateModel();

            var obj = new TypeWithDateTime { When = DateTime.Today };
            TypeWithDateTime clone = (TypeWithDateTime) model.DeepClone(obj);
            Assert.AreEqual(obj.When, clone.When);
        }


        [Test]
        public void TestDateTimeInPlace()
        {
            var model = CreateModel();
            model.CompileInPlace();
            var obj = new TypeWithDateTime { When = DateTime.Today };
            
            TypeWithDateTime clone = (TypeWithDateTime)model.DeepClone(obj);
            
            Assert.AreEqual(obj.When, clone.When);
        }


        [Test]
        public void TestDateTimeCanCompileFully()
        {
            var model = CreateModel().Compile("TestDateTimeCanCompileFully", "TestDateTimeCanCompileFully.dll");
            PocoListTests.VerifyPE("TestDateTimeCanCompileFully.dll");
        }
        [Test]
        public void TestDateTimeCompiled()
        {
            var model = CreateModel().Compile();

            var obj = new TypeWithDateTime { When = DateTime.Today };

            TypeWithDateTime clone = (TypeWithDateTime)model.DeepClone(obj);            
            Assert.AreEqual(obj.When, clone.When);
        }


        static RuntimeTypeModel CreateModel()
        {
            var model = TypeModel.Create();
            model.Add(typeof(TypeWithDateTime), false)
                .Add(1, "When");
            return model;
        }
    }
    
}
