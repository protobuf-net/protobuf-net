//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.IO;
//using ProtoBuf.Serializers;
//using System.Reflection;
//using System.Reflection.Emit;
//using ProtoBuf.Compiler;
//using ProtoBuf.Meta;

//namespace ProtoBuf
//{
//    public abstract class ProtoSerializer
//    {
//        protected ProtoSerializer(TypeModel model)
//        {
//            this.model = model;
//        }
//        private readonly TypeModel model;
//        public int Serialize(Stream dest, object obj)
//        {
//            using (ProtoWriter writer = new ProtoWriter(dest, model))
//            {
//                return Serialize(obj, writer);
//            }
//        }

//        protected abstract int Serialize(object obj, ProtoWriter dest);


//        /*internal static IProtoSerializer<T> Compile<T>(string name, IProtoSerializer head)
//        {
//            return (IProtoSerializer<T>)Compile(name, head);
//        }*/
//        internal static ProtoSerializer Compile(string name, IProtoSerializer head)
//        {
//            Type expectedType = head.ExpectedType;
//            AssemblyName an = new AssemblyName(name);
//            AssemblyBuilder asm = AppDomain.CurrentDomain.DefineDynamicAssembly(
//                an, AssemblyBuilderAccess.RunAndSave);
//            ModuleBuilder mod = asm.DefineDynamicModule(name);

//            //Type genericInterface = typeof(IProtoSerializer<>).MakeGenericType(expectedType);
//            Type[] interfaces = new Type[] {
//                //genericInterface
//            };


//            Type baseClass = typeof(ProtoSerializer);
//            TypeBuilder tb = mod.DefineType(name,
//                TypeAttributes.Class | TypeAttributes.NotPublic | TypeAttributes.Sealed,
//                baseClass, interfaces);
//            tb.DefineDefaultConstructor(MethodAttributes.Public);
//            Type[] paramTypes = new Type[] { typeof(object), typeof(ProtoWriter) };
//            MethodInfo baseMethod = baseClass.GetMethod("Serialize",
//                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null,
//                paramTypes, null);


//            MethodBuilder typedMethodWriter = tb.DefineMethod(baseMethod.Name + "Typed",
//                MethodAttributes.Private, typeof(int), new Type[] { expectedType, typeof(ProtoWriter) });
//            ILGenerator il = typedMethodWriter.GetILGenerator();
//            CompilerContext ctx = new CompilerContext(il, true, false);
//            ctx.LoadInputValue();
//            ctx.NullCheckedTail(expectedType, head);
//            ctx.Return();

//            /*
//            MethodInfo interfaceWrite = genericInterface.GetMethod("Serialize");
//            MethodBuilder typedMethodStream = tb.DefineMethod(baseMethod.Name + "Typed",
//                MethodAttributes.Private | MethodAttributes.Virtual, typeof(int), new Type[] { typeof(Stream), expectedType });
//            il = typedMethodStream.GetILGenerator();
//            ctx = new CompilerContext(il, true, false);
//            ctx.BuildWriterWrapper(typedMethodWriter);
//            tb.DefineMethodOverride(typedMethodStream, interfaceWrite);
//            */

//            MethodBuilder orMethod = tb.DefineMethod(baseMethod.Name, baseMethod.Attributes & ~MethodAttributes.Abstract,
//                baseMethod.CallingConvention, typeof(int), paramTypes);
//            il = orMethod.GetILGenerator();
//            ctx = new CompilerContext(il, true, false);
//            ctx.EmitInstance();
//            ctx.LoadInputValue();
//            ctx.CastFromObject(expectedType);
//            ctx.LoadDest();
//            ctx.EmitCall(typedMethodWriter);
//            il.Emit(OpCodes.Ret);

//            tb.DefineMethodOverride(orMethod, baseMethod);
//            return (ProtoSerializer)Activator.CreateInstance(tb.CreateType());

//        }
//    }
//}
