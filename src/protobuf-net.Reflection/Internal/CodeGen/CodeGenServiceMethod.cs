#nullable enable
using System;
using Google.Protobuf.Reflection;

namespace ProtoBuf.Reflection.Internal.CodeGen;

internal class CodeGenServiceMethod
{
    public CodeGenServiceMethod(string name)
    {
        Name = name;
    }

    private Type? _requestType;
    private Type? _responseType;
    
    public string Name { get; set; }
    public string OriginalName { get; set; }
    public Type RequestType
    {
        get { return _requestType ??= new(); }
        set => _requestType = value;
    }
    public Type ResponseType
    {
        get { return _responseType ??= new(); }
        set => _responseType = value;
    }

    public CodeGenServiceMethodParametersDescriptor ParametersDescriptor { get; set; }
        = CodeGenServiceMethodParametersDescriptor.None;

    internal void FixupPlaceholders(CodeGenParseContext context)
    {
    }

    internal static CodeGenServiceMethod Parse(MethodDescriptorProto method, CodeGenParseContext context)
    {
        var name = context.NameNormalizer.GetName(method);
        var newMethod = new CodeGenServiceMethod(name)
        {
            OriginalName = method.Name,
            RequestType =
            {
                RawType = context.GetContractType(method.InputType),
                Representation = method.ClientStreaming ? CodeGenTypeRepresentation.AsyncEnumerable : CodeGenTypeRepresentation.ValueTask
            },
            ResponseType =
            {
                RawType = context.GetContractType(method.OutputType),
                Representation = method.ServerStreaming ? CodeGenTypeRepresentation.AsyncEnumerable : CodeGenTypeRepresentation.ValueTask
            }
        };

        return newMethod;
    }


    public class Type
    {
        public CodeGenTypeRepresentation Representation { get; set; }
        
        public CodeGenType RawType { get; set; }
        
        public override string ToString()
        {
            return Representation switch
            {
                CodeGenTypeRepresentation.Raw => RawType.ToString(),
                CodeGenTypeRepresentation.ValueTask => $"global::System.Threading.Tasks.ValueTask<global::{RawType}>",
                CodeGenTypeRepresentation.Task => $"global::System.Threading.Tasks.Task<global::{RawType}>",
                CodeGenTypeRepresentation.AsyncEnumerable => $"global::System.Collections.Generic.IAsyncEnumerable<global::{RawType}>",
                _ => throw new ArgumentOutOfRangeException(nameof(CodeGenTypeRepresentation))
            };
                
        }   
    }
}
