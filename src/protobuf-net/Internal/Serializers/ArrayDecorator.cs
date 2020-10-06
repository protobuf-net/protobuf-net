//using ProtoBuf.Compiler;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Reflection;

//namespace ProtoBuf.Internal.Serializers
//{
//    internal static class ArrayDecorator
//    {
//        public static IRuntimeProtoSerializerNode Create(Type type, int fieldNumber, SerializerFeatures features)
//        {
//            return (IRuntimeProtoSerializerNode)Activator.CreateInstance(typeof(ArrayDecorator<>).MakeGenericType(type),
//                new object[] { fieldNumber, features });
//        }

//        // look for: public T[] ReadRepeated<T>(SerializerFeatures features, T[] values, ISerializer<T> serializer = null)
//        internal static readonly MethodInfo s_ReadRepeated = (
//            from method in typeof(ProtoReader.State).GetMethods(BindingFlags.Instance | BindingFlags.Public)
//            where method.Name == nameof(ProtoReader.State.ReadRepeated) && method.IsGenericMethodDefinition
//            let targs = method.GetGenericArguments()
//            where targs.Length == 1
//            let args = method.GetParameters()
//            let arrType = targs[0].MakeArrayType()
//            where args.Length == 3
//            && method.ReturnType == arrType
//            && args[0].ParameterType == typeof(SerializerFeatures)
//            && args[1].ParameterType == arrType
//            && args[2].ParameterType == typeof(ISerializer<>).MakeGenericType(targs)
//            select method).SingleOrDefault();
//    }
//    internal sealed class ArrayDecorator<T> : IRuntimeProtoSerializerNode, ICompiledSerializer
//    {
//        static readonly MethodInfo s_ReadRepeated = ArrayDecorator.s_ReadRepeated.MakeGenericMethod(typeof(T));
//        static readonly MethodInfo s_WriteRepeated = ListDecorator.s_WriteRepeated.MakeGenericMethod(typeof(T));

//        private readonly int _fieldNumber;
//        private readonly SerializerFeatures _features;

//        public ArrayDecorator(int fieldNumber, SerializerFeatures features)
//        {
//            Debug.Assert(typeof(T) != typeof(byte), "Should have used BlobSerializer");
//            _fieldNumber = fieldNumber;
//            _features = features;
//        }

//        public Type ExpectedType => typeof(T[]);
//        public bool RequiresOldValue => (_features & SerializerFeatures.OptionClearCollection) == 0;
//        public bool ReturnsValue { get { return true; } }

//        public void Write(ref ProtoWriter.State state, object value)
//            => state.WriteRepeated(_fieldNumber, _features, (T[])value);

//        public object Read(ref ProtoReader.State state, object value)
//            => state.ReadRepeated(_features, RequiresOldValue ? (T[])value : null);

//        public void EmitRead(CompilerContext ctx, Local valueFrom)
//        {
//            using var loc = RequiresOldValue ? ctx.GetLocalWithValue(typeof(T[]), valueFrom) : default;
//            ctx.LoadState();
//            ctx.LoadValue((int)_features);
//            if (loc is null)
//                ctx.LoadNullRef();
//            else
//                ctx.LoadValue(loc);
//            ctx.LoadSelfAsService<ISerializer<T>, T>();
//            ctx.EmitCall(s_ReadRepeated);
//        }

//        public void EmitWrite(CompilerContext ctx, Local valueFrom)
//        {
//            using var loc = ctx.GetLocalWithValue(typeof(T[]), valueFrom);
//            ctx.LoadState();
//            ctx.LoadValue(_fieldNumber);
//            ctx.LoadValue((int)_features);
//            ctx.LoadValue(loc);
//            ctx.LoadSelfAsService<ISerializer<T>, T>();
//            ctx.EmitCall(s_WriteRepeated);
//        }
//    }
//}