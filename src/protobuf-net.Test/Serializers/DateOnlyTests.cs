using System;
using Xunit;
using ProtoBuf.Meta;

namespace ProtoBuf.unittest.Serializers
{
#if NET6_0_OR_GREATER
    public class DateOnlyTests
    {
        public class TypeWithDateOnly
        {
            public DateOnly When { get; set; }
        }

        [Fact]
        public void TestDateOnlyRuntime()
        {
            var model = CreateModel();

            var obj = new TypeWithDateOnly { When = DateOnly.FromDateTime(DateTime.Today) };
            TypeWithDateOnly clone = (TypeWithDateOnly)model.DeepClone(obj);
            Assert.Equal(obj.When, clone.When);
        }

        [Fact]
        public void TestDateOnlyInPlace()
        {
            var model = CreateModel();
            model.CompileInPlace();
            var obj = new TypeWithDateOnly { When = DateOnly.FromDateTime(DateTime.Today) };

            TypeWithDateOnly clone = (TypeWithDateOnly)model.DeepClone(obj);

            Assert.Equal(obj.When, clone.When);
        }

        [Fact]
        public void TestDateOnlyCanCompileFully()
        {
            _ = CreateModel().Compile("TestDateOnlyCanCompileFully", "TestDateOnlyCanCompileFully.dll");
            PEVerify.Verify("TestDateOnlyCanCompileFully.dll");
        }

        [Fact]
        public void TestDateOnlyCompiled()
        {
            var model = CreateModel().Compile();

            var obj = new TypeWithDateOnly { When = DateOnly.FromDateTime(DateTime.Today) };

            TypeWithDateOnly clone = (TypeWithDateOnly)model.DeepClone(obj);
            Assert.Equal(obj.When, clone.When);
        }


        static RuntimeTypeModel CreateModel()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(TypeWithDateOnly), false)
                .Add(1, "When");
            return model;
        }
    }
#endif
}
