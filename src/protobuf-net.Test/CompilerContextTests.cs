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

            AssemblyName an = new AssemblyName { Name = "ManualCompiler" };
            AssemblyBuilder asm = AppDomain.CurrentDomain.DefineDynamicAssembly(an,
                AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder module = asm.DefineDynamicModule("ManualCompiler", "ManualCompiler.dll");
            var baseType = typeof(TypeModel);
            var type = module.DefineType("MyModel", baseType.Attributes & ~TypeAttributes.Abstract, baseType);

            var baseMethod = baseType.GetMethod("GetKeyImpl",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var args = baseMethod.GetParameters();

            var method = type.DefineMethod(baseMethod.Name, (baseMethod.Attributes & ~MethodAttributes.Abstract),
                baseMethod.ReturnType, Array.ConvertAll(args, a => a.ParameterType));
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldc_I4, 42);
            il.Emit(OpCodes.Ret);
            type.DefineMethodOverride(method, baseMethod);

            il = RuntimeTypeModel.Override(type, "Serialize");
            il.ThrowException(typeof(NotImplementedException));

            il = RuntimeTypeModel.Override(type, "DeserializeCore");
            il.ThrowException(typeof(NotImplementedException));

            var t = type.CreateType();
            asm.Save("ManualCompiler.dll");

            TypeModel tm = (TypeModel)Activator.CreateInstance(t);
            Type kt = typeof(string);
            bool known = tm.IsKnownType(ref kt);
            Assert.True(known);
        }
#endif
    }

    public sealed class MyModel : TypeModel
    {
        protected internal override void Serialize(ref ProtoWriter.State state, Type type, object value)
        { }
        protected internal override object Deserialize(ref ProtoReader.State state, Type type, object value) => null;
    }
}
