using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                    WriteDiscriminatedUnionDefinitionFile(ctx, protoUnionFileDescriptor);
                    generatedFileText = buffer.ToString();
                }
                
                yield return new CodeFile(fileName, generatedFileText);
            }
        }

        void WriteDiscriminatedUnionDefinitionFile(GeneratorContext ctx, ProtoUnionFileDescriptor protoUnionFileDescriptor)
        {
            var unionTypes = protoUnionFileDescriptor.UnionTypes;
            
            object state = null;
            WriteFileHeader(ctx, ctx.File, ref state);
            WriteProtoBufUsing(ctx);
            
            WriteNamespaceHeader(ctx, protoUnionFileDescriptor.Namespace);
            WriteClassHeader(ctx, protoUnionFileDescriptor.Class, isPartial: true);
            
            foreach (var unionType in unionTypes)
            {
                WriteDiscriminatedUnionField(ctx, unionType.Key, unionType.Value);
                ctx.WriteLine();
            }

            foreach (var unionField in protoUnionFileDescriptor.UnionFields)
            {
                var unionType = unionTypes[unionField.UnionName];
                WriteDiscriminatedUnionMember(ctx, unionField, unionType);
                ctx.WriteLine();
            }

            WriteClassFooter(ctx, protoUnionFileDescriptor.Class);
            WriteNamespaceFooter(ctx, protoUnionFileDescriptor.Namespace);
            WriteFileFooter(ctx, ctx.File, ref state);
        }

        void WriteDiscriminatedUnionField(GeneratorContext ctx, string unionName, DiscriminatedUnionType discriminatedUnionType)
        {
            ctx.WriteLine($"private {discriminatedUnionType.GetTypeName()} __pbn_{unionName};");
            ctx.WriteLine($"[ProtoIgnore] public int {unionName} => __pbn_{unionName}.Discriminator;");
        }

        void WriteDiscriminatedUnionMember(GeneratorContext ctx, ProtoUnionField field, DiscriminatedUnionType discriminatedUnionType)
        {
            ctx.WriteLine($"[ProtoMember({field.FieldNumber})]")
                .WriteLine($"public {field.CSharpType}? {field.MemberName}")
                .WriteLine("{").Indent()
                    .WriteLine($"get => __pbn_{field.UnionName}.Is({field.FieldNumber}) ? ({field.CSharpType})__pbn_{field.UnionName}.{field.UnionUsageFieldName} : default;")
                    .WriteLine("set")
                    .WriteLine("{")
                    .Indent()
                        .WriteLine("if (value is null)")
                        .WriteLine("{").Indent()
                            .WriteLine($"{discriminatedUnionType.GetTypeName()}.Reset(ref __pbn_{field.UnionName}, {field.FieldNumber});")
                        .Outdent().WriteLine("}")
                        .WriteLine("else")
                        .WriteLine("{").Indent()
                            .WriteLine($"__pbn_{field.UnionName} = new ({field.FieldNumber}, value);")
                        .Outdent().WriteLine("}")
                    .Outdent().WriteLine("}")
                .Outdent().WriteLine("}");
            
            //     public bool ShouldSerializeBar() => __pbn_Abc.Is(1);
            ctx.WriteLine($"public bool ShouldSerialize{field.MemberName}() => __pbn_{field.UnionName}.Is({field.FieldNumber});");
        }
    }
}