using System;

using ProtoBuf.Meta;

using Xunit;

namespace ProtoBuf.unittest.Serializers
{
    public class UriTests
    {
        public class TypeWithUri
        {
            public Uri Value { get; set; }
        }

        [Theory]
        [InlineData("http://example.com")]
        [InlineData(@"/relative/path/to/file.txt")]
        public void TestUriRuntime(string uriString)
        {
            var model = CreateModel();

            var obj = new TypeWithUri { Value = new Uri(uriString, UriKind.RelativeOrAbsolute) };
            TypeWithUri clone = (TypeWithUri) model.DeepClone(obj);
            Assert.Equal(obj.Value, clone.Value);
        }

        [Theory]
        [InlineData("http://example.com")]
        [InlineData(@"/relative/path/to/file.txt")]
        public void TestUriInPlace(string uriString)
        {
            var model = CreateModel();
            model.CompileInPlace();
            var obj = new TypeWithUri { Value = new Uri(uriString, UriKind.RelativeOrAbsolute) };

            TypeWithUri clone = (TypeWithUri)model.DeepClone(obj);
            
            Assert.Equal(obj.Value, clone.Value);
        }

        [Fact]
        public void TestUriCanCompileFully()
        {
            var model = CreateModel().Compile("TestUriCanCompileFully", "TestUriCanCompileFully.dll");
            PEVerify.Verify("TestUriCanCompileFully.dll");
        }

        [Theory]
        [InlineData("http://example.com")]
        [InlineData(@"/relative/path/to/file.txt")]
        public void TestUriCompiled(string uriString)
        {
            var model = CreateModel().Compile();

            var obj = new TypeWithUri { Value = new Uri(uriString, UriKind.RelativeOrAbsolute) };

            TypeWithUri clone = (TypeWithUri)model.DeepClone(obj);            
            Assert.Equal(obj.Value, clone.Value);
        }


        static RuntimeTypeModel CreateModel()
        {
            var model = TypeModel.Create();
            model.Add(typeof(TypeWithUri), false)
                .Add(1, "Value");
            return model;
        }
    }
}