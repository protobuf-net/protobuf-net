using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Locator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Build
{
    internal class MSBuildFixture : IDisposable
    {
        public MSBuildFixture()
        {
            MSBuildLocator.RegisterDefaults();
        }

        void IDisposable.Dispose()
        {
        }
    }

    public class BuildTests : IClassFixture<MSBuildFixture>
    {
        private readonly ILogger logger;
        private readonly ITestOutputHelper o;

        private static readonly Dictionary<string, string> gp =
            new Dictionary<string, string>
            {
                ["Configuration"] = "Debug",
                ["Platform"] = "AnyCPU",
            };

        public BuildTests(ITestOutputHelper o)
        {
            this.o = o;
            this.logger = new XUnitTestLogger(o);
        }

        //private string GetOutput(string exePath, string args)
        //{
        //    var psi = new ProcessStartInfo(exePath, args)
        //    {
        //        UseShellExecute = false,
        //        RedirectStandardOutput = true,
        //        CreateNoWindow = true,
        //    };
        //    var proc = Process.Start(psi);
        //    var text = proc.StandardOutput.ReadToEnd();
        //    proc.WaitForExit();
        //    return text;
        //}

        private void LogProps(Project proj)
        {
            foreach (var kvp in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>().OrderBy(e => e.Key))
            {
                o.WriteLine(kvp.Key + ": " + kvp.Value);
            }

            foreach (var prop in proj.AllEvaluatedProperties.OrderBy(p => p.Name))
            {
                o.WriteLine(prop.Name + ": " + prop.EvaluatedValue + " (" + prop.UnevaluatedValue + ")");
            }
        }

        private string BuildProject(string projFile)
        {
            var pc = new ProjectCollection(gp);
            var proj = pc.LoadProject(projFile);
            var restored = proj.Build("Restore", new[] { logger });
            if (!restored)
            {
                LogProps(proj);
            }
            Assert.True(restored, "Failed to restore packages");
            var result = proj.Build(logger);
            var outputPath = proj.GetPropertyValue("TargetPath");
            Assert.True(result, "Build failed");
            return outputPath;
        }

        [Fact]
        public void CSharpCodeGenTest()
        {
            var exepath = BuildProject("Data/Proj1/Proj.csproj");
        }

        [Fact]
        public void VBCodeGenTest()
        {
            var exepath = BuildProject("Data/Proj2/Proj.vbproj");
        }

        [Fact]
        public void CodeGenErrors()
        {
            var testLogger = new TestLogger();
            const string projFile = "Data/Proj3/Proj.csproj";

            var pc = new ProjectCollection(gp);
            var proj = pc.LoadProject(projFile);
            var restored = proj.Build("Restore", new[] { logger });
            if (!restored) LogProps(proj);
            Assert.True(restored, "Failed to restore packages");
            var result = proj.Build(new ILogger[] { logger, testLogger });
            Assert.False(result);
            Assert.Single(testLogger.Errors);
            Assert.Single(testLogger.Warnings);
        }
    }
}
