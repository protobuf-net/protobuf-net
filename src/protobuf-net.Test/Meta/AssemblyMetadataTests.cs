using ProtoBuf.Meta;
using System;
using System.Diagnostics;
using Xunit;

namespace ProtoBuf.unittest.Meta
{
    public class AssemblyMetadataTests
    {
        [Theory()]
        [InlineData(null, null, null, null, null, null, "1.2.3.4", "1.2")]
        [InlineData("", "", "", "", "", "", "1.2.3.4", "1.2")]
        [InlineData("Company Name", "", "", "Product Name", "Title", "", "1.2.3.4", "1.2")]
        [InlineData("Company Name", "Copyright", "Description", "Product Name", "Title", "Legal Trademark", "1.2.3.4", "1.2")]
        public void Test(string companyName, string copyright, string description, string productName, string title,
            string trademark, string version, string productVersion)
        {
            // arrange
            var outputPath = "AssemblyMetadataTestContract.dll";

            var options = new RuntimeTypeModel.CompilerOptions()
            {
                Accessibility = RuntimeTypeModel.Accessibility.Public,
                TypeName = "Model",
                OutputPath = outputPath,
                AssemblyCompanyName = companyName,
                AssemblyCopyright = copyright,
                AssemblyDescription = description,
                AssemblyProductName = productName,
                AssemblyTitle = title,
                AssemblyTrademark = trademark,
                AssemblyVersion = new Version(version),
                AssemblyProductVersion = new Version(productVersion)
            };
            var model = RuntimeTypeModel.Create();

            // act
            model.Compile(options);

            // assert
            var fileInfo = FileVersionInfo.GetVersionInfo(outputPath);

            Assert.Equal(Normalize(version),        fileInfo.FileVersion);
            Assert.Equal(Normalize(companyName),    fileInfo.CompanyName);
            Assert.Equal(Normalize(copyright),      fileInfo.LegalCopyright);
            Assert.Equal(Normalize(trademark),      fileInfo.LegalTrademarks);
            Assert.Equal(Normalize(title),          fileInfo.FileDescription);
            Assert.Equal(Normalize(productName),    fileInfo.ProductName);
            Assert.Equal(Normalize(description),    fileInfo.Comments);
            Assert.Equal(Normalize(productVersion), fileInfo.ProductVersion);
        }

        private string Normalize(string value)
        {
            return string.IsNullOrEmpty(value) ? " " : value;
        }
    }
}
