#nullable enable
namespace ProtoBuf.Reflection.Internal.CodeGen.Error;

internal sealed class CodeGenError
{
    public CodeGenErrorLevel Level { get; set; }
    
    public string SymbolType { get; set; }
    
    public string Location { get; set; }
    
    public string Description { get; set; }
}
