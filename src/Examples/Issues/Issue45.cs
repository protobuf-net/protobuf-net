#if !COREFX
using System.Reflection;
using Xunit;
using System;
using ProtoBuf;
using System.IO;
using Xunit.Abstractions;

namespace Examples.Issues
{
    public class LateLoadedTests
    {
        private ITestOutputHelper Log { get; }
        public LateLoadedTests(ITestOutputHelper _log) => Log = _log;

        [Fact]
        public void TestLateLoad()
        {
            const string dllPath = "LateLoaded.dll";
            if(!File.Exists(dllPath))
            {
                Log.WriteLine("Late-load dll not found {0}; test inconclusive", dllPath);
                return;
            }
            Assembly assembly = Assembly.LoadFrom(dllPath);
            Type type = assembly.GetType("LateLoaded.Foo");
            Assert.NotNull(type); //, "Resolve type");

            object obj = Activator.CreateInstance(type, nonPublic: true);
            const string EXPECTED = "Some value";
            type.GetProperty("BaseProp").SetValue(obj, EXPECTED, null);

            MethodInfo method = typeof(Serializer).GetMethod("DeepClone").MakeGenericMethod(type);

            object clone = method.Invoke(null, new object[] { obj });
            Assert.NotNull(clone); //, "Create clone");
            Assert.NotSame(obj, clone); //, "Clone different instance");
            Assert.IsType(type, clone); //, "Clone correct type");
            object value = type.GetProperty("BaseProp").GetValue(clone, null);
            Assert.Equal(EXPECTED, value); //, "Clone value");
        }

        static LateLoadedTests()
        {   // static-ctor to make sure we only do this once
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
        }

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {   // make sure we don't get confused with different versions of protobuf-net
            if(args.Name.StartsWith("protobuf-net, Version="))
            {
                return typeof (ProtoContractAttribute).Assembly;
            }
            return null;
        }
    }
}
#endif