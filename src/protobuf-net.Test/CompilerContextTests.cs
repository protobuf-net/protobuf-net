using ProtoBuf.Meta;
using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace ProtoBuf
{
    public class CompilerContextTests
    {
#if NET462
        [Fact]
        public void ManualCompiler()
        {
            var nilModel = TypeModel.NullModel.Singleton;
            Assert.False(nilModel.IsDefined(typeof(string)));
            Assert.True(nilModel.CanSerialize(typeof(string)));
            Assert.True(nilModel.CanSerializeBasicType(typeof(string)));
            Assert.False(nilModel.CanSerializeContractType(typeof(string)));

            AssemblyName an = new AssemblyName { Name = "ManualCompiler" };
            AssemblyBuilder asm = AppDomain.CurrentDomain.DefineDynamicAssembly(an,
                AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder module = asm.DefineDynamicModule("ManualCompiler", "ManualCompiler.dll");
            var baseType = typeof(TypeModel);
            var type = module.DefineType("MyModel", baseType.Attributes & ~TypeAttributes.Abstract, baseType);

            var t = type.CreateType();
            asm.Save("ManualCompiler.dll");

            TypeModel tm = (TypeModel)Activator.CreateInstance(t);
            Assert.False(tm.IsDefined(typeof(string)));
            Assert.True(tm.CanSerialize(typeof(string)));
            Assert.True(tm.CanSerializeBasicType(typeof(string)));
            Assert.False(tm.CanSerializeContractType(typeof(string)));
        }
#endif
    }

    public sealed class MyModel : TypeModel
    {
    }
}
