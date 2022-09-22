#nullable enable
using Google.Protobuf.Reflection;

namespace ProtoBuf.Reflection.Internal.CodeGen;

internal class CodeGenServiceMethod
{
    public CodeGenServiceMethod(string name)
    {
        Name = name;
    }

    private RequestType? _requestType;
    private ResponseType? _responseType;
    
    public string Name { get; set; }
    public string OriginalName { get; set; }
    public RequestType Request => _requestType ??= new();
    public ResponseType Response => _responseType ??= new();

    internal void FixupPlaceholders(CodeGenParseContext context)
    {
    }

    internal static CodeGenServiceMethod Parse(MethodDescriptorProto method, CodeGenParseContext context)
    {
        var name = context.NameNormalizer.GetName(method);
        var newMethod = new CodeGenServiceMethod(name)
        {
            OriginalName = method.Name,
            Request =
            {
                Type = context.GetContractType(method.InputType),
                IsStreamed = method.ClientStreaming
            },
            Response =
            {
                Type = context.GetContractType(method.OutputType),
                IsStreamed = method.ServerStreaming
            }
        };

        return newMethod;
    }

    public class ResponseType
    {
        public bool IsStreamed { get; set; }
        
        public CodeGenType Type { get; set; }
        
        public override string ToString()
        {
            return IsStreamed 
                ? $"global::System.Threading.Tasks.IAsyncEnumerable<{Type}>" 
                : $"global::System.Threading.Tasks.ValueTask<{Type}>";
        }
    }

    public class RequestType
    {
        public bool IsStreamed { get; set; }
        
        public CodeGenType Type { get; set; }

        public override string ToString()
        {
            return IsStreamed 
                ? $"global::System.Threading.Tasks.IAsyncEnumerable<{Type}>" 
                : $"global::System.Threading.Tasks.ValueTask<{Type}>";
        }
    }
}
