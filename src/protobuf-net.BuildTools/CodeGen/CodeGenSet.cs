#nullable enable
using Google.Protobuf.Reflection;
using System.Collections.Generic;

namespace ProtoBuf.CodeGen;

internal class CodeGenSet
{
    public static CodeGenSet Parse(FileDescriptorSet descriptorSet, CodeGenContext context)
    {
        var set = new CodeGenSet();
        foreach (var file in descriptorSet.Files)
        {
            if (!file.IncludeInOutput) continue;

            var newFile = new CodeGenFile(file.Name);
            string namespacePrefix = context.NameNormalizer.GetName(file);
            if (!string.IsNullOrWhiteSpace(namespacePrefix)) namespacePrefix += ".";
            foreach (var type in file.MessageTypes)
            {
                newFile.Messages.Add(CodeGenMessage.Parse(type, namespacePrefix, context, file.Package));
            }
            foreach (var type in file.EnumTypes)
            {
                newFile.Enums.Add(CodeGenEnum.Parse(type, namespacePrefix, context, file.Package));
            }
            set.Files.Add(newFile);
        }
        return set;
    }

    public List<CodeGenFile> Files { get; } = new List<CodeGenFile>();
    public bool ShouldSerializeFiles() => Files.Count > 0;
}
