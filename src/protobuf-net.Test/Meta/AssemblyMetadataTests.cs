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

            var defaultValue = " ";
            var expectedVersion        = string.IsNullOrEmpty(version)        ? defaultValue : version;
            var expectedProductVersion = string.IsNullOrEmpty(productVersion) ? defaultValue : productVersion;
            var expectedCompanyName    = string.IsNullOrEmpty(companyName)    ? " "          : companyName;
            var expectedCopyright      = string.IsNullOrEmpty(copyright)      ? defaultValue : copyright;
            var expectedTrademark      = string.IsNullOrEmpty(trademark)      ? defaultValue : trademark;
            var expectedTitle          = string.IsNullOrEmpty(title)          ? defaultValue : title;
            var expectedProductName    = string.IsNullOrEmpty(productName)    ? defaultValue : productName;
            var expectedDescription    = string.IsNullOrEmpty(description)    ? defaultValue : description;

            Assert.Equal(expectedVersion,        fileInfo.FileVersion);
            Assert.Equal(expectedCompanyName,    fileInfo.CompanyName);
            Assert.Equal(expectedCopyright,      fileInfo.LegalCopyright);
            Assert.Equal(expectedTrademark,      fileInfo.LegalTrademarks);
            Assert.Equal(expectedTitle,          fileInfo.FileDescription);
            Assert.Equal(expectedProductName,    fileInfo.ProductName);
            Assert.Equal(expectedDescription,    fileInfo.Comments);
            Assert.Equal(expectedProductVersion, fileInfo.ProductVersion);
        }
    }
}
