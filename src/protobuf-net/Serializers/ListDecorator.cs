using ProtoBuf.Compiler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace ProtoBuf.Serializers
{
    internal static class ListDecorator
    {
        public static IRuntimeProtoSerializerNode Create(Type constructType, Type type, int fieldNumber, SerializerFeatures features)
        {
            return (IRuntimeProtoSerializerNode)Activator.CreateInstance(typeof(ListDecorator<,>).MakeGenericType(constructType, type),
                new object[] { fieldNumber, features });
        }

        // look for: public ICollection<T> ReadRepeated<T>(SerializerFeatures features, ICollection<T> values, ISerializer<T> serializer = null)
        internal static readonly MethodInfo s_ReadRepeated = (
            from method in typeof(ProtoReader.State).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            where method.Name == nameof(ProtoReader.State.ReadRepeated) && method.IsGenericMethodDefinition
            let targs = method.GetGenericArguments()
            where targs.Length == 1
            let args = method.GetParameters()
            where args.Length == 3
            && args[0].ParameterType == typeof(SerializerFeatures)
            && args[1].ParameterType == typeof(ICollection<>).MakeGenericType(targs)
            && args[2].ParameterType == typeof(ISerializer<>).MakeGenericType(targs)
            && method.ReturnType == args[1].ParameterType
            select method).SingleOrDefault();

        // look for: public void WriteRepeated<T>(int fieldNumber, SerializerFeatures features, ICollection<T> values, ISerializer<T> serializer
        internal static readonly MethodInfo s_WriteRepeated = (
            from method in typeof(ProtoWriter.State).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            where method.Name == nameof(ProtoWriter.State.WriteRepeated) && method.IsGenericMethodDefinition
            && method.ReturnType == typeof(void)
            let targs = method.GetGenericArguments()
            where targs.Length == 1
            let args = method.GetParameters()
            where args.Length == 4
            && args[0].ParameterType == typeof(int)
            && args[1].ParameterType == typeof(SerializerFeatures)
            && args[2].ParameterType == typeof(ICollection<>).MakeGenericType(targs)
            && args[3].ParameterType == typeof(ISerializer<>).MakeGenericType(targs)
            select method).SingleOrDefault();
    }
    internal sealed class ListDecorator<TConstruct, T> : IRuntimeProtoSerializerNode
        where TConstruct : class, ICollection<T>
    {
        static readonly MethodInfo s_ReadRepeated = ListDecorator.s_ReadRepeated.MakeGenericMethod(typeof(T));
        static readonly MethodInfo s_WriteRepeated = ListDecorator.s_WriteRepeated.MakeGenericMethod(typeof(T));

        private readonly int _fieldNumber;
        private readonly SerializerFeatures _features;

        public ListDecorator(int fieldNumber, SerializerFeatures features)
        {
            _fieldNumber = fieldNumber;
            _features = features;
        }

        public Type ExpectedType => typeof(ICollection<T>);
        public bool RequiresOldValue => true;
        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        public object Read(ref ProtoReader.State state, object value)
        {
            var typed = (ICollection<T>)value;
            typed ??= state.CreateInstance<TConstruct>();
            return state.ReadRepeated(_features, typed);
        }

        public void EmitRead(CompilerContext ctx, Local valueFrom)
        {
            using var loc = ctx.GetLocalWithValue(typeof(ICollection<T>), valueFrom);

            var notNull = ctx.DefineLabel();
            ctx.LoadValue(loc);
            ctx.BranchIfTrue(notNull, true);
            ctx.CreateInstance<TConstruct>();
            ctx.StoreValue(loc);
            ctx.MarkLabel(notNull);

            ctx.LoadState();
            ctx.LoadValue((int)_features);
            ctx.LoadValue(loc);
            ctx.LoadSelfAsService<ISerializer<T>, T>();
            ctx.EmitCall(s_ReadRepeated);
        }

        public void Write(ref ProtoWriter.State state, object value)
            => state.WriteRepeated(_fieldNumber, _features, (ICollection<T>)value);

        public void EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            using var loc = ctx.GetLocalWithValue(typeof(ICollection<T>), valueFrom);
            ctx.LoadState();
            ctx.LoadValue(_fieldNumber);
            ctx.LoadValue((int)_features);
            ctx.LoadValue(loc);
            ctx.LoadSelfAsService<ISerializer<T>, T>();
            ctx.EmitCall(s_WriteRepeated);
        }
    }
}