using System;
using Xunit;
using ProtoBuf.Meta;
using System.IO;
using ProtoBuf.unittest.Meta;

namespace ProtoBuf.unittest.Serializers
{

    
    public class DateTimeTests
    {
        public class TypeWithDateTime
        {
            public DateTime When { get; set; }
        }
        [Fact]
        public void TestDateTimeRuntime()
        {
            var model = CreateModel();

            var obj = new TypeWithDateTime { When = DateTime.Today };
            TypeWithDateTime clone = (TypeWithDateTime) model.DeepClone(obj);
            Assert.Equal(obj.When, clone.When);
        }


        [Fact]
        public void TestDateTimeInPlace()
        {
            var model = CreateModel();
            model.CompileInPlace();
            var obj = new TypeWithDateTime { When = DateTime.Today };
            
            TypeWithDateTime clone = (TypeWithDateTime)model.DeepClone(obj);
            
            Assert.Equal(obj.When, clone.When);
        }

        [Fact]
        public void TestDateTimeCanCompileFully()
        {
            _ = CreateModel().Compile("TestDateTimeCanCompileFully", "TestDateTimeCanCompileFully.dll");
            PEVerify.Verify("TestDateTimeCanCompileFully.dll");
        }

        [Fact]
        public void TestDateTimeCompiled()
        {
            var model = CreateModel().Compile();

            var obj = new TypeWithDateTime { When = DateTime.Today };

            TypeWithDateTime clone = (TypeWithDateTime)model.DeepClone(obj);            
            Assert.Equal(obj.When, clone.When);
        }


        static RuntimeTypeModel CreateModel()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(TypeWithDateTime), false)
                .Add(1, "When");
            return model;
        }
    }
    
}
