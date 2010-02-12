using System;
using System.Reflection.Emit;

namespace ProtoBuf.Compiler
{
    internal sealed class Local : IDisposable
    {
        LocalBuilder value;
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
        internal Local(CompilerContext ctx, Type type)
        {
            this.ctx = ctx;
            value = ctx.GetFromPool(type);
        }
    }


}
