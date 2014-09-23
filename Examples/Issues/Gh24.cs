using NUnit.Framework;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Examples.Issues
{
    [TestFixture]
    // https://github.com/mgravell/protobuf-net/issues/24
    // assemblies loaded multiple times cause incorrect behavior of ProtoInclude
    public class Gh24
    {
        DirectoryInfo tempDir;
        AppDomain domain;
        FileInfo assemblyPath;

        static readonly byte[] testData = { 0x0a, 0x05, 0x15, 0x46, 0x41, 0x49, 0x4c };

        const string ASSEMBLY_NAME = "Examples.Issues.Gh24";

        [SetUp]
        public void SetUp()
        {
            tempDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "protobuf-net-gh24-" + Convert.ToBase64String(BitConverter.GetBytes(DateTime.UtcNow.Ticks)).Replace('/', '-').Substring(0, 11)));
            tempDir.Create();

            var subdir = tempDir.CreateSubdirectory("payload");
            assemblyPath = new FileInfo(Path.Combine(subdir.FullName, ASSEMBLY_NAME + ".dll"));
            var references = CreateAssembly(assemblyPath);

            var setup = new AppDomainSetup
            {
                ApplicationBase = tempDir.FullName
            };
            domain = AppDomain.CreateDomain("Gh24", null, setup);

            var initializer = (Initializer)domain.CreateInstanceFromAndUnwrap(typeof(Initializer).Assembly.Location, typeof(Initializer).FullName);
            var assemblyPaths = references.ToDictionary(r => r, r => Assembly.Load(r).Location);
            assemblyPaths[new AssemblyName(ASSEMBLY_NAME)] = assemblyPath.FullName;
            initializer.Initialize(assemblyPaths);
        }

        static IEnumerable<AssemblyName> CreateAssembly(FileInfo path)
        {
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(ASSEMBLY_NAME), AssemblyBuilderAccess.Save, path.DirectoryName);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(ASSEMBLY_NAME + ".dll");

            var baseTypeBuilderInfo = BuildBaseType(moduleBuilder);
            var baseTypeBuilder = baseTypeBuilderInfo.Item1;
            var childTypeBuilderInfo = BuildChildType(moduleBuilder, baseTypeBuilder, baseTypeBuilderInfo.Item2);
            var childTypeBuilder = childTypeBuilderInfo.Item1;
            baseTypeBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(ProtoIncludeAttribute).GetConstructor(new[] { typeof(int), typeof(Type) }), new object[] { 1, childTypeBuilder }));
            var programBuilder = BuildProgramType(moduleBuilder, childTypeBuilder, childTypeBuilderInfo.Item2);

            baseTypeBuilder.CreateType();
            childTypeBuilder.CreateType();
            programBuilder.CreateType();

            assemblyBuilder.Save(path.Name);

            return assemblyBuilder.GetReferencedAssemblies();
        }

        static Tuple<TypeBuilder, ConstructorBuilder> BuildBaseType(ModuleBuilder moduleBuilder)
        {
            var baseTypeBuilder = moduleBuilder.DefineType(ASSEMBLY_NAME + ".BaseType", TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit, typeof(Object));
            baseTypeBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(ProtoContractAttribute).GetConstructor(Type.EmptyTypes), new object[0]));
            var baseConstructorBuilder = baseTypeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
            return Tuple.Create(baseTypeBuilder, baseConstructorBuilder);
        }

        static Tuple<TypeBuilder, FieldBuilder> BuildChildType(ModuleBuilder moduleBuilder, Type baseType, ConstructorInfo baseConstructor)
        {
            var childTypeBuilder = moduleBuilder.DefineType(ASSEMBLY_NAME + ".ChildType", TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit, baseType);
            childTypeBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(ProtoContractAttribute).GetConstructor(Type.EmptyTypes), new object[0]));
            var statusBuilder = childTypeBuilder.DefineField("Status", typeof(string), FieldAttributes.Public);
            statusBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(ProtoMemberAttribute).GetConstructor(new[] { typeof(int) }), new object[] { 1 }));
            statusBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(DefaultValueAttribute).GetConstructor(new[] { typeof(string) }), new object[] { "Pass" }));
            var failBuilder = childTypeBuilder.DefineField("Fail", typeof(int), FieldAttributes.Public);
            failBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(ProtoMemberAttribute).GetConstructor(new[] { typeof(int) }), new object[] { 2 }, new[] { typeof(ProtoMemberAttribute).GetProperty("DataFormat") }, new object[] { DataFormat.FixedSize }));
            var childConstructorBuilder = childTypeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, CallingConventions.HasThis, Type.EmptyTypes);
            var generator = childConstructorBuilder.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, baseConstructor);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldstr, "Pass");
            generator.Emit(OpCodes.Stfld, statusBuilder);
            return Tuple.Create(childTypeBuilder, statusBuilder);
        }

        static TypeBuilder BuildProgramType(ModuleBuilder moduleBuilder, Type childType, FieldInfo statusField)
        {
            var programBuilder = moduleBuilder.DefineType(ASSEMBLY_NAME + ".Program", TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Abstract | TypeAttributes.Sealed, typeof(Object));
            var mainBuilder = programBuilder.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static, typeof(String), new[] { typeof(byte[]) });
            var generator = mainBuilder.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Newobj, typeof(MemoryStream).GetConstructor(new[] { typeof(byte[]) }));
            generator.EmitCall(OpCodes.Call, typeof(Serializer).GetMethods(BindingFlags.Public | BindingFlags.Static).Single(m => m.Name == "Deserialize" && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == 1).MakeGenericMethod(new[] { childType }), null);
            generator.Emit(OpCodes.Ldfld, statusField);
            generator.Emit(OpCodes.Ret);
            return programBuilder;
        }

        [TearDown]
        public void TearDown()
        {
            AppDomain.Unload(domain);
            tempDir.Delete(true);
        }

        [Test]
        public void TestKnownTypeResolution()
        {
            var invoker = (Invoker)domain.CreateInstanceFromAndUnwrap(typeof(Invoker).Assembly.Location, typeof(Invoker).FullName);
            var result = invoker.Invoke(testData);
            Assert.AreEqual("Pass", result);
        }

        public class Initializer : MarshalByRefObject
        {
            public void Initialize(Dictionary<AssemblyName, string> assemblies)
            {
                var cache = assemblies.ToDictionary(a => a.Key, a => new Lazy<Assembly>(() => Assembly.LoadFile(a.Value)));
                AppDomain.CurrentDomain.AssemblyResolve += (o, e) =>
                {
                    var request = new AssemblyName(e.Name);
                    return cache.Where(a => AssemblyName.ReferenceMatchesDefinition(request, a.Key)).Select(a => a.Value.Value).FirstOrDefault();
                };

                var main = Assembly.Load(ASSEMBLY_NAME);

                // make the assembly available in the application base directory after loading from subdirectory to reproduce issue
                var copyPath = Path.Combine(Path.GetDirectoryName(main.Location), "..", Path.GetFileName(main.Location));
                File.Copy(main.Location, copyPath);
                Assembly.Load(AssemblyName.GetAssemblyName(copyPath));
            }
        }

        public class Invoker : MarshalByRefObject
        {
            public string Invoke(byte[] bytes)
            {
                return (string)Type.GetType(Assembly.CreateQualifiedName(ASSEMBLY_NAME, ASSEMBLY_NAME + ".Program"), true, false)
                    .GetMethod("Main", BindingFlags.Public | BindingFlags.Static)
                    .Invoke(null, new object[] { bytes });
            }
        }
    }
}
