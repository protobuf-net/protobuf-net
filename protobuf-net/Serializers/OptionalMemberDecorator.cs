#if !NO_RUNTIME
using System;
using ProtoBuf.Meta;

#if FEAT_IKVM
using Type = IKVM.Reflection.Type;
using IKVM.Reflection;
#else
using System.Reflection;
#endif



namespace ProtoBuf.Serializers
{
    /// <summary>
    /// Decorator for optional members of a type implementing IOptionalMemberCallbacks.
    /// Handles calling the ShouldSerialize and WasDeserialized callbacks.
    /// </summary>
    sealed class OptionalMemberDecorator : ProtoDecoratorBase
    {
        private readonly MethodInfo _shouldSerialize;
        private readonly MethodInfo _wasDeserialized;

        private readonly int _memberId;
        private readonly object[] _args;

        public override Type ExpectedType
        {
            get
            {
                return Tail.ExpectedType;
            }
        }

        public override bool RequiresOldValue
        {
            get
            {
                return Tail.RequiresOldValue;
            }
        }

        public override bool ReturnsValue
        {
            get
            {
                return Tail.ReturnsValue;
            }
        }

        public OptionalMemberDecorator(int memberId, MethodInfo shouldSerialize, MethodInfo wasDeserialized, IProtoSerializer tail)
            : base(tail)
        {
            if (shouldSerialize == null || wasDeserialized == null) 
                throw new InvalidOperationException();

            _shouldSerialize = shouldSerialize;
            _wasDeserialized = wasDeserialized;
            
            _memberId = memberId;
            _args = new object[] {memberId};
        }
#if !FEAT_IKVM

        public override void Write(object value, ProtoWriter dest)
        {
            // JCF: Gross, allocating an array every call to this method.
            //      But maybe this method is just here for debug builds and release builds should really be using
            //      EmitWrite... correct?
            if ((bool)_shouldSerialize.Invoke(value, _args) == false)
                return;

            Tail.Write(value, dest);
        }

        public override object Read(object value, ProtoReader source)
        {            
            object result = Tail.Read(value, source);
            
            _wasDeserialized.Invoke(value, _args);

            return result;
        }
#endif

#if FEAT_COMPILER
        protected override void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            using (Compiler.Local loc = ctx.GetLocalWithValue(ExpectedType, valueFrom))
            {
                ctx.LoadAddress(loc, ExpectedType);
                ctx.LoadValue(_memberId);
                ctx.EmitCall(_shouldSerialize);
                Compiler.CodeLabel done = ctx.DefineLabel();
                ctx.BranchIfFalse(done, false);
                Tail.EmitWrite(ctx, loc);
                ctx.MarkLabel(done);
            }

        }
        protected override void EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {            
            using (Compiler.Local loc = ctx.GetLocalWithValue(ExpectedType, valueFrom))
            {
                Tail.EmitRead(ctx, loc);
                ctx.LoadAddress(loc, ExpectedType);
                ctx.LoadValue(_memberId);
                ctx.EmitCall(_wasDeserialized);
            }
        }
#endif
    }
}
#endif