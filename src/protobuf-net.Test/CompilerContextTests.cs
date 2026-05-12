using ProtoBuf.Meta;
using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace ProtoBuf
{
    public class CompilerContextTests
    {
#if NETFRAMEWORK || NET9_0_OR_GREATER
        [Fact]
        public void ManualCompiler()
        {
            var nilModel = TypeModel.NullModel.Singleton;
            Assert.False(nilModel.IsDefined(typeof(string)));
            Assert.True(nilModel.CanSerialize(typeof(string)));
            Assert.True(nilModel.CanSerializeBasicType(typeof(string)));
            Assert.False(nilModel.CanSerializeContractType(typeof(string)));

            AssemblyName an = new AssemblyName { Name = "ManualCompiler" };
#if NET9_0_OR_GREATER
            PersistedAssemblyBuilder asm = new PersistedAssemblyBuilder(an, typeof(object).Assembly);
            ModuleBuilder module = asm.DefineDynamicModule("ManualCompiler");
#else
            AssemblyBuilder asm = AppDomain.CurrentDomain.DefineDynamicAssembly(an,
                AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder module = asm.DefineDynamicModule("ManualCompiler", "ManualCompiler.dll");
#endif
            
            var baseType = typeof(TypeModel);
            var type = module.DefineType("MyModel", baseType.Attributes & ~TypeAttributes.Abstract, baseType);
            
            // ReSharper disable once RedundantAssignment # is needed on netfx
            var t = type.CreateType();

#if NET9_0_OR_GREATER
            var ms = new MemoryStream();
            asm.Save(ms);
            ms.Position = 0;
            t = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromStream(ms).GetType("MyModel")
                 ?? throw new InvalidOperationException("Type not found");
            // could also copy to a file, but Save can only be called once
#else
            asm.Save("ManualCompiler.dll");
#endif
            TypeModel tm = (TypeModel)Activator.CreateInstance(t)!;
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
