using System.Linq;
using Microsoft.CodeAnalysis;
using ProtoBuf.Internal.CodeGen.Abstractions;
using ProtoBuf.Internal.CodeGen.Providers;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGen.Parsers;

internal sealed class ServiceMethodCodeGenModelParser : SymbolCodeGenModelParserBase<IMethodSymbol, CodeGenServiceMethod>
{
    public ServiceMethodCodeGenModelParser(SymbolCodeGenModelParserProvider parserProvider) : base(parserProvider)
    {
    }
    
    public override CodeGenServiceMethod Parse(IMethodSymbol symbol)
    {
        var responseType = ParseResponseType(symbol);
        
        return new CodeGenServiceMethod(symbol.Name)
        {
            ResponseType = responseType
        };
    }

    private CodeGenServiceMethod.Type ParseResponseType(IMethodSymbol symbol)
    {
        var namedTypeSymbol = symbol.ReturnType as INamedTypeSymbol;
        
        if (namedTypeSymbol.Arity == 0)
        {
            // simply it is a raw type
            return new CodeGenServiceMethod.Type
            {
                RawType = ParseContext.GetContractType(symbol.ReturnType.ToString()),
                Representation = CodeGenTypeRepresentation.Raw
            }; 
        }
        
        var genericTypeDefinition = namedTypeSymbol.OriginalDefinition;
        var typeArgumentTypeDefinition = namedTypeSymbol.TypeArguments.FirstOrDefault();

        return new CodeGenServiceMethod.Type
        {
            RawType = ParseContext.GetContractType(typeArgumentTypeDefinition.ToString()),
            Representation = DetermineGenericTypeRepresentation(genericTypeDefinition)
        };
    }

    private CodeGenTypeRepresentation DetermineGenericTypeRepresentation(INamedTypeSymbol symbol) => symbol.ToString() switch
    {
        "System.Collections.Generic.IAsyncEnumerable<T>" => CodeGenTypeRepresentation.AsyncEnumerable,
        "System.Threading.Tasks.Task<T>" or "System.Threading.Tasks.Task<TResult>" => CodeGenTypeRepresentation.Task,
        "System.Threading.Tasks.ValueTask<T>" or "System.Threading.Tasks.ValueTask<TResult>" => CodeGenTypeRepresentation.ValueTask,
        _ => CodeGenTypeRepresentation.Raw
    };
}