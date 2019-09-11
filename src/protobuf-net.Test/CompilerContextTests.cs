using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
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

            //var il = RuntimeTypeModel.Override(type, "GetKeyImpl");
            //il.ThrowException(typeof(NotImplementedException));

            il = RuntimeTypeModel.Override(type, "Serialize");
            il.ThrowException(typeof(NotImplementedException));

            il = RuntimeTypeModel.Override(type, "DeserializeCore");
            il.ThrowException(typeof(NotImplementedException));

            var t = type.CreateType();
            asm.Save("ManualCompiler.dll");

            TypeModel tm = (TypeModel)Activator.CreateInstance(t);
            Type kt = typeof(string);
            int key = tm.GetKey(ref kt);
            Assert.Equal(42, key);
            Console.WriteLine(key);
        }
#endif
    }

    public sealed class MyModel : TypeModel
    {
        protected override int GetKeyImpl(Type type) => 42;
        protected internal override void Serialize(ProtoWriter dest, ref ProtoWriter.State state, int key, object value)
        { }
        protected internal override object DeserializeCore(ProtoReader source, ref ProtoReader.State state, int key, object value) => null;
    }
}
