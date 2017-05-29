using System;
using System.IO;
using System.Linq;
using Google.Protobuf.Reflection;
using Microsoft.AspNetCore.Mvc;
using ProtoBuf;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace protogen.site.Controllers
{

#if RELEASE
    [RequireHttps]
#endif
    public class HomeController : Controller
    {
        readonly IHostingEnvironment _host;
        public HomeController(IHostingEnvironment host)
        {
            _host = host;
        }
        public IActionResult Index() => Index(false);

        [Route("/jsil")]
        public IActionResult ClientSide() => Index(true);

        private IActionResult Index(bool jsil)
        {
            var model = new IndexModel { UseJSIL = jsil };
            model.ProtocVersion = GetProtocVersion(_host, out var canUse);
            model.CanUseProtoc = canUse;
            return View("Index", model);
        }

        [Route("/about")]
        public IActionResult About() => View();

        public IActionResult Error() => View();

        public class GenerateResult
        {
            public string Code
            {
                get;
                set;
            }

            public Error[] ParserExceptions
            {
                get;
                set;
            }

            public Exception Exception
            {
                get;
                set;
            }
        }


        public class IndexModel
        {
            public bool UseJSIL { get; set; }
            public string ProtocVersion { get; set; }
            public bool CanUseProtoc { get; set; }
        }
        [Route("/generate")]
        [HttpPost]
        public GenerateResult Generate(string schema = null, string tooling = null)
        {
            if (string.IsNullOrWhiteSpace(schema))
            {
                return null;
            }
            var result = new GenerateResult();
            try
            {
                using (var reader = new StringReader(schema))
                {
                    var set = new FileDescriptorSet { AllowImports = false };
                    set.Add("my.proto", reader);
                    var parsed = set.Files.Single();


                    set.Process();
                    var errors = set.GetErrors();

                    if (!ProtocTooling.IsDefined(tooling))
                    {
                        if (errors.Length > 0)
                        {
                            result.ParserExceptions = errors;
                        }
                        result.Code = parsed.GenerateCSharp(errors: errors);
                    }
                    else
                    {
                        // we're going to offer protoc! hold me...
                        if (errors.Length != 0 && schema.Contains("import"))
                        {
                            result.Code = "code output disabled because of import";
                        }
                        else
                        {
                            result.Code = RunProtoc(_host, schema, tooling, out errors);
                            if (errors.Length > 0)
                            {
                                result.ParserExceptions = errors;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
            }
            return result;
        }
        static string protocVersion = null;
        static bool protocUsable;
        public static string GetProtocVersion(IHostingEnvironment host, out bool canUse)
        {
            if (protocVersion == null)
            {

                try
                {
                    int code = RunProtoc(host, "--version", Path.GetTempPath(), out var stdout, out var stderr);
                    if (code == 0 && string.IsNullOrWhiteSpace(stderr) && !string.IsNullOrWhiteSpace(stdout))
                    {
                        protocVersion = stdout.Trim();
                        protocUsable = true;
                    }
                    else
                    {
                        protocVersion = "protoc error";
                    }
                }
                catch (Exception ex)
                {
                    protocVersion = "exception: " + ex.Message;
                }
            }
            canUse = protocUsable;
            return protocVersion;
        }
        static int RunProtoc(IHostingEnvironment host, string arguments, string workingDir, out string stdout, out string stderr)
        {
            var exePath = Path.Combine(host.WebRootPath, "protoc.exe");
            if (!System.IO.File.Exists(exePath))
            {
                throw new FileNotFoundException("protoc not found");
            }
            using (var proc = new Process())
            {
                var psi = proc.StartInfo;
                psi.FileName = exePath;
                psi.Arguments = arguments;
                if (!string.IsNullOrEmpty(workingDir)) psi.WorkingDirectory = workingDir;
                psi.RedirectStandardError = psi.RedirectStandardOutput = true;
                psi.UseShellExecute = false;
                proc.Start();
                var stdoutTask = proc.StandardOutput.ReadToEndAsync();
                var stderrTask = proc.StandardError.ReadToEndAsync();
                if (!proc.WaitForExit(5000))
                {
                    try { proc.Kill(); } catch { }
                }
                var exitCode = proc.ExitCode;
                stderr = stdout = "";
                if (stdoutTask.Wait(1000)) stdout = stdoutTask.Result;
                if (stderrTask.Wait(1000)) stderr = stderrTask.Result;

                return exitCode;
            }
        }
        public class ProtocTooling
        {
            public string Caption { get; }
            public string Tooling { get; }
            public ProtocTooling(string tooling, string caption)
            {
                Tooling = tooling;
                Caption = caption;
            }
            public static ReadOnlyCollection<ProtocTooling> Options { get; }
            = new List<ProtocTooling>
            {
                new ProtocTooling("cpp", "C++"),
                new ProtocTooling("csharp", "C#"),
                new ProtocTooling("java", "Java"),
                new ProtocTooling("javanano", "Java Nano"),
                new ProtocTooling("js", "JavaScript"),
                new ProtocTooling("objc", "Objective-C"),
                new ProtocTooling("php", "PHP"),
                new ProtocTooling("python", "Python"),
                new ProtocTooling("ruby", "Ruby"),
            }.AsReadOnly();
            public static bool IsDefined(string tooling) => Options.Any(x => x.Tooling == tooling);
        }
        private string RunProtoc(IHostingEnvironment host, string schema, string tooling, out Error[] errors)
        {
            var tmp = Path.GetTempPath();
            var session = Path.Combine(tmp, Guid.NewGuid().ToString());
            Directory.CreateDirectory(session);
            try
            {
                const string file = "my.proto";
                System.IO.File.WriteAllText(Path.Combine(session, file), schema);
                var args = $"--{tooling}_out=\"{session}\" {file}";
                int exitCode = RunProtoc(host, args, session, out var stdout, out var stderr);
                errors = ProtoBuf.Error.Parse(stdout, stderr);
                if (exitCode == 0)
                {
                    string found = null;
                    foreach (var generated in Directory.EnumerateFiles(session))
                    {
                        if (Path.GetFileName(generated) == file) continue; // that's our input!
                        if (found != null)
                        {
                            return "(multiple files generated)";
                        }
                        found = generated;
                    }
                    if (found == null)
                    {
                        return "(no file generated)";
                    }
                    return System.IO.File.ReadAllText(found);
                }
                else
                {
                    return "(protoc exited with an error)";
                }
            }
            finally
            {
                Directory.Delete(session, true);
            }
        }
    }
}