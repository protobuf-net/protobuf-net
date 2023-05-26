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
            foreach (var fileDescriptor in protoUnionClassDescriptors)
            {
                var fileName = Path.ChangeExtension(
                    Path.GetFileNameWithoutExtension(fileDescriptor.Filename) + $"_{fileDescriptor.UnionName}",
                    DefaultFileExtension);
            
                string generatedFileText;
                using (var buffer = new StringWriter())
                {
                    var fileDescriptorProto = new FileDescriptorProto
                    {
                        Name = fileDescriptor.Filename
                    };
                
                    var ctx = new GeneratorContext(this, file: fileDescriptorProto, normalizer, buffer, Indent, options: null);
                    WriteDiscriminatedUnionDefinitionFile(ctx, fileDescriptor);
                    generatedFileText = buffer.ToString();
                }
            
                yield return new CodeFile(fileName, generatedFileText);
            }
        }

        void WriteDiscriminatedUnionDefinitionFile(GeneratorContext ctx, ProtoUnionFileDescriptor fileDescriptor)
        {
            object state = null;
            WriteFileHeader(ctx, ctx.File, ref state);
            WriteProtoBufUsing(ctx);
            
            WriteNamespaceHeader(ctx, fileDescriptor.Namespace);
            WriteClassHeader(ctx, fileDescriptor.Class, isPartial: true);
            
            WriteDiscriminatedUnionField(ctx, fileDescriptor.UnionName, fileDescriptor.UnionType);
            ctx.WriteLine();

            foreach (var unionField in fileDescriptor.UnionFields)
            {
                WriteDiscriminatedUnionMember(ctx, unionField, fileDescriptor.UnionType);
                ctx.WriteLine();
            }

            WriteClassFooter(ctx, fileDescriptor.Class);
            WriteNamespaceFooter(ctx, fileDescriptor.Namespace);
            WriteFileFooter(ctx, ctx.File, ref state);
        }

        void WriteDiscriminatedUnionField(GeneratorContext ctx, string unionName, DiscriminatedUnionType discriminatedUnionType)
        {
            ctx.WriteLine($"private {discriminatedUnionType.GetTypeName()} {GetUnionField(unionName)};");
            ctx.WriteLine($"[ProtoIgnore] public int {unionName} => {GetUnionField(unionName)}.Discriminator;");
        }

        void WriteDiscriminatedUnionMember(GeneratorContext ctx, ProtoUnionField field, DiscriminatedUnionType discriminatedUnionType)
        {
            ctx.WriteLine($"[ProtoMember({field.FieldNumber})]")
                .WriteLine($"public {field.CSharpType}? {field.MemberName}")
                .WriteLine("{").Indent()
                    .WriteLine($"get => {GetUnionField(field.UnionName)}.Is({field.FieldNumber}) ? ({field.CSharpType}){GetUnionField(field.UnionName)}.{field.UnionUsageFieldName} : default;")
                    .WriteLine("set")
                    .WriteLine("{")
                    .Indent()
                        .WriteLine("if (value is null)")
                        .WriteLine("{").Indent()
                            .WriteLine($"{discriminatedUnionType.GetTypeName()}.Reset(ref {GetUnionField(field.UnionName)}, {field.FieldNumber});")
                        .Outdent().WriteLine("}")
                        .WriteLine("else")
                        .WriteLine("{").Indent()
                            .WriteLine($"{GetUnionField(field.UnionName)} = new ({field.FieldNumber}, value);")
                        .Outdent().WriteLine("}")
                    .Outdent().WriteLine("}")
                .Outdent().WriteLine("}");
            
            //     public bool ShouldSerializeBar() => __pbn_Abc.Is(1);
            ctx.WriteLine($"public bool ShouldSerialize{field.MemberName}() => {GetUnionField(field.UnionName)}.Is({field.FieldNumber});");
        }

        internal static string GetUnionField(string unionName) => "__pbn_" + unionName;
    }
}