using System;
using System.Reflection;

namespace ProtoBuf.Internal.Serializers
{
    internal sealed class MemberSpecifiedDecorator : ProtoDecoratorBase
    {
        public override Type ExpectedType => Tail.ExpectedType;

        public override bool RequiresOldValue => Tail.RequiresOldValue;

        public override bool ReturnsValue => Tail.ReturnsValue;

        private readonly MethodInfo getSpecified, setSpecified;
        public MemberSpecifiedDecorator(MethodInfo getSpecified, MethodInfo setSpecified, IRuntimeProtoSerializerNode tail)
            : base(tail)
        {
            if (getSpecified is null && setSpecified is null) throw new InvalidOperationException();
            this.getSpecified = getSpecified;
            this.setSpecified = setSpecified;
        }

        public override void Write(ref ProtoWriter.State state, object value)
        {
            if (getSpecified is null || (bool)getSpecified.Invoke(value, null))
            {
                Tail.Write(ref state, value);
            }
        }

        public override object Read(ref ProtoReader.State state, object value)
        {
            object result = Tail.Read(ref state, value);
            if (setSpecified is not null) setSpecified.Invoke(value, new object[] { true });
            return result;
        }

        protected override void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            if (getSpecified is null)
            {
                Tail.EmitWrite(ctx, valueFrom);
                return;
            }
            using Compiler.Local loc = ctx.GetLocalWithValue(ExpectedType, valueFrom);
            ctx.LoadAddress(loc, ExpectedType);
            ctx.EmitCall(getSpecified);
            Compiler.CodeLabel done = ctx.DefineLabel();
            ctx.BranchIfFalse(done, false);
            Tail.EmitWrite(ctx, loc);
            ctx.MarkLabel(done);
        }
        protected override void EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            if (setSpecified is null)
            {
                Tail.EmitRead(ctx, valueFrom);
                return;
            }
            using Compiler.Local loc = ctx.GetLocalWithValue(ExpectedType, valueFrom);
            Tail.EmitRead(ctx, loc);
            ctx.LoadAddress(loc, ExpectedType);
            ctx.LoadValue(1); // true
            ctx.EmitCall(setSpecified);
        }
    }
}