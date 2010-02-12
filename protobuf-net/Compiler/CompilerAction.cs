
namespace ProtoBuf.Compiler
{
    internal interface IBranchAction
    {
        void If(CompilerContext ctx);
        void Else(CompilerContext ctx);
    }
}
