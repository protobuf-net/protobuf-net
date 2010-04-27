#if !NO_RUNTIME
using System;



namespace ProtoBuf.Serializers
{
    sealed class UriDecorator : ProtoDecoratorBase
    {
        public override bool RequiresOldValue { get { return false; } }
        public override bool ReturnsValue { get { return true; } }
        public override Type ExpectedType { get { return typeof(Uri); } }
        public UriDecorator(IProtoSerializer tail) : base(tail) { }

        public override void Write(object value, ProtoWriter dest)
        {
            Tail.Write(((Uri)value).AbsoluteUri, dest);
        }
        public override object Read(object value, ProtoReader source)
        {
            Helpers.DebugAssert(value == null); // not expecting incoming
            string s = (string)Tail.Read(null, source);
            return s.Length == 0 ? null : new Uri(s);
        }
#if FEAT_COMPILER
        protected override void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadValue(valueFrom);
            ctx.LoadValue(typeof(Uri).GetProperty("AbsoluteUri"));
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
            ctx.EmitCtor(typeof(Uri), typeof(string));
            ctx.MarkLabel(@end);
            
        }
#endif 
    }
}
#endif
