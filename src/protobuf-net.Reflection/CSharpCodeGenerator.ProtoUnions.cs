using System.Collections.Generic;
using System.IO;
using Google.Protobuf.Reflection;
using ProtoBuf.Internal.ProtoUnion;

namespace ProtoBuf.Reflection
{
    public partial class CSharpCodeGenerator
    {
        internal IEnumerable<CodeFile> Generate(IEnumerable<ProtoUnionFileDescriptor> protoUnionClassDescriptors, NameNormalizer normalizer = null)
        {
            foreach (var protoUnionFileDescriptor in protoUnionClassDescriptors)
            {
                var fileName = Path.ChangeExtension(protoUnionFileDescriptor.Filename, DefaultFileExtension);
                
                string generatedFileText;
                using (var buffer = new StringWriter())
                {
                    var fileDescriptorProto = new FileDescriptorProto
                    {
                        Name = protoUnionFileDescriptor.Filename
                    };
                    
                    var ctx = new GeneratorContext(this, file: fileDescriptorProto, normalizer, buffer, Indent, options: null);
                    WriteFile(ctx, protoUnionFileDescriptor);
                    generatedFileText = buffer.ToString();
                }
                
                yield return new CodeFile(fileName, generatedFileText);
            }
        }

        void WriteFile(GeneratorContext ctx, ProtoUnionFileDescriptor protoUnionFileDescriptor)
        {
            object state = null;
            WriteFileHeader(ctx, ctx.File, ref state);
            WriteNamespaceHeader(ctx, protoUnionFileDescriptor.Namespace);
            WriteClassHeader(ctx, protoUnionFileDescriptor.Class, isPartial: true);
            
                    
                    
            WriteClassFooter(ctx, protoUnionFileDescriptor.Class);
            WriteNamespaceFooter(ctx, protoUnionFileDescriptor.Namespace);
            WriteFileFooter(ctx, ctx.File, ref state);
        }
    }
}