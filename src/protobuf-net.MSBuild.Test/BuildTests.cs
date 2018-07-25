using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Build
{
    public class BuildTests
    {
        ILogger logger;
        ITestOutputHelper o;

        static Dictionary<string, string> gp =
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

        string GetOutput(string exePath, string args)
        {
            var psi = new ProcessStartInfo(exePath, args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };
            var proc = Process.Start(psi);
            var text = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            return text;
        }

        void LogProps(Project proj)
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

        string BuildProject(string projFile)
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
        public void BuildTest()
        {
            var exepath = BuildProject("Data/Proj1/Proj.csproj");
            Assert.Equal("Hello, World\r\n", GetOutput(exepath, ""));
        }
    }

    class XUnitTestLogger : ILogger
    {
        static bool DumpEnv = false;
        static bool Verbose = true;

        ITestOutputHelper o;

        public XUnitTestLogger(ITestOutputHelper o)
        {
            this.o = o;
        }

        public LoggerVerbosity Verbosity { get; set; }
        public string Parameters { get; set; }

        public void Initialize(IEventSource eventSource)
        {
            eventSource.AnyEventRaised += EventSource_AnyEventRaised;
            eventSource.BuildStarted += EventSource_BuildStarted;
            eventSource.ProjectStarted += EventSource_ProjectStarted;
            eventSource.MessageRaised += EventSource_MessageRaised;
            eventSource.ErrorRaised += EventSource_ErrorRaised;
        }

        private void EventSource_ErrorRaised(object sender, BuildErrorEventArgs e)
        {
            o.WriteLine(e.Message + " " + e.File + "(" + e.LineNumber + "," + e.ColumnNumber + ")");
        }

        private void EventSource_MessageRaised(object sender, BuildMessageEventArgs e)
        {
            if (e.Message.StartsWith("DEBUG:"))
            {
                o.WriteLine(e.Message);
            }
        }

        private void EventSource_ProjectStarted(object sender, ProjectStartedEventArgs e)
        {
            if (DumpEnv)
            {
                o.WriteLine("Initial Properties:");
                foreach (DictionaryEntry kvp in e.Properties)
                {
                    o.WriteLine($"{kvp.Key} {kvp.Value}");
                }
            }
        }

        private void EventSource_BuildStarted(object sender, BuildStartedEventArgs e)
        {
            if (DumpEnv)
            {
                o.WriteLine("Environment:");
                foreach (var kvp in e.BuildEnvironment)
                {
                    o.WriteLine($"{kvp.Key}: {kvp.Value}");
                }
            }
        }

        private void EventSource_AnyEventRaised(object sender, BuildEventArgs e)
        {
            if (Verbose)
            {
                o.WriteLine(e.Message);
            }
        }

        public void Shutdown()
        {
        }
    }
}
