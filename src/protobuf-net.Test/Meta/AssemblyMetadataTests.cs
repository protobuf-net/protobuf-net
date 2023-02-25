using ProtoBuf.Meta;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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
            var options = new RuntimeTypeModel.CompilerOptions()
            {
                Accessibility = RuntimeTypeModel.Accessibility.Public,
                TypeName = "Model",
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

            // verify in-memory assembly output
            var assembly = model.Compile(options).GetType().Assembly;
            Assert.Equal(Normalize(version), Normalize(assembly.GetName().Version?.ToString()));
            Assert.Equal(Normalize(version), GetMetadata<AssemblyFileVersionAttribute>(a => a.Version));
            Assert.Equal(Normalize(companyName), GetMetadata<AssemblyCompanyAttribute>(a => a.Company));
            Assert.Equal(Normalize(copyright), GetMetadata<AssemblyCopyrightAttribute>(a => a.Copyright));
            Assert.Equal(Normalize(trademark), GetMetadata<AssemblyTrademarkAttribute>(a => a.Trademark));
            Assert.Equal(Normalize(title), GetMetadata<AssemblyTitleAttribute>(a => a.Title));
            Assert.Equal(Normalize(productName), GetMetadata<AssemblyProductAttribute>(a => a.Product));
            Assert.Equal(Normalize(description), GetMetadata<AssemblyDescriptionAttribute>(a => a.Description));
            Assert.Equal(Normalize(productVersion), GetMetadata<AssemblyInformationalVersionAttribute>(a => a.InformationalVersion));

            string GetMetadata<TAttribute>(Func<TAttribute, string> selector)
                where TAttribute : Attribute
            {
                var attrib = assembly.GetCustomAttribute<TAttribute>();
                var s = attrib is null ? null : selector(attrib);
                return Normalize(s);
            }

#if NETFRAMEWORK
            // verify dll output
            options.OutputPath = "AssemblyMetadataTestContract.dll";

            model.Compile(options);
            Assert.True(File.Exists(options.OutputPath), "File not generated");

            // assert
            var fileInfo = FileVersionInfo.GetVersionInfo(options.OutputPath);

            Assert.Equal(Normalize(version),        fileInfo.FileVersion);
            Assert.Equal(Normalize(companyName),    fileInfo.CompanyName);
            Assert.Equal(Normalize(copyright),      fileInfo.LegalCopyright);
            Assert.Equal(Normalize(trademark),      fileInfo.LegalTrademarks);
            Assert.Equal(Normalize(title),          fileInfo.FileDescription);
            Assert.Equal(Normalize(productName),    fileInfo.ProductName);
            Assert.Equal(Normalize(description),    fileInfo.Comments);
            Assert.Equal(Normalize(productVersion), fileInfo.ProductVersion);

            PEVerify.Verify(options.OutputPath); // do this last, since it deletes the file
#endif
        }

        private string Normalize(string value)
        {
            return string.IsNullOrEmpty(value) ? " " : value;
        }
    }
}