using Google.Protobuf.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using ProtoBuf.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ProtoBuf.BuildTools
{
    [Generator]
    public sealed class ProtoFileGenerator : ISourceGenerator
    {
        void ISourceGenerator.Initialize(GeneratorInitializationContext context) { }

        void ISourceGenerator.Execute(GeneratorExecutionContext context)
        {
            File.WriteAllText(@"c:\Code\gen.txt", "I haz exist" + Environment.NewLine);
            // find anything that matches our files
            CodeGenerator generator;
            string langver;
            
            switch (context.Compilation.Language)
            {
                case "C#":
                    generator = CSharpCodeGenerator.Default;
                    langver = "9.0"; // TODO: lookup from context
                    break;
                case "VB":
                    generator = VBCodeGenerator.Default;
                    langver = "14.0"; // TODO: lookup from context
                    break;
                default:
                    return; // nothing doing
            }
            var schemas = context.AdditionalFiles.Where(at => at.Path.EndsWith(".proto"));
            var set = new FileDescriptorSet();
            foreach (var schema in schemas)
            {
                
                var content = schema.GetText(context.CancellationToken);
                if (content is null) continue;

                using (var sr = new StringReader(content.ToString()))
                {
                    File.AppendAllText(@"c:\Code\gen.txt", schema.Path + Environment.NewLine);
                    set.Add(Path.GetFileName(schema.Path), true, sr);
                }
                set.Process();
                var errors = set.GetErrors();
                foreach (var error in errors)
                {
                    var position = new LinePosition(error.LineNumber, error.ColumnNumber);
                    var level = error.IsError ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning;
                    context.ReportDiagnostic(Diagnostic.Create($"PBN{error.ErrorNumber}", "Protobuf", error.Message, level, level, true, error.IsError ? 0 : 2,
                        location: Location.Create(schema.Path, default, new LinePositionSpan(position, position))));
                }
            }
            if (set.Files.Any())
            {
                var options = new Dictionary<string, string>
                {
                    {"services", "yes" },
                    {"oneof", "yes" },
                };
                if (langver is string) options.Add("langver", langver);
                var files = generator.Generate(set, options: options);
                foreach (var file in files)
                {
                    context.AddSource(file.Name, SourceText.From(file.Text, Encoding.UTF8));
                }
            }
        }
    }
}
