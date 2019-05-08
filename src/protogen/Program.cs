using Google.Protobuf.Reflection;
using ProtoBuf;
using ProtoBuf.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace protogen
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                string outPath = null; // -o{FILE}, --descriptor_set_out={FILE}
                bool version = false; // --version
                bool help = false; // -h, --help
                var importPaths = new List<string>(); // -I{PATH}, --proto_path={PATH}
                var inputFiles = new List<string>(); // {PROTO_FILES} (everything not `-`)
                bool exec = false;
                string package = null; // --package=foo
                CodeGenerator codegen = null;

                Dictionary<string, string> options = null;
                foreach (string arg in args)
                {
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

                    if(lhs.StartsWith("+"))
                    {
                        if(options == null) options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        options[lhs.Substring(1)] = rhs;
                        continue;
                    }

                    switch (lhs)
                    {
                        case "":
                            break;
                        case "--version":
                            version = true;
                            break;
                        case "--package":
                            package = rhs;
                            break;
                        case "-h":
                        case "--help":
                            help = true;
                            break;
                        case "--csharp_out":
                            outPath = rhs;
                            codegen = CSharpCodeGenerator.Default;
                            exec = true;
                            break;
                        case "--vb_out":
                            outPath = rhs;
#pragma warning disable CS0618
                            codegen = VBCodeGenerator.Default;
#pragma warning restore CS0618
                            exec = true;
                            break;
                        case "--descriptor_set_out":
                            outPath = rhs;
                            codegen = null;
                            exec = true;
                            break;
                        case "--proto_path":
                            importPaths.Add(rhs);
                            break;
                        case "--pwd":
                            Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
#if NETCOREAPP2_1 || NETSTANDARD2_0
                            Console.WriteLine($"Program: {typeof(Program).Assembly.Location}");
                            Console.WriteLine($"CodeGenerator: {typeof(CodeGenerator).Assembly.Location}");
#endif
                            break;
                        default:
                            if (lhs.StartsWith("-") || !string.IsNullOrWhiteSpace(rhs))
                            {
                                help = true;
                                break;
                            }
                            else
                            {
                                inputFiles.Add(lhs);
                            }
                            break;
                    }
                }

                if (help)
                {
                    ShowHelp();
                    return 0;
                }
                else if (version)
                {
                    var ver = GetVersion(typeof(Program));
                    Console.WriteLine($"protogen {ver}");
                    var tmp = GetVersion<ProtoReader>();
                    if (tmp != ver) Console.WriteLine($"protobuf-net {tmp}");
                    tmp = GetVersion<FileDescriptorSet>();
                    if (tmp != ver) Console.WriteLine($"protobuf-net.Reflection {tmp}");
                    return 0;
                }
                else if (inputFiles.Count == 0)
                {
                    Console.Error.WriteLine("Missing input file.");
                    return -1;
                }
                else if (!exec)
                {
                    Console.Error.WriteLine("Missing output directives.");
                    return -1;
                }
                else
                {
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

                    if(inputFiles.Count == 1 && importPaths.Count == 1)
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

                        if(searchOption != null)
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

                    if(exitCode != 0) return exitCode;
                    set.Process();
                    var errors = set.GetErrors();
                    foreach(var err in errors)
                    {
                        if (err.IsError) exitCode++;
                        Console.Error.WriteLine(err.ToString());
                    }
                    if (exitCode != 0) return exitCode;

                    if (codegen == null)
                    {
                        using (var fds = File.Create(outPath))
                        {
                            Serializer.Serialize(fds, set);
                        }
                        
                        return 0;
                    }

                    var files = codegen.Generate(set, options: options);
                    foreach (var file in files)
                    {
                        var path = Path.Combine(outPath, file.Name);

                        var dir = Path.GetDirectoryName(path);
                        if(!Directory.Exists(dir))
                        {
                            Console.Error.WriteLine($"Output directory does not exist, creating... {dir}");
                            Directory.CreateDirectory(dir);
                        }

                        File.WriteAllText(path, file.Text);
                        Console.WriteLine($"generated: {path}");
                    }

                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
                return -1;
            }
        }

        // with thanks to "Dave": https://stackoverflow.com/a/340454/23354
        public static string MakeRelativePath(string fromPath, string toPath)
        {
#if NETCOREAPP2_0 || NETCOREAPP2_1
            return Path.GetRelativePath(fromPath, toPath);
#else
            if (String.IsNullOrEmpty(fromPath)) throw new ArgumentNullException(nameof(fromPath));
            if (String.IsNullOrEmpty(toPath)) throw new ArgumentNullException(nameof(toPath));

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
#if NETCOREAPP1_1
            var attrib = type.GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
#else
            var attribs = type.Assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
            var attrib = attribs.Length == 0 ? null : attribs[0] as AssemblyInformationalVersionAttribute;
#endif
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
  +OPTION=VALUE               Specify a custom OPTION/VALUE pair for the
                              selected code generator.
  --package=PACKAGE           Add a default package (when no package is
                              specified); can use #FILE# and #DIR# tokens.

Note that PROTO_FILES can be *.proto or **/*.proto (recursive) when a single
import location is used, to process all schema files found. In recursive mode,
imports from the current directory can also be specified by name-only.");
        }
    }
}
