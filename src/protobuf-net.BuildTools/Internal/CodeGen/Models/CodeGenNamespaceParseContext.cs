namespace ProtoBuf.Internal.CodeGen.Models;

internal record CodeGenNamespaceParseContext
{
    public string NamespaceName { get; }

    public CodeGenNamespaceParseContext(string namespaceName)
    {
        NamespaceName = namespaceName;
    }
}