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

                foreach (var model in models)
                {
                    // one fixture that ref-emit cannot compile must not cost every other reference;
                    // a deliberate divergence (non-public setters, say) simply has no reference file
                    try
                    {
                        Emit(model, fixtureDir);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"{model.Name}: ref-emit declined - {ex.Message}");
                    }
                }
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

        /// <summary>
        /// Replay the model's <c>[ProtoSurrogate]</c> declarations onto the reference model, which is
        /// what <c>RuntimeTypeModel.SetSurrogate</c> exists for.
        /// </summary>
        /// <remarks>
        /// Without this the reference model has never heard of the surrogate and serializes the
        /// underlying type directly (or refuses to), so the comparison would be against a
        /// differently-configured model rather than against ref-emit.
        /// <para>
        /// Declarations are gathered least-to-most specific — the assembly first, then the model —
        /// exactly as the generator gathers them, so the more specific one wins.
        /// </para>
        /// </remarks>
        private static void ApplySurrogates(RuntimeTypeModel model, Type modelType)
        {
            var declarations = modelType.Assembly
                .GetCustomAttributes(typeof(ProtoSurrogateAttribute), inherit: false)
                .Cast<ProtoSurrogateAttribute>()
                .Concat(modelType.GetCustomAttributes(typeof(ProtoSurrogateAttribute), inherit: false)
                    .Cast<ProtoSurrogateAttribute>());

            foreach (var declaration in declarations)
            {
                if (declaration.Converter is null)
                {
                    // the cast form: MetaType.SetSurrogate(Type) is the public equivalent
                    model.Add(declaration.Type, applyDefaultBehaviour: false)
                        .SetSurrogate(declaration.Surrogate);
                    continue;
                }

                // the named-method form, which is how a type with no usable operators is hooked up.
                // Only the generic SetSurrogate takes conversion delegates, so it is built here.
                var toSurrogate = MakeConverter(declaration.Converter, declaration.ToSurrogate,
                    declaration.Type, declaration.Surrogate);
                var toUnderlying = MakeConverter(declaration.Converter, declaration.ToType,
                    declaration.Surrogate, declaration.Type);

                typeof(RuntimeTypeModel)
                    .GetMethod(nameof(RuntimeTypeModel.SetSurrogate))
                    .MakeGenericMethod(declaration.Type, declaration.Surrogate)
                    .Invoke(model, new object[]
                    {
                        toSurrogate, toUnderlying, DataFormat.Default, CompatibilityLevel.NotSpecified,
                    });
            }
        }

        private static Delegate MakeConverter(Type converter, string methodName, Type from, Type to)
        {
            var method = converter.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static,
                null, new[] { from }, null)
                ?? throw new InvalidOperationException(
                    $"{converter.Name}.{methodName}({from.Name}) not found");

            return Delegate.CreateDelegate(typeof(Func<,>).MakeGenericType(from, to), method);
        }

        /// <summary>
        /// Replay the model's <c>[ProtoSubType]</c> declarations onto the reference model, which is
        /// what <c>MetaType.AddSubType</c> exists for.
        /// </summary>
        /// <remarks>
        /// Without this the reference model has never heard of the linkage and serializes the
        /// sub-type as a standalone contract, so the comparison would be against a
        /// differently-configured model rather than against ref-emit - the same trap the surrogate
        /// replay above exists for. Gathered from the assembly and then the model, as the generator
        /// gathers them; unlike surrogates these accumulate rather than override.
        /// </remarks>
        private static void ApplySubTypes(RuntimeTypeModel model, Type modelType)
        {
            var declarations = modelType.Assembly
                .GetCustomAttributes(typeof(ProtoSubTypeAttribute), inherit: false)
                .Cast<ProtoSubTypeAttribute>()
                .Concat(modelType.GetCustomAttributes(typeof(ProtoSubTypeAttribute), inherit: false)
                    .Cast<ProtoSubTypeAttribute>());

            foreach (var declaration in declarations)
            {
                model.Add(declaration.BaseType, applyDefaultBehaviour: true)
                    .AddSubType(declaration.FieldNumber, declaration.SubType,
                        declaration.IsGroup == true ? DataFormat.Group : DataFormat.Default);
            }
        }

        /// <summary>
        /// Replay the model's [ProtoSerializer] declarations onto the reference model, which is what
        /// MetaType.SerializerType exists for. Open declarations are closed over every matching
        /// instantiation reachable from the seeds' member graphs.
        /// </summary>
        private static void ApplySerializers(RuntimeTypeModel model, Type modelType, List<Type> seeds)
        {
            var declarations = modelType.Assembly
                .GetCustomAttributes(typeof(ProtoSerializerAttribute), inherit: false)
                .Cast<ProtoSerializerAttribute>()
                .Concat(modelType.GetCustomAttributes(typeof(ProtoSerializerAttribute), inherit: false)
                    .Cast<ProtoSerializerAttribute>())
                .ToList();
            if (declarations.Count == 0) return;

            var closed = new List<(Type Type, Type Serializer)>();
            var open = new List<(Type Definition, Type Serializer)>();
            foreach (var declaration in declarations)
            {
                if (declaration.Type.IsGenericTypeDefinition) open.Add((declaration.Type, declaration.Serializer));
                else closed.Add((declaration.Type, declaration.Serializer));
            }

            foreach (var seed in seeds)
            foreach (var reached in ReachableTypes(seed))
            {
                if (!reached.IsConstructedGenericType) continue;
                foreach (var (definition, serializer) in open)
                {
                    if (reached.GetGenericTypeDefinition() != definition) continue;
                    if (closed.Any(x => x.Type == reached)) continue;
                    closed.Add((reached, serializer.MakeGenericType(reached.GetGenericArguments())));
                }
            }

            foreach (var (underlying, serializer) in closed)
            {
                model.Add(underlying, applyDefaultBehaviour: false).SerializerType = serializer;
            }
        }

        /// <summary>
        /// Every type reachable from a contract's public members. Member recursion stays inside the
        /// fixture assembly; generic arguments and element types are always followed.
        /// </summary>
        private static IEnumerable<Type> ReachableTypes(Type root)
        {
            var seen = new HashSet<Type>();
            var stack = new Stack<Type>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (!seen.Add(current)) continue;
                yield return current;
                if (Nullable.GetUnderlyingType(current) is { } wrapped) stack.Push(wrapped);
                if (current.IsConstructedGenericType)
                {
                    foreach (var argument in current.GenericTypeArguments) stack.Push(argument);
                }
                if (current.IsArray) stack.Push(current.GetElementType()!);
                if (current.Assembly != root.Assembly) continue;
                foreach (var property in current.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    stack.Push(property.PropertyType);
                }
                foreach (var field in current.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    stack.Push(field.FieldType);
                }
            }
        }

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

            // the model-level options have to be mirrored onto the reference model, or it is not the
            // same model: AllowParseableTypes changes the wire form of any qualifying member, and
            // without this the reference would silently be the *unparsed* shape
            model.AllowParseableTypes = modelType
                .GetCustomAttributes(typeof(ProtoModelAttribute), inherit: false)
                .Cast<ProtoModelAttribute>()
                .Any(static x => x.AllowParseableTypes);

            ApplySurrogates(model, modelType);
            ApplySerializers(model, modelType, seeds);
            ApplySubTypes(model, modelType);

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

            // stamp which fixture this came from, so a reference that was never re-run can be told from
            // one where ref-emit genuinely produced nothing - see ReferenceProvenance
            var inputName = stem + ".input.cs";
            var inputPath = Path.Combine(fixtureDir, inputName);
            var header = File.Exists(inputPath)
                ? ReferenceProvenance.Header(inputName, ReferenceProvenance.Hash(File.ReadAllText(inputPath)))
                : "";
            File.WriteAllText(target, header + code);
            Console.WriteLine($"{modelType.Name}: {seeds.Count} seed(s) -> {target}");
        }
    }
}
