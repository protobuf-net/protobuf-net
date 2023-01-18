using Google.Protobuf.Reflection;
using ProtoBuf;
using ProtoBuf.Reflection;
using ProtoBuf.Reflection.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace protogen
{
    internal static class Program
    {
        private enum ExecMode
        {
            None,
            Error,
            Help,
            Version,
            Generate,
            Decode,
            Grpc,
        }
        private static async Task<int> Main(string[] args)
        {
            var mode = ExecMode.None;
            void SetMode(ExecMode value)
            {
                // always allow to switch into error/help; otherwise: set once only
                if (mode == ExecMode.None || (value is ExecMode.Error or ExecMode.Help))
                {
                    mode = value;
                }
                else
                {
                    Console.Error.WriteLine($"Cannot perform {value}; already attempting {mode}");
                    mode = ExecMode.Error;
                }
            }
            try
            {
                string outPath = null; // -o{FILE}, --descriptor_set_out={FILE}
                var importPaths = new List<string>(); // -I{PATH}, --proto_path={PATH}
                var inputFiles = new List<string>(); // {PROTO_FILES} (everything not `-`)
                string package = null; // --package=foo
                string grpcMode = null, grpcUrl = null, grpcService = null, decodeRootType = null;
                CodeGenerator codegen = null;

                Dictionary<string, string> options = null;
                for (int i = 0; i < args.Length; i++)
                {
                    var arg = args[i];
                    string lhs = arg, rhs = "";
                    int index = arg.IndexOf('=');
                    if (index > 0)
                    {
                        lhs = arg.Substring(0, index);
                        rhs = arg.Substring(index + 1);
                    }
                    else if (arg.StartsWith("-o"))
                    {
                        lhs = "--descriptor_set_out";
                        rhs = arg.Substring(2);
                    }
                    else if (arg.StartsWith("-I"))
                    {
                        lhs = "--proto_path";
                        rhs = arg.Substring(2);
                    }

                    if (lhs.StartsWith("+"))
                    {
                        if (options == null) options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        options[lhs.Substring(1)] = rhs;
                        continue;
                    }

                    switch (lhs)
                    {
                        case "":
                            break;
                        case "--version":
                            SetMode(ExecMode.Version);
                            break;
                        case "--package":
                            package = rhs;
                            break;
                        case "-h":
                        case "--help":
                            SetMode(ExecMode.Help);
                            break;
                        case "--csharp_out":
                            outPath = rhs;
                            codegen = CSharpCodeGenerator.Default;
                            SetMode(ExecMode.Generate);
                            break;
                        case "--vb_out":
                            outPath = rhs;
                            codegen = VBCodeGenerator.Default;
                            SetMode(ExecMode.Generate);
                            break;
                        case "--descriptor_set_out":
                            outPath = rhs;
                            codegen = null;
                            SetMode(ExecMode.Generate);
                            break;
                        case "--proto_path":
                            importPaths.Add(rhs);
                            break;
                        case "--pwd":
                            Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
                            Console.WriteLine($"Program: {typeof(Program).Assembly.Location}");
                            Console.WriteLine($"CodeGenerator: {typeof(CodeGenerator).Assembly.Location}");
                            break;
                        case "--grpc":
                            if (++i < args.Length)
                            {
                                SetMode(ExecMode.Grpc);
                                grpcMode = args[i];
                                if (++i < args.Length)
                                {
                                    grpcUrl = args[i];

                                    if (string.Equals(grpcMode, "get", StringComparison.OrdinalIgnoreCase) && ++i < args.Length)
                                    {
                                        grpcService = args[i];
                                    }
                                }
                            }
                            break;
                        case "--decode":
                            if (string.IsNullOrWhiteSpace(decodeRootType = rhs) && ++i < args.Length)
                            {
                                decodeRootType = args[i];
                            }
                            if (string.IsNullOrWhiteSpace(decodeRootType))
                            {
                                Console.Error.WriteLine("No root type specified for decode");
                                SetMode(ExecMode.Error);
                            }
                            else
                            {
                                SetMode(ExecMode.Decode);
                            }
                            break;
                        default:
                            if (lhs.StartsWith("-") || !string.IsNullOrWhiteSpace(rhs))
                            {
                                SetMode(ExecMode.Error);
                                break;
                            }
                            else
                            { // protogen --decode MyRootType my.proto < some.bin
                                inputFiles.Add(lhs);
                            }
                            break;
                    }
                }

                switch (mode)
                {
                    case ExecMode.Help:
                    case ExecMode.Error:
                        ShowHelp();
                        break;
                    case ExecMode.Version:
                        var ver = GetVersion(typeof(Program));
                        Console.WriteLine($"protogen {ver}");
                        var tmp = GetVersion<ProtoReader>();
                        if (tmp != ver) Console.WriteLine($"protobuf-net {tmp}");
                        tmp = GetVersion<FileDescriptorSet>();
                        if (tmp != ver) Console.WriteLine($"protobuf-net.Reflection {tmp}");
                        break;
                    case ExecMode.Grpc:
#if GRPC_TOOLS
                        return await GrpcTools.ExecuteAsync(grpcMode, grpcUrl, grpcService, codegen, outPath, options);
#else
                        Console.Error.Write("gRPC tools are not available on this platform");
                        await Task.Yield(); // this is just to make the compiler happy, and doesn't really matter
                        return 1;
#endif
                    case ExecMode.Generate:
                    case ExecMode.Decode: // share the parse step between generate/decode
                        if (inputFiles.Count == 0)
                        {
                            Console.Error.WriteLine("Missing input file.");
                            return -1;
                        }
                        int exitCode = 0;
                        var set = new FileDescriptorSet
                        {
                            DefaultPackage = package
                        };
                        if (importPaths.Count == 0)
                        {
                            set.AddImportPath(Directory.GetCurrentDirectory());
                        }
                        else
                        {
                            foreach (var dir in importPaths)
                            {
                                if (Directory.Exists(dir))
                                {
                                    set.AddImportPath(dir);
                                }
                                else
                                {
                                    Console.Error.WriteLine($"Directory not found: {dir}");
                                    exitCode = 1;
                                }
                            }
                        }

                        // add the library area for auto-imports (library inbuilts)
                        set.AddImportPath(Path.GetDirectoryName(typeof(Program).Assembly.Location));

                        if (inputFiles.Count == 1 && importPaths.Count == 1)
                        {
                            SearchOption? searchOption = null;
                            if (inputFiles[0] == "**/*.proto"
                                || inputFiles[0] == "**\\*.proto")
                            {
                                searchOption = SearchOption.AllDirectories;
                                set.AllowNameOnlyImport = true;
                            }
                            else if (inputFiles[0] == "*.proto")
                            {
                                searchOption = SearchOption.TopDirectoryOnly;
                            }

                            if (searchOption != null)
                            {
                                inputFiles.Clear();
                                var searchRoot = importPaths[0];
                                foreach (var path in Directory.EnumerateFiles(importPaths[0], "*.proto", searchOption.Value))
                                {
                                    inputFiles.Add(MakeRelativePath(searchRoot, path));
                                }
                            }
                        }

                        foreach (var input in inputFiles)
                        {
                            if (!set.Add(input, true))
                            {
                                Console.Error.WriteLine($"File not found: {input}");
                                exitCode = 1;
                            }
                        }

                        if (exitCode != 0) return exitCode;
                        set.Process();
                        var errors = set.GetErrors();
                        foreach (var err in errors)
                        {
                            if (err.IsError) exitCode++;
                            Console.Error.WriteLine(err.ToString());
                        }
                        if (exitCode != 0) return exitCode;

                        if (mode == ExecMode.Decode)
                        {
                            using var visitor = new TextDecodeVisitor(Console.Out);
                            visitor.Visit(Console.OpenStandardInput(), set, decodeRootType);
                        }
                        else // othewise: code-gen
                        {
                            if (codegen == null)
                            {
                                using (var fds = File.Create(outPath))
                                {
                                    Serializer.Serialize(fds, set);
                                }

                                break;
                            }

                            var files = codegen.Generate(set, options: options);
                            WriteFiles(files, outPath);
                        }
                        break;
                    case ExecMode.None:
                        Console.Error.WriteLine("Missing output directives.");
                        return -1;
                    default:
                        Console.Error.WriteLine($"Unhandled mode: {mode}");
                        return -1;
                }
                return mode == ExecMode.Error ? -1 : 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
                return -1;
            }
        }

        internal static void WriteFiles(IEnumerable<CodeFile> files, string outPath)
        {
            foreach (var file in files)
            {
                var path = Path.Combine(outPath, file.Name);

                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                {
                    Console.Error.WriteLine($"Output directory does not exist, creating... {dir}");
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(path, file.Text);
                Console.WriteLine($"generated: {path}");
            }
        }

        // with thanks to "Dave": https://stackoverflow.com/a/340454/23354
        public static string MakeRelativePath(string fromPath, string toPath)
        {
#if !NETFRAMEWORK
            return Path.GetRelativePath(fromPath, toPath);
#else
            if (String.IsNullOrEmpty(fromPath)) throw new ArgumentNullException(nameof(fromPath));
            if (String.IsNullOrEmpty(toPath)) throw new ArgumentNullException(nameof(toPath));
            // make sure there is a trailing '/', else Uri.MakeRelativeUri won't work as expected
            char lastChar = fromPath[fromPath.Length - 1];
            if (lastChar != Path.DirectorySeparatorChar && lastChar != Path.AltDirectorySeparatorChar)
                fromPath += Path.DirectorySeparatorChar;

            Uri fromUri = new Uri(fromPath, UriKind.RelativeOrAbsolute);
            if (!fromUri.IsAbsoluteUri)
            {
                fromUri = new Uri(Path.Combine(Directory.GetCurrentDirectory(), fromPath));
            }
            Uri toUri = new Uri(toPath, UriKind.RelativeOrAbsolute);
            if (!toUri.IsAbsoluteUri)
            {
                toUri = new Uri(Path.Combine(Directory.GetCurrentDirectory(), toPath));
            }

            if (fromUri.Scheme != toUri.Scheme) { return toPath; } // path can't be made relative.

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            String relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (toUri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
#endif
        }

        private static string GetVersion<T>() => GetVersion(typeof(T));
        private static string GetVersion(Type type)
        {
            var attribs = type.Assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
            var attrib = attribs.Length == 0 ? null : attribs[0] as AssemblyInformationalVersionAttribute;

            return attrib?.InformationalVersion ?? "(unknown)";
        }

        private static void ShowHelp()
        { // deliberately mimicking "protoc"'s calling syntax
            Console.WriteLine(@"Usage: protogen [OPTION] PROTO_FILES
Parse PROTO_FILES and generate output based on the options given:
  -IPATH, --proto_path=PATH   Specify the directory in which to search for
                              imports.  May be specified multiple times;
                              directories will be searched in order.  If not
                              given, the current working directory is used.
  --version                   Show version info and exit.
  -h, --help                  Show this text and exit.
  --decode=MESSAGE_TYPE       Read a binary message of the given type from
                              standard input and write it in text format
                              to standard output.  The message type must
                              be defined in PROTO_FILES or their imports.
  -oFILE,                     Writes a FileDescriptorSet (a protocol buffer,
    --descriptor_set_out=FILE defined in descriptor.proto) containing all of
                              the input files to FILE.
  --csharp_out=OUT_DIR        Generate C# source file(s).
  --vb_out=OUT_DIR            Generate VB source file(s).
  +langver=VERSION            Request a specific language version from the
                              selected code generator.
  +names={auto|original|noplural}
                              Specify naming convention rules.
  +oneof={default|enum}       Specify whether 'oneof' should generate enums.
  +listset={yes|no}           Specify whether lists should emit setters
  +services={grpc;wcf}        Semi-colon list of service metadata to support.
  +OPTION=VALUE               Specify a custom OPTION/VALUE pair for the
                              selected code generator.
  --package=PACKAGE           Add a default package (when no package is
                              specified); can use #FILE# and #DIR# tokens." +
#if GRPC_TOOLS
@"
  --grpc list URL             List all gRPC service available from URL
  --grpc get URL SERVICE      Generate code for the given gRPC service" +
#endif

@"
Note that PROTO_FILES can be *.proto or **/*.proto (recursive) when a single
import location is used, to process all schema files found. In recursive mode,
imports from the current directory can also be specified by name-only.");
        }
    }
}
