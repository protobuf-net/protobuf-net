using System.Linq;
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
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
        var (requestType, parametersDescriptor) = ParseParameters(symbol);

        return new CodeGenServiceMethod(symbol.Name)
        {
            RequestType = requestType,
            ResponseType = responseType,
            ParametersDescriptor = parametersDescriptor
        };
    }

    private (CodeGenServiceMethod.Type requestType, CodeGenServiceMethodParametersDescriptor parametersDescriptor) ParseParameters(IMethodSymbol symbol)
    {
        var parameters = symbol.Parameters;
        CodeGenServiceMethod.Type requestType = null;
        var parametersDescriptor = CodeGenServiceMethodParametersDescriptor.None;

        foreach (var parameter in parameters)
        {
            // skipping known predefined types
            // (we know that CallContext \ CancellationToken can not be a user request message type)
            if (IsGrpcCallContext(parameter))
            {
                parametersDescriptor |= CodeGenServiceMethodParametersDescriptor.HasCallContext;
                continue;
            }

            if (IsCancellationToken(parameter))
            {
                parametersDescriptor |= CodeGenServiceMethodParametersDescriptor.HasCancellationToken;
                continue;
            }
            
            // after we have passed predefined types,
            // we know it is a user message type. Let's parse it!
            requestType = ParseServiceMethodType(parameter.Type);
        }

        return (requestType, parametersDescriptor);
    } 
    
    private CodeGenServiceMethod.Type ParseResponseType(IMethodSymbol symbol)
    {
        return ParseServiceMethodType(symbol.ReturnType);
    }

    private CodeGenServiceMethod.Type ParseServiceMethodType(ITypeSymbol typeSymbol)
    {
        var namedTypeSymbol = typeSymbol as INamedTypeSymbol;
        if (namedTypeSymbol.Arity == 0)
        {
            // simply it is a raw type
            return new CodeGenServiceMethod.Type
            {
                RawType = ParseContext.GetContractType(typeSymbol.ToString()),
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

    private bool IsGrpcCallContext(IParameterSymbol parameterSymbol) => parameterSymbol.Type.ToString() == "ProtoBuf.Grpc.CallContext" && parameterSymbol.Type.InProtoBufNamespace();
    
    private bool IsCancellationToken(IParameterSymbol parameterSymbol) => parameterSymbol.Type.ToString() == "System.Threading.CancellationToken" && parameterSymbol.Type.InThreadingNamespace();

    private CodeGenTypeRepresentation DetermineGenericTypeRepresentation(INamedTypeSymbol symbol) => symbol.ToString() switch
    {
        "System.Collections.Generic.IAsyncEnumerable<T>" when symbol.InGenericCollectionsNamespace() => CodeGenTypeRepresentation.AsyncEnumerable,
        "System.Threading.Tasks.Task<T>" or "System.Threading.Tasks.Task<TResult>" when symbol.InThreadingTasksNamespace() => CodeGenTypeRepresentation.Task,
        "System.Threading.Tasks.ValueTask<T>" or "System.Threading.Tasks.ValueTask<TResult>" when symbol.InThreadingTasksNamespace() => CodeGenTypeRepresentation.ValueTask,
        _ => CodeGenTypeRepresentation.Raw
    };
}