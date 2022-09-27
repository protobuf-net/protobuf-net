#nullable enable
using Google.Protobuf.Reflection;
using System.Collections.Generic;
using System.ComponentModel;
using ProtoBuf.Reflection.Internal.CodeGen.Collections;

namespace ProtoBuf.Reflection.Internal.CodeGen;

internal class CodeGenService : CodeGenType
{
    [DefaultValue(CodeGenGenerate.All)]
    public CodeGenGenerate Emit { get; set; } = CodeGenGenerate.All;

    public CodeGenService(string name, string fullyQualifiedPrefix) : base(name, fullyQualifiedPrefix)
    {
        OriginalName = base.Name;
    }

    public new string Name
    {
        get => base.Name;
        set => base.Name = value;
    }
    
    public string OriginalName { get; set; }
    public string Package { get; set; } = string.Empty;

    private NonNullableList<CodeGenServiceMethod> _serviceMethods;
    public ICollection<CodeGenServiceMethod> ServiceMethods => _serviceMethods ??= new();
    
    [DefaultValue(Access.Public)]
    public Access Access { get; set; } = Access.Public;
    
    public bool ShouldSerializeOriginalName() => OriginalName != Name;
    public bool ShouldSerializePackage() => !string.IsNullOrWhiteSpace(Package);

    internal static CodeGenService Parse(ServiceDescriptorProto service, string fullyQualifiedPrefix, CodeGenParseContext context, string package)
    {
        var name = context.NameNormalizer.GetName(service);
        var newService = new CodeGenService(name, fullyQualifiedPrefix);
        context.Register(service.FullyQualifiedName, newService);
        newService.OriginalName = service.Name;
        newService.Package = package;

        if (service.Methods.Count > 0)
        {
            foreach (var method in service.Methods)
            {
                newService.ServiceMethods.Add(CodeGenServiceMethod.Parse(method, context));
            }
        }

        return newService;
    }
    
    internal void FixupPlaceholders(CodeGenParseContext context)
    {
    }
}
