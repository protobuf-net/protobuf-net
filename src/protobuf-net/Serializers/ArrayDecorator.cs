using ProtoBuf.Compiler;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace ProtoBuf.Serializers
{
    internal static class ArrayDecorator
    {
        internal static readonly MethodInfo s_ReadRepeated = (
            from method in typeof(ProtoReader.State).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            where method.Name == nameof(ProtoReader.State.ReadRepeated) && method.IsGenericMethodDefinition
            let targs = method.GetGenericArguments()
            where targs.Length == 1
            let args = method.GetParameters()
            where args.Length == 3
            && args[0].ParameterType == typeof(SerializerFeatures)
            && args[1].ParameterType == targs[0].MakeArrayType()
            && args[2].ParameterType == typeof(ISerializer<>).MakeGenericType(targs)
            select method).Single();

        internal static readonly MethodInfo s_WriteRepeated = (
            from method in typeof(ProtoWriter.State).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            where method.Name == nameof(ProtoWriter.State.WriteRepeated) && method.IsGenericMethodDefinition
            let targs = method.GetGenericArguments()
            where targs.Length == 1
            let args = method.GetParameters()
            where args.Length == 4
            && args[0].ParameterType == typeof(int)
            && args[1].ParameterType == typeof(SerializerFeatures)
            && args[2].ParameterType == targs[0].MakeArrayType()
            && args[3].ParameterType == typeof(ISerializer<>).MakeGenericType(targs)
            select method).Single();

        public static ProtoDecoratorBase Create(Type type, IRuntimeProtoSerializerNode tail, int fieldNumber, SerializerFeatures features)
        {
            return (ProtoDecoratorBase)Activator.CreateInstance(typeof(ArrayDecorator<>).MakeGenericType(type),
                new object[] { tail, fieldNumber, features });
        }
    }
    internal sealed class ArrayDecorator<T> : ProtoDecoratorBase
    {
        private readonly int _fieldNumber;
        private readonly SerializerFeatures _features;

        public ArrayDecorator(IRuntimeProtoSerializerNode tail, int fieldNumber, SerializerFeatures features)
            : base(tail)
        {
#if FEAT_NULL_LIST_ITEMS
            Type underlyingItemType = supportNull ? itemType : (Nullable.GetUnderlyingType(itemType) ?? itemType);
            Debug.Assert(underlyingItemType == Tail.ExpectedType
                || (Tail.ExpectedType == typeof(object) && !underlyingItemType.IsValueType), "invalid tail");
#else
            Debug.Assert(typeof(T) == Tail.ExpectedType);
#endif

            Debug.Assert(Tail.ExpectedType != typeof(byte), "Should have used BlobSerializer");
            
            _fieldNumber = fieldNumber;
            _features = features;
        }

        public override Type ExpectedType => typeof(T[]);
        public override bool RequiresOldValue => (_features & SerializerFeatures.OptionOverwriteList) == 0;
        public override bool ReturnsValue { get { return true; } }

        public override void Write(ref ProtoWriter.State state, object value)
            => state.WriteRepeated(_fieldNumber, _features, (T[])value);

        public override object Read(ref ProtoReader.State state, object value)
            => state.ReadRepeated(_features, (T[])value);

        static readonly MethodInfo s_ReadRepeated = ArrayDecorator.s_ReadRepeated.MakeGenericMethod(typeof(T));
        static readonly MethodInfo s_WriteRepeated = ArrayDecorator.s_WriteRepeated.MakeGenericMethod(typeof(T));
        protected override void EmitRead(CompilerContext ctx, Local valueFrom)
        {
            using var loc = ctx.GetLocalWithValue(typeof(T[]), valueFrom);
            ctx.LoadState();
            ctx.LoadValue((int)_features);
            ctx.LoadValue(loc);
            ctx.LoadNullRef();
            ctx.EmitCall(s_ReadRepeated);
        }

        protected override void EmitWrite(ProtoBuf.Compiler.CompilerContext ctx, ProtoBuf.Compiler.Local valueFrom)
        {
            using var loc = ctx.GetLocalWithValue(typeof(T[]), valueFrom);
            ctx.LoadState();
            ctx.LoadValue(_fieldNumber);
            ctx.LoadValue((int)_features);
            ctx.LoadValue(loc);
            ctx.LoadNullRef();
            ctx.EmitCall(s_WriteRepeated);
        }
    }
}