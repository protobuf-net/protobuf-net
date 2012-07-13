using System;
using System.IO;
using IKVM.Reflection;
using ProtoBuf.Meta;

namespace TestIkvm
{
    class Program
    {
        // this sample loads a Metro/WinRT assembly into a TypeModel, explores the model with
        // IKVM-reflection, and builds a Metro/WinRT serialization assembly matching the
        // input assembly. We can then reference and use this serialization library from
        // our WinRT application, and: zero reflection
        static void Main()
        {

            // model to work with
            var model = TypeModel.Create();

            // all of the following is configuring IKVM to target the Metro/WinRT platform
            string root = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            if (string.IsNullOrEmpty(root)) root = Environment.GetEnvironmentVariable("ProgramFiles");
            root = Path.Combine(root, @"Reference Assemblies\Microsoft\Framework\.NETCore\v4.5");
            string[] probePaths = { root, @"..\..\..\MetroDto\bin\x86\release" };
            //model.AssemblyResolve += (sender,args) => Resolve(probePaths, sender, args);

            model.AssemblyResolve += (sender, args) =>
            {
                string nameOnly = args.Name.Split(',')[0];
                if (nameOnly == "protobuf-net_IKVM") nameOnly = "protobuf-net";
                foreach (var asm in ((Universe)sender).GetAssemblies())
                {
                    if(asm.GetName().Name == nameOnly) return asm;
                }
                throw new InvalidOperationException("All assemblies must be resolved explicity; did not resolve: " + args.Name);
            };
            model.Load(Path.Combine(root, "mscorlib.dll"));
            model.Load(Path.Combine(root, "System.Runtime.dll"));
            foreach (var file in Directory.GetFiles(root, "*.dll"))
            {
                if (Path.GetFileName(file) == "mscorlib.dll") continue;
                if (Path.GetFileName(file) == "System.Runtime.dll") continue;
                model.Load(file);
            }
            // load our actual library/model dll
            model.Load(@"..\..\..\MetroDto\bin\x86\release\protobuf-net.dll");
            model.Load(@"..\..\..\MetroDto\bin\x86\release\MetroDto.dll");

            // load our root types into the model (it will cascade them automatically)
            var metaType = model.Add("DAL.DatabaseCompat, MetroDto", true);
            model.Add("SM2Stats, MetroDto", true);

            // configure the output file/serializer name, and borrow the framework particulars from
            // the type we loaded
            var options = new RuntimeTypeModel.CompilerOptions
            {
                TypeName = "Foo",
                OutputPath = "Foo.dll"
            };
            options.SetFrameworkOptions(metaType);

            // GO WORK YOUR MAGIC, CRAZY THING!!
            model.Compile(options);
        }

        // this big-chunk-o redirection exists to cater for the fact that in WinRT a lot of types have
        // moved around; find them in their new homes! this also does some voodoo to mimic
        // regular probling paths, allowing it to look for all likely-looking assemblies in a few
        // locations, rather than having to specify everything explicitly
        static IKVM.Reflection.Assembly Resolve(string[] probePaths, object sender, IKVM.Reflection.ResolveEventArgs args)
        {
            string name = args.Name;
            if(string.IsNullOrEmpty(name)) return null;
            //string[] systemRuntime = { "mscorlib", "System" };
            //for (int i = 0; i < systemRuntime.Length; i++)
            //{
            //    if(name == systemRuntime[i] || name.StartsWith(systemRuntime[i] + ","))
            //    {
            //        name = "System.Runtime";
            //        break;
            //    }
            //}
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
