using Google.Protobuf.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ProtoBuf.Models;
using ProtoBuf.Reflection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace protogen.site.Controllers
{
#if RELEASE
    [RequireHttps]
#endif

    public class HomeController : Controller
    {
        private readonly IWebHostEnvironment _host;
        public HomeController(IWebHostEnvironment host)
        {
            _host = host;
        }

        [Route("/generate")]
        [HttpPost]
        public IActionResult Generate([FromBody] GeneratorViewModel generatorViewModel)
        {
            using var reader = new StringReader(generatorViewModel.ProtoContent);
            var set = new FileDescriptorSet
            {
                ImportValidator = path => ValidateImport(path),
            };
            set.AddImportPath(Path.Combine(_host.WebRootPath, "protoc"));
            set.Add("my.proto", true, reader);

            set.Process();
            var errors = set.GetErrors();
            if (errors.Length != 0)
            {
                //code parsing is supposed to happening client side, so we don't send error here
                return BadRequest();
            }
            if (generatorViewModel.IsProtogen())
            {
                return Ok( 
                    generatorViewModel
                        .GetCodeGenerator()
                        .Generate(set, generatorViewModel.GetNameNormalizerForConvention(), 
                            generatorViewModel.GetOptions())
                        .ToList());
                }

            // if we got this far, it means that we resolved all the imports, so
            // we don't need to worry about protoc going out-of-bounds with external files
            // (since we constrain with ValidateImport), so: off to 'protoc' we go!
            var files = RunProtoc(_host,
                generatorViewModel.ProtoContent,
                generatorViewModel.GetProtocTooling(),
                out var stdout,
                out var stderr,
                out var exitCode);
            if (exitCode != 0)
            {
                return base.StatusCode(500, new { stderr, stdout, exitCode });
            }
            return Ok(files);
        }

        private Dictionary<string, string> legalImports = null;
        private readonly static char[] DirSeparators = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        private bool ValidateImport(string path) => ResolveImport(path) != null;
        private string ResolveImport(string path)
        {
            // only allow the things that we actively find under "protoc" on the web root,
            // remembering to normalize our slashes; this means that c:\... or ../../ etc will
            // all fail, as they are not in "legalImports"
            if (legalImports == null)
            {
                var tmp = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var root = Path.Combine(_host.WebRootPath, "protoc");
                foreach (var found in Directory.EnumerateFiles(root, "*.proto", SearchOption.AllDirectories))
                {
                    if (found.StartsWith(root))
                    {
                        tmp.Add(found.Substring(root.Length).TrimStart(DirSeparators)
                            .Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), found);
                    }
                }
                legalImports = tmp;
            }
            return legalImports.TryGetValue(path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                out string actual) ? actual : null;
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
            public static ReadOnlyCollection<ProtocTooling> Options { get; } = new List<ProtocTooling> {
                new ProtocTooling ("cpp", "C++"),
                new ProtocTooling ("csharp", "C#"),
                new ProtocTooling ("java", "Java"),
                new ProtocTooling ("javanano", "Java Nano"),
                new ProtocTooling ("js", "JavaScript"),
                new ProtocTooling ("objc", "Objective-C"),
                new ProtocTooling ("php", "PHP"),
                new ProtocTooling ("python", "Python"),
                new ProtocTooling ("ruby", "Ruby"),
            }.AsReadOnly();
            public static bool IsDefined(string tooling) => Options.Any(x => x.Tooling == tooling);
        }
        private CodeFile[] RunProtoc(IWebHostEnvironment host, string schema, string tooling, out string stdout, out string stderr, out int exitCode)
        {
            var tmp = Path.GetTempPath();
            var session = Path.Combine(tmp, Guid.NewGuid().ToString());
            Directory.CreateDirectory(session);
            try
            {
                const string file = "my.proto";
                var fullPath = Path.Combine(session, file);
                System.IO.File.WriteAllText(fullPath, schema);

                var includeRoot = Path.Combine(host.WebRootPath, "protoc");
                var args = $"--experimental_allow_proto3_optional --{tooling}_out=\"{session}\" --proto_path=\"{session}\" --proto_path=\"{includeRoot}\" \"{fullPath}\"";
                var exePath = Path.Combine(host.WebRootPath, "protoc\\protoc.exe");
                if (!System.IO.File.Exists(exePath))
                {
                    throw new FileNotFoundException("protoc not found");
                }
                using (var proc = new Process())
                {
                    var psi = proc.StartInfo;
                    psi.FileName = exePath;
                    psi.Arguments = args;
                    if (!string.IsNullOrEmpty(session)) psi.WorkingDirectory = session;
                    psi.CreateNoWindow = true;
                    psi.RedirectStandardError = psi.RedirectStandardOutput = true;
                    psi.UseShellExecute = false;
                    proc.Start();
                    var stdoutTask = proc.StandardOutput.ReadToEndAsync();
                    var stderrTask = proc.StandardError.ReadToEndAsync();
                    if (!proc.WaitForExit(5000))
                    {
                        try { proc.Kill(); } catch { }
                    }
                    exitCode = proc.ExitCode;
                    stderr = stdout = "";
                    if (stdoutTask.Wait(1000)) stdout = stdoutTask.Result;
                    if (stderrTask.Wait(1000)) stderr = stderrTask.Result;

                }
                List<CodeFile> files = new List<CodeFile>();
                if (exitCode == 0)
                {
                    foreach (var generated in Directory.EnumerateFiles(session, "*.*", SearchOption.AllDirectories))
                    {
                        var name = Path.GetFileName(generated);
                        if (name == file) continue; // that's our input!

                        files.Add(new CodeFile(name, System.IO.File.ReadAllText(generated)));
                    }
                    return files.ToArray();
                }
                else
                {
                    return null;
                }
            }
            finally
            {
                Directory.Delete(session, true);
            }
        }
    }
}