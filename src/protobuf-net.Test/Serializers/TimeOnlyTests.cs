using System;
using Xunit;
using ProtoBuf.Meta;

namespace ProtoBuf.unittest.Serializers
{
#if NET6_0_OR_GREATER
    public class TimeOnlyTests
    {
        public class TypeWithTimeOnly
        {
            public TimeOnly When { get; set; }
        }

        [Fact]
        public void TestTimeOnlyRuntime()
        {
            var model = CreateModel();

            var obj = new TypeWithTimeOnly { When = TimeOnly.FromDateTime(DateTime.Today) };
            TypeWithTimeOnly clone = (TypeWithTimeOnly)model.DeepClone(obj);
            Assert.Equal(obj.When, clone.When);
        }

        [Fact]
        public void TestTimeOnlyInPlace()
        {
            var model = CreateModel();
            model.CompileInPlace();
            var obj = new TypeWithTimeOnly { When = TimeOnly.FromDateTime(DateTime.Today) };

            TypeWithTimeOnly clone = (TypeWithTimeOnly)model.DeepClone(obj);

            Assert.Equal(obj.When, clone.When);
        }

        [Fact]
        public void TestTimeOnlyCanCompileFully()
        {
            _ = CreateModel().Compile("TestTimeOnlyCanCompileFully", "TestTimeOnlyCanCompileFully.dll");
            PEVerify.Verify("TestTimeOnlyCanCompileFully.dll");
        }

        [Fact]
        public void TestTimeOnlyCompiled()
        {
            var model = CreateModel().Compile();

            var obj = new TypeWithTimeOnly { When = TimeOnly.FromDateTime(DateTime.Today) };

            TypeWithTimeOnly clone = (TypeWithTimeOnly)model.DeepClone(obj);
            Assert.Equal(obj.When, clone.When);
        }


        static RuntimeTypeModel CreateModel()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(TypeWithTimeOnly), false)
                .Add(1, "When");
            return model;
        }
    }
#endif
}
