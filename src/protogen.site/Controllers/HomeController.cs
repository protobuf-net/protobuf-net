using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Google.Protobuf.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Reflection;

namespace protogen.site.Controllers {
#if RELEASE
    [RequireHttps]
#endif

    public class HomeController : Controller {
        private readonly IWebHostEnvironment _host;
        public HomeController (IWebHostEnvironment host) {
            _host = host;
        }

        public class IndexModel {
            public string ProtocVersion { get; set; }
            public bool CanUseProtoc { get; set; }
            public bool OneOfEnum { get; set; }
            public string LangVer { get; set; }
            public string Names { get; set; }

            public string LibVersion => _libVersion;

            private static readonly string _libVersion;

            static IndexModel () {
                var tmVer = GetVersion (typeof (TypeModel));
                var cgVer = GetVersion (typeof (CodeGenerator));
                _libVersion = tmVer == cgVer ? tmVer : (tmVer + "/" + cgVer);
            }
            private static string GetVersion (Type type) {
                var assembly = type.GetTypeInfo ().Assembly;
                return assembly.GetCustomAttribute<AssemblyFileVersionAttribute> ()?.Version ??
                    assembly.GetName ().Version.ToString ();
            }
        }
        public const int MaxFileLength = 1024 * 1024;

        public ActionResult Decode (string hex = null, string base64 = null, IFormFile file = null, bool deep = true) {
            byte[] data = null;
            try {
                if (hex != null) hex = hex.Trim ();
                if (base64 != null) base64 = base64.Trim ();

                if (file != null && file.Length <= MaxFileLength) {
                    using (var stream = file.OpenReadStream ())
                    using (var ms = new MemoryStream ((int) file.Length)) {
                        stream.CopyTo (ms);
                        data = ms.ToArray ();
                    }
                } else if (!string.IsNullOrWhiteSpace (hex)) {
                    hex = hex.Replace (" ", "").Replace ("-", "");
                    int len = hex.Length / 2;
                    var tmp = new byte[len];
                    for (int i = 0; i < len; i++) {
                        tmp[i] = Convert.ToByte (hex.Substring (i * 2, 2), 16);
                    }
                    data = tmp;
                } else if (!string.IsNullOrWhiteSpace (base64)) {
                    data = Convert.FromBase64String (base64);
                }
            } catch { }
            return View (new DecodeModel (data, deep));
        }

        public class DecodeModel {
            private ArraySegment<byte> data;
            public bool Deep { get; }

            public int SkipField { get; }

            private DecodeModel (byte[] data, bool deep, int offset, int count, int skipField = 0) {
                this.data = data == null ?
                    default :
                    new ArraySegment<byte> (data, offset, count);
                Deep = deep;
                SkipField = skipField;
            }
            public DecodeModel (byte[] data, bool deep) : this (data, deep, 0, data?.Length ?? 0) { }

            public string AsHex () => ContainsValue ? BitConverter.ToString (data.Array, data.Offset, data.Count) : null;

            public string AsHex (int offset, int count) => ContainsValue ? BitConverter.ToString (data.Array, data.Offset + offset, count) : null;
            public string AsBase64 () => ContainsValue ? Convert.ToBase64String (data.Array, data.Offset, data.Count) : null;
            public string AsString () {
                try {
                    return Encoding.UTF8.GetString (data.Array, data.Offset, data.Count);
                } catch { return null; }
            }
            public int Count => data.Count;
            public ProtoReader GetReader (out ProtoReader.State state) {
                var ms = new MemoryStream (data.Array, data.Offset, data.Count, false);
                return ProtoReader.Create (out state, ms, null, null);
            }
            public bool ContainsValue => data.Array != null;
            public bool CouldBeProto () {
                if (!ContainsValue) return false;
                try {
                    using (var reader = GetReader (out var state)) {
                        int field;
                        while ((field = reader.ReadFieldHeader (ref state)) > 0) {
                            reader.SkipField (ref state);
                        }
                        return reader.GetPosition (ref state) == Count; // MemoryStream will let you seek out of bounds!
                    }
                } catch {
                    return false;
                }
            }
            public DecodeModel Slice (int offset, int count, int skipField = 0) => new DecodeModel (data.Array, Deep, data.Offset + offset, count, skipField);
        }

        [Route ("/generate")]
        [HttpPost]
        public IActionResult Generate ([Required] string schema, [Required] string tooling) {
            using (var reader = new StringReader (schema)) {
                var set = new FileDescriptorSet {
                ImportValidator = path => ValidateImport (path),
                };
                set.AddImportPath (Path.Combine (_host.WebRootPath, "protoc"));
                set.Add ("my.proto", true, reader);

                set.Process ();
                var errors = set.GetErrors ();

                // we're going to offer protoc! hold me...
                if (errors.Length != 0 && schema.Contains ("import")) {
                    // code output disabled because of import
                    return BadRequest ();
                } else {
                    var files = RunProtoc (_host, schema, tooling, out var stdout, out var stderr, out var exitCode);
                    if (exitCode != 0) {
                        return base.StatusCode (500, new { stderr, stdout, exitCode });
                    }
                    return Ok (files);
                }
            }
        }

