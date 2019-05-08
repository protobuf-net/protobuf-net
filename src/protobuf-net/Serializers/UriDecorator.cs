#if !NO_RUNTIME
using System;
using System.Reflection;

#if FEAT_COMPILER
using ProtoBuf.Compiler;
#endif

namespace ProtoBuf.Serializers
{
    internal sealed class UriDecorator : ProtoDecoratorBase
    {
        private static readonly Type expectedType = typeof(Uri);
        public UriDecorator(IProtoSerializer tail) : base(tail) { }

        public override Type ExpectedType => expectedType;

        public override bool RequiresOldValue => false;

        public override bool ReturnsValue => true;

        public override void Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
        {
            Tail.Write(dest, ref state, ((Uri)value).OriginalString);
        }

        public override object Read(ProtoReader source, ref ProtoReader.State state, object value)
        {
            Helpers.DebugAssert(value == null); // not expecting incoming
            string s = (string)Tail.Read(source, ref state, null);
            return s.Length == 0 ? null : new Uri(s, UriKind.RelativeOrAbsolute);
        }

#if FEAT_COMPILER
        protected override void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadValue(valueFrom);
            ctx.LoadValue(typeof(Uri).GetProperty("OriginalString"));
            Tail.EmitWrite(ctx, null);
        }
        protected override void EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            Tail.EmitRead(ctx, valueFrom);
            ctx.CopyValue();
            Compiler.CodeLabel @nonEmpty = ctx.DefineLabel(), @end = ctx.DefineLabel();
            ctx.LoadValue(typeof(string).GetProperty("Length"));
            ctx.BranchIfTrue(@nonEmpty, true);
            ctx.DiscardValue();
            ctx.LoadNullRef();
            ctx.Branch(@end, true);
            ctx.MarkLabel(@nonEmpty);
            ctx.LoadValue((int)UriKind.RelativeOrAbsolute);
            ctx.EmitCtor(typeof(Uri), typeof(string), typeof(UriKind));
            ctx.MarkLabel(@end);
        }
#endif
    }
}
#endif