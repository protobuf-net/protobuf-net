using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ProtoBuf.AotRefGen
{
    /// <summary>
    /// Emits "what ref-emit would have written" for each AOT golden fixture, as C#.
    /// </summary>
    /// <remarks>
    /// This exists so that the source generator's expected output is derived from the working
    /// ref-emit implementation rather than guessed at. Run it after adding or changing a fixture,
    /// and use the resulting <c>*.reference.cs</c> as the target when writing the emitter; it is
    /// not consumed by the tests, it is a reviewing aid.
    /// </remarks>
    internal static class Program
    {
        private const string DefaultRelativeFixtureDir = @"..\..\..\..\BuildToolsUnitTests\Aot\Data";

        private static int Main(string[] args)
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var fixtureDir = Path.GetFullPath(args.Length > 0
                    ? args[0]
                    : Path.Combine(baseDir, DefaultRelativeFixtureDir));

                if (!Directory.Exists(fixtureDir))
                {
                    Console.Error.WriteLine($"fixture directory not found: {fixtureDir}");
                    return 1;
                }

                // AssemblyBuilder.Save only accepts a bare file name, so emit beside our own binaries;
                // that also lets the decompiler resolve protobuf-net types when reading it back
                Directory.SetCurrentDirectory(baseDir);

                var models = FindModels().ToList();
                if (models.Count == 0)
                {
                    Console.Error.WriteLine("no [ProtoModel] types found; are the fixtures linked?");
                    return 1;
                }

                foreach (var model in models) Emit(model, fixtureDir);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"{ex.GetType().Name}: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
                return 1;
            }
        }

        private static IEnumerable<Type> FindModels()
            => from type in typeof(Program).Assembly.GetTypes()
               where type.IsDefined(typeof(ProtoModelAttribute), inherit: false)
               orderby type.Name
               select type;

        private static void Emit(Type modelType, string fixtureDir)
        {
            var seeds = modelType
                .GetCustomAttributes(typeof(ProtoSerializableAttribute), inherit: false)
                .Cast<ProtoSerializableAttribute>()
                .Select(static x => x.Type)
                .ToList();

            if (seeds.Count == 0)
            {
                Console.Error.WriteLine($"{modelType.Name}: no [ProtoSerializable] seeds; skipping");
                return;
            }

            var model = RuntimeTypeModel.Create();
            foreach (var seed in seeds) model.Add(seed, applyDefaultBehaviour: true);

            var dllName = modelType.Name + ".dll";
            if (File.Exists(dllName)) File.Delete(dllName);
            model.Compile(modelType.Name, dllName);

            var settings = new DecompilerSettings(LanguageVersion.CSharp10_0)
            {
                ThrowOnAssemblyResolveErrors = false,
                ShowXmlDocumentation = false,
            };
            var code = new CSharpDecompiler(Path.GetFullPath(dllName), settings).DecompileWholeModuleAsString();

            // fixtures follow the convention <Name>.input.cs declaring model type <Name>Model
            var stem = modelType.Name.EndsWith("Model", StringComparison.Ordinal) && modelType.Name.Length > "Model".Length
                ? modelType.Name.Substring(0, modelType.Name.Length - "Model".Length)
                : modelType.Name;

            var target = Path.Combine(fixtureDir, stem + ".reference.cs");
            File.WriteAllText(target, code);
            Console.WriteLine($"{modelType.Name}: {seeds.Count} seed(s) -> {target}");
        }
    }
}
