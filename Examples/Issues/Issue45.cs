using System.Reflection;
using NUnit.Framework;
using System;
using ProtoBuf;

namespace Examples.Issues
{
    [TestFixture]
    public class LateLoadedTests
    {
        [Test]
        public void TestLateLoad()
        {
            Assembly assembly = Assembly.LoadFrom("LateLoaded.dll");
            Type type = assembly.GetType("LateLoaded.Foo");
            Assert.IsNotNull(type, "Resolve type");

            object obj = Activator.CreateInstance(type);
            const string EXPECTED = "Some value";
            type.GetProperty("BaseProp").SetValue(obj, EXPECTED, null);

            MethodInfo method = typeof(Serializer).GetMethod("DeepClone").MakeGenericMethod(type);

            object clone = method.Invoke(null, new object[] { obj });
            Assert.IsNotNull(clone, "Create clone");
            Assert.AreNotSame(obj, clone, "Clone different instance");
            Assert.IsInstanceOfType(type, clone, "Clone correct type");
            object value = type.GetProperty("BaseProp").GetValue(clone, null);
            Assert.AreEqual(EXPECTED, value, "Clone value");
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
