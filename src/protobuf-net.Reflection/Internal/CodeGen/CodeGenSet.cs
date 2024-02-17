#nullable enable
using Google.Protobuf.Reflection;
using ProtoBuf.Internal.CodeGen;
using ProtoBuf.Reflection.Internal.CodeGen.Collections;
using System.Collections.Generic;
using System.IO;

namespace ProtoBuf.Reflection.Internal.CodeGen;

internal class CodeGenSet
{

    public CodeGenSet(IDiagnosticSink? diagnosticsSink) => DiagnosticSink = diagnosticsSink;

    public IDiagnosticSink? DiagnosticSink { get; }

    public ICollection<CodeGenFile> Files { get; } = new NonNullableList<CodeGenFile>();
    public bool ShouldSerializeFiles() => Files.Count > 0;

    public static CodeGenSet Parse(FileDescriptorSet descriptorSet, CodeGenDescriptorParseContext context)
    {
        var set = new CodeGenSet(null);
        foreach (var file in descriptorSet.Files)
        {
            if (!file.IncludeInOutput) continue;

            var newFile = new CodeGenFile(file.Name);
            string namespacePrefix = context.NameNormalizer.GetName(file);
            if (string.IsNullOrEmpty(namespacePrefix)) namespacePrefix = Path.GetFileNameWithoutExtension(file.Name);
            if (!string.IsNullOrWhiteSpace(namespacePrefix)) namespacePrefix += ".";
            
            foreach (var type in file.MessageTypes)
            {
                if (!context.AddMapEntry(type))
                {
                    newFile.Messages.Add(CodeGenMessage.Parse(type, namespacePrefix, context, file.Package));
                }
            }
            foreach (var type in file.EnumTypes)
            {
                newFile.Enums.Add(CodeGenEnum.Parse(type, namespacePrefix, context, file.Package));
            }
            foreach (var service in file.Services)
            {
                newFile.Services.Add(CodeGenService.Parse(service, namespacePrefix, context, file.Package));
            }
            set.Files.Add(newFile);
        }

        // note: if message/enum type is consumed before it is defined, we simplify things
        // by using a place-holder initially (via the protobuf FQN); we need to go back over the
        // tree, and substitute out any such place-holders for the final types
        context.FixupPlaceholders();

        return set;
    }
}
