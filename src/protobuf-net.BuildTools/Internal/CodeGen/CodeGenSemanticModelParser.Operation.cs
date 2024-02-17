#nullable enable
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Reflection.Internal.CodeGen;
using System.Linq;

namespace ProtoBuf.Internal.CodeGen;

internal partial class CodeGenSemanticModelParser
{
    public CodeGenServiceMethod? ParseOperation(IMethodSymbol symbol)
    {
        var responseType = ParseResponseType(symbol);
        var (requestType, parametersDescriptor) = ParseParameters(symbol);

        return new CodeGenServiceMethod(symbol.Name, symbol)
        {
            RequestType = requestType,
            ResponseType = responseType,
            ParametersDescriptor = parametersDescriptor
        };
    }

    private (CodeGenServiceMethod.Type? requestType, CodeGenServiceMethodParametersDescriptor parametersDescriptor) ParseParameters(IMethodSymbol symbol)
    {
        var parameters = symbol.Parameters;
        CodeGenServiceMethod.Type? requestType = null;
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
        var namedTypeSymbol = (INamedTypeSymbol)typeSymbol;
        if (namedTypeSymbol.Arity == 0)
        {
            // simply it is a raw type
            return new CodeGenServiceMethod.Type
            {
                RawType = Context.GetContractType(typeSymbol.ToString()),
                Representation = CodeGenTypeRepresentation.Raw
            }; 
        }
        
        var genericTypeDefinition = namedTypeSymbol.OriginalDefinition;
        var typeArgumentTypeDefinition = namedTypeSymbol.TypeArguments.FirstOrDefault();

        return new CodeGenServiceMethod.Type
        {
            RawType = typeArgumentTypeDefinition is null ? CodeGenType.Unknown : Context.GetContractType(typeArgumentTypeDefinition.GetFullyQualifiedType()),
            Representation = DetermineGenericTypeRepresentation(genericTypeDefinition)
        };
    }

    private static bool IsGrpcCallContext(IParameterSymbol parameterSymbol) => parameterSymbol.Type.Name == "CallContext" && parameterSymbol.Type.InProtoBufGrpcNamespace();
    
    private static bool IsCancellationToken(IParameterSymbol parameterSymbol) => parameterSymbol.Type.Name == "CancellationToken" && parameterSymbol.Type.InThreadingNamespace();

    private static CodeGenTypeRepresentation DetermineGenericTypeRepresentation(INamedTypeSymbol symbol) => symbol.Name switch
    {
        "IAsyncEnumerable" when symbol.InGenericCollectionsNamespace() && symbol.IsGenericType => CodeGenTypeRepresentation.AsyncEnumerable,
        "Task" when symbol.InThreadingTasksNamespace() => CodeGenTypeRepresentation.Task,
        "ValueTask" when symbol.InThreadingTasksNamespace() => CodeGenTypeRepresentation.ValueTask,
        _ => CodeGenTypeRepresentation.Raw
    };
}