        private Dictionary<string, string> legalImports = null;
        private readonly static char[] DirSeparators = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        private bool ValidateImport (string path) => ResolveImport (path) != null;
        private string ResolveImport (string path) {
            // only allow the things that we actively find under "protoc" on the web root,
            // remembering to normalize our slashes; this means that c:\... or ../../ etc will
            // all fail, as they are not in "legalImports"
            if (legalImports == null) {
                var tmp = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
                var root = Path.Combine (_host.WebRootPath, "protoc");
                foreach (var found in Directory.EnumerateFiles (root, "*.proto", SearchOption.AllDirectories)) {
                    if (found.StartsWith (root)) {
                        tmp.Add (found.Substring (root.Length).TrimStart (DirSeparators)
                            .Replace (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), found);
                    }
                }
                legalImports = tmp;
            }
            return legalImports.TryGetValue (path.Replace (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                out string actual) ? actual : null;
        }

        private static string protocVersion = null;
        private static bool protocUsable;
        public string GetProtocVersion (IWebHostEnvironment host, out bool canUse) {
            if (protocVersion == null) {
                try {
                    RunProtoc (host, "--version", Path.GetTempPath (), out var stdout, out var stderr, out var exitCode);
                    if (exitCode == 0 && string.IsNullOrWhiteSpace (stderr) && !string.IsNullOrWhiteSpace (stdout)) {
                        protocVersion = stdout.Trim ();
                        protocUsable = true;
                    } else {
                        protocVersion = "protoc error";
                    }
                } catch (Exception ex) {
                    protocVersion = "exception: " + ex.Message;
                }
            }
            canUse = protocUsable;
            return protocVersion;
        }
        public class ProtocTooling {
            public string Caption { get; }
            public string Tooling { get; }
            public ProtocTooling (string tooling, string caption) {
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
            }.AsReadOnly ();
            public static bool IsDefined (string tooling) => Options.Any (x => x.Tooling == tooling);
        }
        private CodeFile[] RunProtoc (IWebHostEnvironment host, string schema, string tooling, out string stdout, out string stderr, out int exitCode) {
            var tmp = Path.GetTempPath ();
            var session = Path.Combine (tmp, Guid.NewGuid ().ToString ());
            Directory.CreateDirectory (session);
            try {
                const string file = "my.proto";
                var fullPath = Path.Combine (session, file);
                System.IO.File.WriteAllText (fullPath, schema);

                var includeRoot = Path.Combine (host.WebRootPath, "protoc");
                var args = $"--{tooling}_out=\"{session}\" --proto_path=\"{session}\" --proto_path=\"{includeRoot}\" \"{fullPath}\"";
                var exePath = Path.Combine (host.WebRootPath, "protoc\\protoc.exe");
                if (!System.IO.File.Exists (exePath)) {
                    throw new FileNotFoundException ("protoc not found");
                }
                using (var proc = new Process ()) {
                    var psi = proc.StartInfo;
                    psi.FileName = exePath;
                    psi.Arguments = args;
                    if (!string.IsNullOrEmpty (session)) psi.WorkingDirectory = session;
                    psi.CreateNoWindow = true;
                    psi.RedirectStandardError = psi.RedirectStandardOutput = true;
                    psi.UseShellExecute = false;
                    proc.Start ();
                    var stdoutTask = proc.StandardOutput.ReadToEndAsync ();
                    var stderrTask = proc.StandardError.ReadToEndAsync ();
                    if (!proc.WaitForExit (5000)) {
                        try { proc.Kill (); } catch { }
                    }
                    exitCode = proc.ExitCode;
                    stderr = stdout = "";
                    if (stdoutTask.Wait (1000)) stdout = stdoutTask.Result;
                    if (stderrTask.Wait (1000)) stderr = stderrTask.Result;

                }
                List<CodeFile> files = new List<CodeFile> ();
                if (exitCode == 0) {
                    foreach (var generated in Directory.EnumerateFiles (session, "*.*", SearchOption.AllDirectories)) {
                        var name = Path.GetFileName (generated);
                        if (name == file) continue; // that's our input!

                        files.Add (new CodeFile (name, System.IO.File.ReadAllText (generated)));
                    }
                    return files.ToArray ();
                } else {
                    return null;
                }
            } finally {
                Directory.Delete (session, true);
            }
        }
    }
}