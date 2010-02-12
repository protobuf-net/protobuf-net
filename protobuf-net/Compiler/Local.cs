#if FEAT_COMPILER
using System;
using System.Reflection.Emit;

namespace ProtoBuf.Compiler
{
    internal sealed class Local : IDisposable
    {
        public static readonly Local InputValue = new Local(null, null);
        LocalBuilder value;
        public Local AsCopy()
        {
            if (ctx == null) return this; // can re-use if context-free
            return new Local(value);
        }
        internal LocalBuilder Value
        {
            get
            {
                if (value == null) throw new ObjectDisposedException(GetType().Name);
                return value;
            }
        }
        CompilerContext ctx;
        public void Dispose()
        {
            if (ctx != null) { ctx.ReleaseToPool(value); }
            value = null;
            ctx = null;
        }
        private Local(LocalBuilder value)
        {
            this.value = value;
        }
        internal Local(Compiler.CompilerContext ctx, Type type)
        {
            this.ctx = ctx;
            if (ctx != null) { value = ctx.GetFromPool(type); }
        }
    }


}
#endif