using NUnit.Framework;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Lifetime;

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
        ClientSponsor sponsor;
        RemoteSide remoteSide;

        const string ASSEMBLY_NAME = "LateLoaded";

        [SetUp]
        public void SetUp()
        {
            tempDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "protobuf-net-gh24-" + Convert.ToBase64String(BitConverter.GetBytes(DateTime.UtcNow.Ticks)).Replace('/', '-').Substring(0, 11)));
            tempDir.Create();

            var subdir = tempDir.CreateSubdirectory("temp");
            assemblyPath = new FileInfo(Path.Combine(subdir.FullName, ASSEMBLY_NAME + ".dll"));
            CopyAssembly(assemblyPath);

            var setup = new AppDomainSetup
            {
                ApplicationBase = tempDir.FullName,
                // setting this to true allows both tests to pass
                DisallowApplicationBaseProbing = false
            };
            domain = AppDomain.CreateDomain("Gh24", null, setup);

            sponsor = new ClientSponsor();

            remoteSide = (RemoteSide)domain.CreateInstanceFromAndUnwrap(typeof(RemoteSide).Assembly.Location, typeof(RemoteSide).FullName);
            sponsor.Register(remoteSide);

            var references = new[] { typeof(Serializer).Assembly };
            var assemblyPaths = references.ToDictionary(a => a.GetName(), a => a.Location);
            assemblyPaths[new AssemblyName(ASSEMBLY_NAME)] = assemblyPath.FullName;
            remoteSide.Initialize(assemblyPaths);
        }

        static void CopyAssembly(FileInfo path)
        {
            var source = new FileInfo(Path.Combine(Path.GetDirectoryName(new Uri(typeof(Gh24).Assembly.GetName(false).EscapedCodeBase).LocalPath), ASSEMBLY_NAME + ".dll"));
            source.CopyTo(path.FullName);
        }

        [TearDown]
        public void TearDown()
        {
            AppDomain.Unload(domain);
            tempDir.Delete(true);
        }

        [Test]
        public void TestAppBaseTypeResolution()
        {
            var result = remoteSide.InvokeAppBase();
            Assert.AreEqual("Base,Child", result);
        }

        [Test]
        public void TestOtherTypeResolution()
        {
            var result = remoteSide.InvokeOther();
            Assert.AreEqual("Base,Child", result);
        }

        public class RemoteSide : MarshalByRefObject
        {
            Type appBaseType;
            Type otherType;

            public void Initialize(Dictionary<AssemblyName, string> assemblies)
            {
                var cache = assemblies.ToDictionary(a => a.Key, a => new Lazy<Assembly>(() => Assembly.LoadFile(a.Value)));
                AppDomain.CurrentDomain.AssemblyResolve += (o, e) =>
                {
                    var request = new AssemblyName(e.Name);
                    return cache.Where(a => AssemblyName.ReferenceMatchesDefinition(request, a.Key)).Select(a => a.Value.Value).FirstOrDefault();
                };

                var main = Assembly.Load(ASSEMBLY_NAME);
                otherType = main.GetType(ASSEMBLY_NAME + ".Bar", true, false);

                // make the assembly available in the application base directory after loading from subdirectory to reproduce issue
                var copyPath = Path.Combine(Path.GetDirectoryName(main.Location), "..", Path.GetFileName(main.Location));
                File.Copy(main.Location, copyPath);
                var appBaseMain = Assembly.Load(main.GetName());
                appBaseType = appBaseMain.GetType(ASSEMBLY_NAME + ".Bar", true, false);
            }

            string Test(Type type)
            {
                var baseProp = type.GetProperty("BaseProp");
                var childProp = type.GetProperty("ChildProp");
                var obj = Activator.CreateInstance(type);
                baseProp.SetValue(obj, "Base", null);
                childProp.SetValue(obj, "Child", null);

                MethodInfo method = typeof(Serializer).GetMethod("DeepClone").MakeGenericMethod(type);
                var clone = method.Invoke(null, new object[] { obj });

                var baseValue = (string)baseProp.GetValue(clone, null);
                var childValue = (string)childProp.GetValue(clone, null);
                return baseValue + "," + childValue;
            }

            public string InvokeAppBase()
            {
                return Test(appBaseType);
            }

            public string InvokeOther()
            {
                return Test(otherType);
            }
        }
    }
}
