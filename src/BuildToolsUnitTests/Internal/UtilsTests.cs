using BuildToolsUnitTests.Abstractions;
using ProtoBuf.Internal.Roslyn.Extensions;
using Xunit;

namespace BuildToolsUnitTests.Internal
{
    public class UtilsTests : RoslynAnalysisTests
    {
        [Fact]
        public void AddUsingsIfNotExist_ProperlyModifiesSourceText()
        {
            var testCode = @"
using System;

public class Foo {{
    public string Bar {{ get; set; }}
}}";

            var compilationUnitSyntax = BuildCompilationUnitSyntax(testCode);
            var newCompilationUnitSyntax = compilationUnitSyntax.AddUsingsIfNotExist("System.ComponentModel");
            var resultCode = newCompilationUnitSyntax.ToFullString();

            Assert.NotEmpty(resultCode);
            Assert.Contains("using System.ComponentModel;", resultCode);
        }
    }
}