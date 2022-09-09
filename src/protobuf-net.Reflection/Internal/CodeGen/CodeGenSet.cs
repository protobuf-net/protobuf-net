#nullable enable
using Google.Protobuf.Reflection;
using System.Collections.Generic;

namespace ProtoBuf.Reflection.Internal.CodeGen;

internal class CodeGenSet
{
    public static CodeGenSet Parse(FileDescriptorSet descriptorSet, CodeGenParseContext context)
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

        // note: if message/enum type is consumed before it is defined, we simplify things
        // by using a place-holder initially (via the protobuf FQN); we need to go back over the
        // tree, and substitute out any such place-holders for the final types
        set.FixupPlaceholders(context);

        return set;
    }

    private void FixupPlaceholders(CodeGenParseContext context)
    {
        if (ShouldSerializeFiles())
        {
            foreach (var file in Files)
            {
                file.FixupPlaceholders(context);
            }
        }
    }

    public List<CodeGenFile> Files { get; } = new List<CodeGenFile>();
    public bool ShouldSerializeFiles() => Files.Count > 0;
}
