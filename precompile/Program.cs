using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ProtoBuf.Meta;

namespace precompile
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                Console.WriteLine("protobuf-net pre-compiler");
                PreCompileContext ctx;
                if (!CommandLineAttribute.TryParse(args, out ctx))
                {
                    return -1;
                }

                if (ctx.Help)
                {
                    Console.WriteLine(@"

Generates a serialization dll that can be used with just the
(platform-specific) protobuf-net core, allowing fast and efficient
serialization even on light frameworks (CF, SL, SP7, Metro, etc).

The input assembly(ies) is(are) anaylsed for types decorated with
[ProtoContract]. All such types are added to the model, as are any
types that they require.

Note: the compiler must be able to resolve a protobuf-net.dll
that is suitable for the target framework; this is done most simply
by ensuring that the appropriate protobuf-net.dll is next to the
input assembly.

Options:

    -f:<framework> - Can be an explicit path, or a path relative to:
                     Reference Assemblies\Microsoft\Framework
    -o:<file>      - Output dll path
    -t:<typename>  - Type name of the serializer to generate
    -p:<path>      - Additional directory to probe for assemblies
    <file>         - Input file to analyse

Example:

    precompile -f:.NETCore\v4.5 MyDtos\My.dll -o:MySerializer.dll
        -t:MySerializer");
                    return -1;
                }
                if (!ctx.SanityCheck()) return -1;

                bool allGood = ctx.Execute();
                return allGood ? 0 : -1;
            }
            catch (Exception ex)
            {
                while (ex != null)
                {
                    Console.Error.WriteLine(ex.Message);
                    Console.Error.WriteLine(ex.StackTrace);
                    Console.Error.WriteLine();
                    ex = ex.InnerException;
                }
                return -1;
            }
        }
    }

    class PreCompileContext
    {
        [CommandLine("f"), CommandLine("framework")]
        public string Framework { get; set; }

        private readonly List<string> probePaths = new List<string>();
        [CommandLine("p"), CommandLine("probe")]
        public List<string> ProbePaths { get { return probePaths; } }

        private readonly List<string> inputs = new List<string>();
        [CommandLine("")]
        public List<string> Inputs { get { return inputs; } }

        [CommandLine("t"), CommandLine("type")]
        public string TypeName { get; set; }

        [CommandLine("o"), CommandLine("out")]
        public string AssemblyName { get; set; }

        [CommandLine("?"), CommandLine("help"), CommandLine("h")]
        public bool Help { get; set; }


        public bool SanityCheck()
        {
            bool allGood = true;
            if (inputs.Count == 0)
            {
                Console.Error.WriteLine("No input assemblies");
                allGood = false;
            }
            if (string.IsNullOrEmpty(TypeName))
            {
                Console.Error.WriteLine("No serializer type-name specified");
                allGood = false;
            }
            if (string.IsNullOrEmpty(AssemblyName))
            {
                Console.Error.WriteLine("No output assembly file specified");
                allGood = false;
            }

            if (string.IsNullOrEmpty(Framework))
            {
                Console.WriteLine("No framework specified; defaulting to " + Environment.Version);
                probePaths.Add(Path.GetDirectoryName(typeof(string).Assembly.Location));
            }
            else
            {
                if (Directory.Exists(Framework))
                { // very clear and explicit
                    probePaths.Add(Framework);
                }
                else
                {
                    string root = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
                    if (string.IsNullOrEmpty(root)) root = Environment.GetEnvironmentVariable("ProgramFiles");
                    root = Path.Combine(root, @"Reference Assemblies\Microsoft\Framework\");
                    if(!Directory.Exists(root)) {
                        Console.Error.WriteLine("Framework reference assemblies root folder could not be found");
                        allGood = false;
                    } else {
                        string frameworkRoot = Path.Combine(root, Framework);
                        if (Directory.Exists(frameworkRoot))
                        {
                            // fine
                            probePaths.Add(frameworkRoot);
                        }
                        else
                        {
                            Console.Error.WriteLine("Framework not found: " + Framework);
                            Console.Error.WriteLine("Available frameworks are:");
                            string[] files = Directory.GetFiles(root, "mscorlib.dll", SearchOption.AllDirectories);
                            foreach (var file in files)
                            {
                                string dir = Path.GetDirectoryName(file);
                                if (dir.StartsWith(root)) dir = dir.Substring(root.Length);
                                Console.Error.WriteLine(dir);
                            }
                            allGood = false;
                        }
                    }
                }
            }
            foreach (var inp in inputs)
            {
                if(File.Exists(inp)) {
                    string dir = Path.GetDirectoryName(inp);
                    if(!probePaths.Contains(dir)) probePaths.Add(dir);
                }
                else
                {
                    Console.Error.WriteLine("Input not found: " + inp);
                    allGood = false;
                }                
            }
            return allGood;
        }

        IEnumerable<string> ProbeForFiles(string file)
        {
            foreach (var probePath in probePaths)
            {
                string combined = Path.Combine(probePath, file);
                if (File.Exists(combined))
                {
                    yield return combined;
                }
            }
        }
        public bool Execute()
        {
            // model to work with
            var model = TypeModel.Create();

            model.AssemblyResolve += (sender, args) =>
            {
                string nameOnly = args.Name.Split(',')[0];
                var uni = ((IKVM.Reflection.Universe)sender);
                foreach (var tmp in uni.GetAssemblies())
                {
                    if (tmp.GetName().Name == nameOnly) return tmp;
                }
                var asm = ResolveNewAssembly(uni, nameOnly + ".dll");
                if(asm != null) return asm;
                asm = ResolveNewAssembly(uni, nameOnly + ".exe");
                if(asm != null) return asm;
                
                throw new InvalidOperationException("All assemblies must be resolved explicity; did not resolve: " + args.Name);
            };
            bool allGood = true;
            if (ResolveNewAssembly(model.Universe, "mscorlib.dll") == null)
            {
                Console.Error.WriteLine("mscorlib.dll not found!");
                allGood = false;
            }
            ResolveNewAssembly(model.Universe, "System.dll"); // not so worried about whether that one exists...
            if (ResolveNewAssembly(model.Universe, "protobuf-net.dll") == null)
            {
                Console.Error.WriteLine("protobuf-net.dll not found!");
                allGood = false;
            }
            if (!allGood) return false;
            var assemblies = new List<IKVM.Reflection.Assembly>();
            MetaType metaType = null;
            foreach (var file in inputs)
            {
                assemblies.Add(model.Load(file));
            }
            // scan for obvious protobuf types
            var attributeType = model.Universe.GetType("System.Attribute, mscorlib");
            var toAdd = new List<IKVM.Reflection.Type>();
            foreach (var asm in assemblies)
            {
                foreach (var type in asm.GetTypes())
                {
                    bool add = false;
                    if (!(type.IsClass || type.IsValueType)) continue;

                    foreach (var attrib in type.__GetCustomAttributes(attributeType, true))
                    {
                        string name = attrib.Constructor.DeclaringType.FullName;
                        switch(name) 
                        {
                            case "ProtoBuf.ProtoContractAttribute":
                                add = true;
                                break;
                        }
                        if (add) break;
                    }
                    if (add) toAdd.Add(type);
                }
            }

            if (toAdd.Count == 0)
            {
                Console.Error.WriteLine("No [ProtoContract] types found; nothing to do!");
                return false;
            }

            // add everything we explicitly know about
            toAdd.Sort((x, y) => string.Compare(x.FullName, y.FullName));            
            foreach (var type in toAdd)
            {
                Console.WriteLine("Adding " + type.FullName + "...");
                var tmp = model.Add(type, true);
                if (metaType == null) metaType = tmp; // use this as the template for the framework version
            }
            // add everything else we can find
            model.Cascade();
            var inferred = new List<IKVM.Reflection.Type>();
            foreach (MetaType type in model.GetTypes())
            {
                if(!toAdd.Contains(type.Type)) inferred.Add(type.Type);
            }
            inferred.Sort((x, y) => string.Compare(x.FullName, y.FullName));
            foreach (var type in inferred)
            {
                Console.WriteLine("Adding " + type.FullName + "...");
            }

            
            // configure the output file/serializer name, and borrow the framework particulars from
            // the type we loaded
            var options = new RuntimeTypeModel.CompilerOptions
            {
                TypeName = TypeName,
                OutputPath = AssemblyName
            };
            if (metaType != null)
            {
                options.SetFrameworkOptions(metaType);
            }
            Console.WriteLine("Compiling to " + options.OutputPath + "...");
            // GO WORK YOUR MAGIC, CRAZY THING!!
            model.Compile(options);
            Console.WriteLine("All done");

            return true;

        }

        private IKVM.Reflection.Assembly ResolveNewAssembly(IKVM.Reflection.Universe uni, string fileName)
        {
            foreach (var match in ProbeForFiles(fileName))
            {
                var asm = uni.LoadFile(match);
                if (asm != null)
                {
                    Console.WriteLine("Resolved " + match);
                    return asm;
                }
            }
            return null;
        }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class CommandLineAttribute : Attribute
    {
        public static bool TryParse<T>(string[] args, out T result) where T : class, new()
        {
            result = new T();
            bool allGood = true;
            var props = typeof(T).GetProperties();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].Trim().TrimStart('/','+','-');
                string prefix, value;
                int idx = arg.IndexOf(':');
                if (idx < 0)
                {
                    prefix = "";
                    value = arg;
                }
                else
                {
                    prefix = arg.Substring(0,idx);
                    value = arg.Substring(idx + 1);
                }
                System.Reflection.PropertyInfo foundProp = null;
                foreach (var prop in props)
                {
                    foreach (CommandLineAttribute atttib in prop.GetCustomAttributes(typeof(CommandLineAttribute), true))
                    {
                        if (atttib.Prefix == prefix)
                        {
                            foundProp = prop;
                            break;
                        }
                    }
                    if (foundProp != null) break;
                }

                if (foundProp == null)
                {
                    allGood = false;
                    Console.Error.WriteLine("Argument not understood: " + arg);
                }
                else
                {
                    if (foundProp.PropertyType == typeof(string))
                    {
                        foundProp.SetValue(result, value, null);
                    }
                    else if (foundProp.PropertyType == typeof(List<string>))
                    {
                        ((List<string>)foundProp.GetValue(result, null)).Add(value);
                    }
                    else if (foundProp.PropertyType == typeof(bool))
                    {
                        foundProp.SetValue(result, true, null);
                    }
                }
            }

            return allGood;
        }
        private readonly string prefix;
        public CommandLineAttribute(string prefix) { this.prefix = prefix; }
        public string Prefix { get { return prefix; } }
    }

}
