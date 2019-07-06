
using System.Reflection;
using ProtoBuf.Meta;
#if !NO_RUNTIME
using System;

namespace ProtoBuf.Serializers
{
    /// <summary>
    /// This is not really a serializer; rather, it is a tuple of two serialisers: 1. A <see cref="ScalarValuePassthruDecorator"/>,
    /// 2. A primitive serializer.
    /// They are wrapped up in this type because it is what <see cref="ValueMember.TryGetCoreSerializer"/> returns.
    /// </summary>
    sealed class ScalarValuePassthruFakeDecorator : ProtoDecoratorBase
    {
        private readonly MethodInfo _toSurrogate;
        private readonly MethodInfo _fromSurrogate;
        private readonly Type _strongType;
        private readonly FieldInfo _singleField;

        public ScalarValuePassthruFakeDecorator(MethodInfo toSurrogate, MethodInfo fromSurrogate, Type strongType, FieldInfo singleField, IProtoSerializer primitiveTypeTail) :
            base(primitiveTypeTail)
        {
            _toSurrogate = toSurrogate;
            _fromSurrogate = fromSurrogate;
            _strongType = strongType;
            _singleField = singleField;
        }

        public ProtoDecoratorBase GetStronglyTypedDecorator(IProtoSerializer stronglyTypedTail)
        {
            return new ScalarValuePassthruDecorator(_toSurrogate, _fromSurrogate, _strongType, _singleField, stronglyTypedTail);
        }

        public IProtoSerializer PrimitiveTypeSerializer => Tail;

        public override Type ExpectedType => throw new NotSupportedException();
        public override void Write(ProtoWriter dest, ref ProtoWriter.State state, object value) => throw new NotSupportedException();

        public override object Read(ProtoReader source, ref ProtoReader.State state, object value) => throw new NotSupportedException();

#if FEAT_COMPILER
        protected override void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom) => throw new NotSupportedException();

        protected override void EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom) => throw new NotSupportedException();
#endif

        public override bool ReturnsValue => throw new NotSupportedException();
        public override bool RequiresOldValue => throw new NotSupportedException();
    }

    /// <remarks>
    /// Converts back and forth between a strong type and the primitive type that it contains.
    /// On the way the value might be represented as a surrogate type.
    /// </remarks>
    /// <seealso cref="ProtoBuf.Meta.MetaType.ScalarValuePassthru"/>.
    sealed class ScalarValuePassthruDecorator : ProtoDecoratorBase
    {
        public override Type ExpectedType { get; }

        private readonly FieldInfo _singleField;
        private readonly MethodInfo _toSurrogate;
        private readonly MethodInfo _fromSurrogate;
        public override bool RequiresOldValue => false;
        public override bool ReturnsValue => true;

        public ScalarValuePassthruDecorator(MethodInfo toSurrogate, MethodInfo fromSurrogate, Type forType,
            FieldInfo singleField,
            IProtoSerializer tail) : base(tail)
        {
            Helpers.DebugAssert(forType != null);
            Helpers.DebugAssert(singleField != null);
            Helpers.DebugAssert(tail.ReturnsValue);
            Helpers.DebugAssert(!tail.RequiresOldValue);
            Helpers.DebugAssert(Helpers.IsValueType(forType));
            Helpers.DebugAssert(toSurrogate == null || Helpers.IsValueType(toSurrogate.ReturnType));
            Helpers.DebugAssert(fromSurrogate == null || fromSurrogate.ReturnType == forType);
            _toSurrogate = toSurrogate;
            _fromSurrogate = fromSurrogate;
            ExpectedType = forType;
            _singleField = singleField;
        }

        public override void Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
        {
            Helpers.DebugAssert(value != null);
            if (_toSurrogate != null)
            {
                value = _toSurrogate.Invoke(null, new object[] { value });
            }

            var innerValue = _singleField.GetValue(value);
            Tail.Write(dest, ref state, innerValue);
        }

        public override object Read(ProtoReader source, ref ProtoReader.State state, object value)
        {
            Helpers.DebugAssert(value == null);
            object newValue = Tail.Read(source, ref state, null);
            if (newValue != null)
            {
                if (_toSurrogate != null)
                {
                    var newInstance = Activator.CreateInstance(_toSurrogate.ReturnType);
                    _singleField.SetValue(newInstance, newValue);

                    return _fromSurrogate.Invoke(null, new object[] { newInstance });
                }
                else
                {
                    var newInstance = Activator.CreateInstance(ExpectedType);
                    _singleField.SetValue(newInstance, newValue);
                    return newInstance;
                }
            }

            return null;
        }


#if FEAT_COMPILER
        protected override void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadValue(valueFrom);
            if (_toSurrogate != null)
            {
                ctx.EmitCall(_toSurrogate);
            }
            ctx.LoadValue(_singleField);
            ctx.WriteNullCheckedTail(_singleField.FieldType, Tail, null);
        }

        protected override void EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            Helpers.DebugAssert(valueFrom == null);

            if (_toSurrogate != null)
            {
                using (Compiler.Local loc = new Compiler.Local(ctx, _toSurrogate.ReturnType))
                {
                    ctx.LoadAddress(loc, _toSurrogate.ReturnType);
                    Tail.EmitRead(ctx, null);
                    ctx.StoreValue(_singleField);
                    ctx.LoadValue(loc);

                    ctx.EmitCall(_fromSurrogate);
                }
            }
            else
            {
                using (Compiler.Local loc = new Compiler.Local(ctx, ExpectedType))
                {
                    ctx.LoadAddress(loc, ExpectedType);
                    Tail.EmitRead(ctx, null);
                    ctx.StoreValue(_singleField);
                    ctx.LoadValue(loc);
                }
            }
        }
#endif
    }
}
#endif