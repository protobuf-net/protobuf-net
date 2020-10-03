using System;
using System.Runtime.InteropServices;
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

        [SkippableTheory]
        [InlineData("http://example.com", UriKind.Absolute)]
        [InlineData("http://example.com/path/to/resource", UriKind.Absolute)]
        [InlineData("http://example.com/path/to/resource with spaces/", UriKind.Absolute)]
        [InlineData("http://example.com/path/to/resource%20with%20spaces%20encoded", UriKind.Absolute)]
        [InlineData("http://example.com/withquerystring?param1=1&param2=second", UriKind.Absolute)]
        [InlineData("http://example.com/withfragment?param=test#anchorname", UriKind.Absolute)]
        [InlineData("/relative/path/to/file.txt", UriKind.Relative, true)]
        [InlineData("/relative/path/to/file with spaces.txt", UriKind.Relative, true)]
        [InlineData("/relative/path/to/file%20with%20spaces%20encoded.txt", UriKind.Relative, true)]
        public void TestUriRuntime(string uriString, UriKind uriKind, bool windowsOnly = false)
        {
            if (windowsOnly) Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
            var model = CreateModel();

            var obj = new TypeWithUri { Value = new Uri(uriString, uriKind) };
            TypeWithUri clone = (TypeWithUri) model.DeepClone(obj);
            Assert.Equal(obj.Value, clone.Value);
        }

        [SkippableTheory]
        [InlineData("http://example.com", UriKind.Absolute)]
        [InlineData("http://example.com/path/to/resource", UriKind.Absolute)]
        [InlineData("http://example.com/path/to/resource with spaces/", UriKind.Absolute)]
        [InlineData("http://example.com/path/to/resource%20with%20spaces%20encoded", UriKind.Absolute)]
        [InlineData("http://example.com/withquerystring?param1=1&param2=second", UriKind.Absolute)]
        [InlineData("http://example.com/withfragment?param=test#anchorname", UriKind.Absolute)]
        [InlineData("/relative/path/to/file.txt", UriKind.Relative, true)]
        [InlineData("/relative/path/to/file with spaces.txt", UriKind.Relative, true)]
        [InlineData("/relative/path/to/file%20with%20spaces%20encoded.txt", UriKind.Relative, true)]
        public void TestUriInPlace(string uriString, UriKind uriKind, bool windowsOnly = false)
        {
            if (windowsOnly) Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
            var model = CreateModel();
            model.CompileInPlace();
            var obj = new TypeWithUri { Value = new Uri(uriString, uriKind) };

            TypeWithUri clone = (TypeWithUri)model.DeepClone(obj);
            
            Assert.Equal(obj.Value, clone.Value);
        }

        [Fact]
        public void TestUriCanCompileFully()
        {
            _ = CreateModel().Compile("TestUriCanCompileFully", "TestUriCanCompileFully.dll");
            PEVerify.Verify("TestUriCanCompileFully.dll");
        }

        [SkippableTheory]
        [InlineData("http://example.com", UriKind.Absolute)]
        [InlineData("http://example.com/path/to/resource", UriKind.Absolute)]
        [InlineData("http://example.com/path/to/resource with spaces/", UriKind.Absolute)]
        [InlineData("http://example.com/path/to/resource%20with%20spaces%20encoded", UriKind.Absolute)]
        [InlineData("http://example.com/withquerystring?param1=1&param2=second", UriKind.Absolute)]
        [InlineData("http://example.com/withfragment?param=test#anchorname", UriKind.Absolute)]
        [InlineData("/relative/path/to/file.txt", UriKind.Relative, true)]
        [InlineData("/relative/path/to/file with spaces.txt", UriKind.Relative, true)]
        [InlineData("/relative/path/to/file%20with%20spaces%20encoded.txt", UriKind.Relative, true)]
        public void TestUriCompiled(string uriString, UriKind uriKind, bool windowsOnly = false)
        {
            if (windowsOnly) Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
            var model = CreateModel().Compile();

            var obj = new TypeWithUri { Value = new Uri(uriString, uriKind) };

            TypeWithUri clone = (TypeWithUri)model.DeepClone(obj);            
            Assert.Equal(obj.Value, clone.Value);
        }

        [Theory]
        [InlineData("http://example.com", UriKind.Absolute)]
        [InlineData("http://example.com/path/to/resource", UriKind.Absolute)]
        [InlineData("http://example.com/path/to/resource with spaces/", UriKind.Absolute)]
        [InlineData("http://example.com/path/to/resource%20with%20spaces%20encoded", UriKind.Absolute)]
        [InlineData("http://example.com/withquerystring?param1=1&param2=second", UriKind.Absolute)]
        [InlineData("http://example.com/withfragment?param=test#anchorname", UriKind.Absolute)]
        [InlineData("/relative/path/to/file.txt", UriKind.Relative)]
        [InlineData("/relative/path/to/file with spaces.txt", UriKind.Relative)]
        [InlineData("/relative/path/to/file%20with%20spaces%20encoded.txt", UriKind.Relative)]
        public void TestUriDirect(string uriString, UriKind uriKind)
        {
            var model = RuntimeTypeModel.Create();

            var obj = new Uri(uriString, uriKind);
            Uri clone = (Uri)model.DeepClone(obj);
            Assert.Equal(obj, clone);
        }

        static RuntimeTypeModel CreateModel()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(TypeWithUri), false)
                .Add(1, "Value");
            return model;
        }
    }
}