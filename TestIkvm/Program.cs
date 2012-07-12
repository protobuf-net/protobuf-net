using System;
using System.IO;
using IKVM.Reflection;
using ProtoBuf.Meta;

namespace TestIkvm
{
    [System.ComponentModel.Description("Just testing")]
    class Program
    {
        static void Main()
        {
            
            var model = TypeModel.Create();
            string root = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            if (string.IsNullOrEmpty(root)) root = Environment.GetEnvironmentVariable("ProgramFiles");
            
            root = Path.Combine(root, @"Reference Assemblies\Microsoft\Framework\.NETCore\v4.5");
            string[] probePaths = { root, @"..\..\..\MetroDto\bin\x86\release" };
            model.AssemblyResolve += (sender,args) => Resolve(probePaths, sender, args);

            model.Load(Path.Combine(root, @"System.Runtime.dll"));
            model.Load(Path.Combine(root, @"System.Runtime.Serialization.Primitives.dll"));

            var metaType = model.Add("DAL.DatabaseCompat, MetroDto", true);
            var options = new RuntimeTypeModel.CompilerOptions
            {
                TypeName = "Foo",
                OutputPath = "Foo.dll"
            };
            options.SetFrameworkOptions(metaType);
            model.Compile(options);

        }

        static IKVM.Reflection.Assembly Resolve(string[] probePaths, object sender, IKVM.Reflection.ResolveEventArgs args)
        {
            string name = args.Name;
            if(string.IsNullOrEmpty(name)) return null;
            string[] systemRuntime = { "mscorlib", "System" };
            for (int i = 0; i < systemRuntime.Length; i++)
            {
                if(name == systemRuntime[i] || name.StartsWith(systemRuntime[i] + ","))
                {
                    name = "System.Runtime";
                    break;
                }
            }
            if (name == "protobuf-net_IKVM" || name.StartsWith("protobuf-net_IKVM"))
            {
                name = "protobuf-net";
            }
            Universe universe = ((Universe)sender);
            foreach (var asm in universe.GetAssemblies())
            {
                if (asm.FullName == name || asm.GetName().Name == name)
                {
                    return asm;
                }
            }
            string part = name.Split(',')[0];
            string dllName =part + ".dll", exeName = part + ".exe";
            foreach (var probe in probePaths)
            {
                string tmp;
                if (File.Exists(tmp = Path.Combine(probe, dllName)))
                {
                    Console.WriteLine("Loading " + tmp);
                    return universe.LoadFile(tmp);
                }
                if (File.Exists(tmp = Path.Combine(probe, exeName)))
                {
                    Console.WriteLine("Loading " + tmp);
                    return universe.LoadFile(tmp);
                }
            }
            throw new InvalidOperationException("Failed to resolve: " + args.Name);
        }
    }
}
