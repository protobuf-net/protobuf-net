#nullable enable
using Google.Protobuf.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Reflection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ProtoBuf.BuildTools.Generators
{
    /// <summary>
    /// Generates protobuf-net types from .proto schemas
    /// </summary>
    [Generator]
    public sealed class ProtoFileGenerator : ISourceGenerator, ILoggingAnalyzer
    {
        private event Action<string>? Log;
        event Action<string>? ILoggingAnalyzer.Log
        {
            add => this.Log += value;
            remove => this.Log -= value;
        }

        void ISourceGenerator.Initialize(GeneratorInitializationContext context) { }

        void ISourceGenerator.Execute(GeneratorExecutionContext context)
        {
            try
            {
                var log = Log;
                log?.Invoke($"Execute with debug log enabled");

                Version?
                    pbnetVersion = context.Compilation.GetProtobufNetVersion(),
                    pbnetGrpcVersion = context.Compilation.GetReferenceVersion("protobuf-net.Grpc"),
                    wcfVersion = context.Compilation.GetReferenceVersion("System.ServiceModel.Primitives");

                log?.Invoke($"Referencing protobuf-net {ShowVersion(pbnetVersion)}, protobuf-net.Grpc {ShowVersion(pbnetGrpcVersion)}, WCF {ShowVersion(wcfVersion)}");

                string ShowVersion(Version? version)
                    => version is null ? "(n/a)" : $"v{version}";

                if (log is not null)
                {
                    foreach (var ran in context.Compilation.ReferencedAssemblyNames.OrderBy(x => x.Name))
                    {
                        log($"reference: {ran.Name} v{ran.Version}");
                    }
                }

                var schemas = context.AdditionalFiles.Where(at => at.Path.EndsWith(".proto")).Select(at => new NormalizedAdditionalText(at)).ToImmutableArray();
                if (schemas.IsDefaultOrEmpty)
                {
                    log?.Invoke("No .proto schemas found");
                    return;
                }

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
                        log?.Invoke($"Unexpected language: {context.Compilation.Language}");
                        return; // nothing doing
                }
                log?.Invoke($"Detected {generator.Name} v{langver}");

                var fileSystem = new AdditionalFilesFileSystem(log, schemas);
                foreach (var schema in schemas)
                {
                    var set = new FileDescriptorSet
                    {
                        FileSystem = fileSystem
                    };

                    var name = Path.GetFileName(schema.Value.Path);
                    var location = Path.GetDirectoryName(schema.Value.Path);
                    log?.Invoke($"Processing '{name}' relative to '{location}'");

                    var userOptions = context.AnalyzerConfigOptions.GetOptions(schema.Value);
                    set.AddImportPath(location);
                    if (userOptions is not null && userOptions.TryGetValue(Literals.AdditionalFileMetadataPrefix + "ImportPaths", out var extraPaths) && !string.IsNullOrWhiteSpace(extraPaths))
                    {
                        var baseUri = new Uri("file://" + schema.Value.Path, UriKind.Absolute);
                        if (extraPaths.IndexOf(';') >= 0)
                        {
                            foreach (var part in extraPaths.Split(';'))
                            {
                                AddExtraPath(part);
                            }
                        }
                        else
                        {
                            AddExtraPath(extraPaths);
                        }
                        void AddExtraPath(string? fragment)
                        {
                            fragment = fragment?.Trim();
                            if (!string.IsNullOrWhiteSpace(fragment))
                            {
                                try
                                {
                                    var relative = new Uri(baseUri, fragment);
                                    if (relative.IsAbsoluteUri)
                                    {
                                        var combined = relative.LocalPath;
                                        log?.Invoke($"Adding extra import path '{relative.AbsolutePath}'");
                                        set.AddImportPath(relative.AbsolutePath);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    log?.Invoke($"Failed to add relative path '{fragment}': {ex.Message}");
                                }
                            }
                        }
                    }
                    
                    if (!set.Add(name))
                    {
                        log?.Invoke($"Failed to add '{name}'; skipping");
                        continue;
                    }

                    set.Process();
                    var errors = set.GetErrors();
                    log?.Invoke($"Parsed schema with {errors.Count(x => x.IsError)} errors, {errors.Count(x => x.IsWarning)} warnings, {set.Files.Count} files");
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
                        const int ErrorNumberOffset = 1000;
                        log?.Invoke(error.ToString());
                        context.ReportDiagnostic(Diagnostic.Create($"PBN{(error.ErrorNumber + ErrorNumberOffset).ToString("0000", CultureInfo.InvariantCulture)}",
                            "Protobuf", error.Message, level, level, true, error.IsError ? 0 : 2,
                            location: Location.Create(error.File, default, span)));
                    }
                    if (set.Files.Any())
                    {
                        var options = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                        if (langver is not null) options.Add("langver", langver);

                        var services = pbnetGrpcVersion switch
                        {   // automatically generate services *if* the consumer is referencing either the WCF or gRPC bits
                            not null when wcfVersion is not null => "grpc;wcf",
                            not null => "grpc",
                            null when wcfVersion is not null => "wcf",
                            _ => null,
                        };
                        if (services is not null)
                        {
                            options.Add("services", "yes");
                        }

                        if (userOptions is not null)
                        {
                            // copy over any keys that we know the tooling might want
                            AddOption(Literals.AdditionalFileMetadataPrefix + "ListSet", "listset");
                            AddOption(Literals.AdditionalFileMetadataPrefix + "OneOf", "oneof");
                            AddOption(Literals.AdditionalFileMetadataPrefix + "Services", "services");
                            AddOption(Literals.AdditionalFileMetadataPrefix + "LangVersion", "langver");
                            AddOption(Literals.AdditionalFileMetadataPrefix + "Package", "package");
                            AddOption(Literals.AdditionalFileMetadataPrefix + "Names", "names");

                            void AddOption(string readKey, string writeKey)
                            {
                                if (userOptions.TryGetValue(readKey, out string? optionValue))
                                {
                                    options[writeKey] = optionValue;
                                }
                            }
                        }

                        if (log is not null)
                        {
                            foreach (var pair in options.OrderBy(x => x.Key))
                            {
                                log($": {pair.Key}={pair.Value}");
                            }
                        }

                        var files = generator.Generate(set, options: options);
                        foreach (var file in files)
                        {
                            var finalName = Path.GetFileName(file.Name); // not allowed to use path qualifiers
                            var ext = Path.GetExtension(finalName);
                            if (!ext.StartsWith(".generated."))
                            {
                                finalName = Path.ChangeExtension(finalName, "generated" + ext);
                            }
                            log?.Invoke($"Adding: '{finalName}' ({file.Text.Length} characters)");
                            context.AddSource(finalName, SourceText.From(file.Text, Encoding.UTF8));
                        }
                    }
                    if (log is not null)
                    {
                        log($"Completed '{name}'");
                        log("");
                    }
                }
            }
            catch (Exception ex)
            {
                if (Log is null) throw;
                Log.Invoke($"Exception: '{ex.Message}' ({ex.GetType().Name})");
                Log.Invoke(ex.StackTrace);
            }
        }

        readonly struct NormalizedAdditionalText
        {
            public AdditionalText Value { get; }
            public string NormalizedPath { get; }
            public NormalizedAdditionalText(AdditionalText additionalText)
            {
                Value = additionalText;
                NormalizedPath = AdditionalFilesFileSystem.NormalizePath(additionalText.Path);
            }
        }

        private class AdditionalFilesFileSystem : IFileSystem
        {
            public static string NormalizePath(string path)
                => path?.Replace('/', '\\') ?? "";

            private readonly Action<string>? _log;
            private readonly ImmutableArray<NormalizedAdditionalText> _schemas;

            public AdditionalFilesFileSystem(Action<string>? log, ImmutableArray<NormalizedAdditionalText> schemas)
            {
                _log = log;
                _schemas = schemas;
            }

            bool IFileSystem.Exists(string path)
            {
                path = NormalizePath(path);
                var found = Find(path);
                _log?.Invoke($"Checking for '{path}': {(found is not null ? "found" : "not found")}");
                return found is not null;

            }

            private AdditionalText? Find(string path)
            {
                path = NormalizePath(path);
                foreach (var schema in _schemas)
                {
                    if (schema.NormalizedPath == path)
                        return schema.Value;
                }
                return default;
            }

            TextReader? IFileSystem.OpenText(string path)
            {
                path = NormalizePath(path);
                var content = Find(path)?.GetText()?.ToString();
                if (content is null)
                {
                    _log?.Invoke($"opening '{path}': not found");
                    return null;
                }

                _log?.Invoke($"opening '{path}': {content.Length} characters");
                return new StringReader(content);
            }
        }
    }
}