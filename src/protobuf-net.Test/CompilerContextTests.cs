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
            Type kt = typeof(string);
            var nilModel = TypeModel.NullModel.Instance;
            Assert.False(nilModel.IsKnownType(ref kt));
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
            kt = typeof(string);
            Assert.False(tm.IsKnownType(ref kt));
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
