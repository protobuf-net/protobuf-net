#nullable enable
using Google.Protobuf.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using ProtoBuf.Reflection;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ProtoBuf.BuildTools
{
    /// <summary>
    /// Generates protobuf-net types from .proto schemas
    /// </summary>
    [Generator]
    public sealed class ProtoFileGenerator : ISourceGenerator
    {
        void ISourceGenerator.Initialize(GeneratorInitializationContext context) { }

        private sealed class Logger
        {
            public Logger(GeneratorExecutionContext context)
                => _context = context;
            private readonly GeneratorExecutionContext _context;

            public void Write(string message)
                => _context.ReportDiagnostic(Diagnostic.Create("PBN9999", "Debug", message, DiagnosticSeverity.Info, DiagnosticSeverity.Info, true, -1));
        }
        static bool TryReadBoolSetting(in GeneratorExecutionContext context, string key, bool defaultValue = false)
            => context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(key, out var s)
                && s is not null && bool.TryParse(s, out bool b) ? b : defaultValue;

        void ISourceGenerator.Execute(GeneratorExecutionContext context)
        {
            bool debugLog = TryReadBoolSetting(context, "pbn_debug_log") || TryReadBoolSetting(context, "build_property.ProtoBufNet_DebugLog");

            Logger? log = debugLog ? new Logger(context) : default;

            log?.Write($"Execute with debug log enabled");

            // find anything that matches our files
            CodeGenerator generator;
            string? langver = null;

            switch (context.Compilation.Language)
            {
                case "C#":
                    generator = CSharpCodeGenerator.Default;
                    if (context.ParseOptions is CSharpParseOptions cs)
                    {
                        langver = cs.LanguageVersion switch
                        {
                            LanguageVersion.CSharp1 => "1",
                            LanguageVersion.CSharp2 => "2",
                            LanguageVersion.CSharp3 => "3",
                            LanguageVersion.CSharp4 => "4",
                            LanguageVersion.CSharp5 => "5",
                            LanguageVersion.CSharp6 => "6",
                            LanguageVersion.CSharp7 => "7",
                            LanguageVersion.CSharp7_1 => "7.1",
                            LanguageVersion.CSharp7_2 => "7.2",
                            LanguageVersion.CSharp7_3 => "7.3",
                            LanguageVersion.CSharp8 => "8",
                            LanguageVersion.CSharp9 => "9",
                            _ => null
                        };
                    }
                    break;
                //case "VB": // completely untested, and pretty sure this isn't even a "thing"
                //    generator = VBCodeGenerator.Default;
                //    langver = "14.0"; // TODO: lookup from context
                //    break;
                default:
                    log?.Write($"Unexpected language: {context.Compilation.Language}");
                    return; // nothing doing
            }
            log?.Write($"Detected {generator.Name} v{langver}");

            var schemas = context.AdditionalFiles.Where(at => at.Path.EndsWith(".proto"));
            var set = new FileDescriptorSet();
            foreach (var schema in schemas)
            {
                var content = schema.GetText(context.CancellationToken);
                if (content is null) continue;

                var contentString = content.ToString();
                log?.Write($"Processing '{schema.Path}' ({contentString.Length} characters)...");

                using (var sr = new StringReader(contentString))
                {
                    set.Add(Path.GetFileName(schema.Path), true, sr);
                }
                set.Process();
                var errors = set.GetErrors();
                log?.Write($"Parsed '{schema.Path}' with {errors.Length} errors/warnings");
                foreach (var error in errors)
                {
                    var position = new LinePosition(error.LineNumber - 1, error.ColumnNumber - 1); // zero index on these positions
                    var span = new LinePositionSpan(position, position);
                    var txt = error.Text;
                    if (txt.IndexOf('\r') < 0 && txt.IndexOf('\n') < 0)
                    {
                        // all on 1 line - we can use the length of txt to construct a span, rather than a single position
                        span = new LinePositionSpan(position, new LinePosition(position.Line, position.Character + txt.Length));
                    }

                    var level = error.IsError ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning;
                    context.ReportDiagnostic(Diagnostic.Create($"PBN1{error.ErrorNumber.ToString("000", CultureInfo.InvariantCulture)}",
                        "Protobuf", error.Message, level, level, true, error.IsError ? 0 : 2,
                        location: Location.Create(schema.Path, default, span)));
                }
            }
            log?.Write($"Files generated: {set.Files.Count}");
            if (set.Files.Any())
            {
                var options = new Dictionary<string, string>
                {
                    {"services", TryReadBoolSetting(context, "build_property.ProtoBufNet_GenerateServices", true) ? "yes" : "no" },
                    {"oneof", TryReadBoolSetting(context, "build_property.ProtoBufNet_UseOneOf", true) ? "yes" : "no" },
                };
                if (langver is string) options.Add("langver", langver);
                var files = generator.Generate(set, options: options);
                foreach (var file in files)
                {
                    var finalName = file.Name;
                    var ext = Path.GetExtension(finalName);
                    if (!ext.StartsWith(".generated."))
                    {
                        finalName = Path.ChangeExtension(finalName, "generated" + ext);
                    }
                    log?.Write($"Adding: '{finalName}' ({file.Text.Length} characters)");
                    context.AddSource(finalName, SourceText.From(file.Text, Encoding.UTF8));
                }
            }
        }
    }
}