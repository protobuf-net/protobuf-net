using System.Collections.Generic;
using System.IO;
using ProtoBuf.Internal;
using ProtoBuf.Internal.ProtoUnion;

namespace ProtoBuf.Reflection
{
    public partial class CSharpCodeGenerator
    {
        internal IEnumerable<CodeFile> Generate(IEnumerable<ProtoUnionClassDescriptor> protoUnionClassDescriptors, NameNormalizer normalizer = null)
        {
            foreach (var protoUnionClassDescriptor in protoUnionClassDescriptors)
            {
                var fileName = Path.ChangeExtension(file.Name, DefaultFileExtension);

                string generated;
                using (var buffer = new StringWriter())
                {
                    var ctx = new GeneratorContext(this, file, normalizer, buffer, Indent, options);

                    ctx.BuildTypeIndex(); // populates for TryFind<T>
                    WriteFile(ctx, file);
                    generated = buffer.ToString();
                }
                
                yield return new CodeFile(fileName, generated);
            }
        }   
    }
}