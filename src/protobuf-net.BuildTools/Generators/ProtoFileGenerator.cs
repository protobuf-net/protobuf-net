#nullable enable
using Google.Protobuf.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using ProtoBuf.BuildTools.Internal;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ProtoBuf.Generators.Abstractions;

namespace ProtoBuf.BuildTools.Generators
{
    /// <summary>
    /// Generates protobuf-net types from .proto schemas
    /// </summary>
    [Generator]
    public sealed class ProtoFileGenerator : GeneratorBase
    {
        /// <inheritdoc/>
        public override void Execute(GeneratorExecutionContext context)
        {
            try
            {
                var log = Log;
               Startup(context);

                var schemas = context.AdditionalFiles.Where(at => at.Path.EndsWith(".proto")).Select(at => new NormalizedAdditionalText(at)).ToImmutableArray();
                if (schemas.IsDefaultOrEmpty)
                {
                    log?.Invoke("No .proto schemas found");
                    return;
                }
                
                if (!TryDetectCodeGenerator(context, out var codeGenerator, out var langver))
                {
                    return;
                }

                // find anything that matches our files
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
                        if (extraPaths.IndexOf(',') >= 0)
                        {
                            foreach (var part in extraPaths.Split(','))
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

                    bool includeInOutput = true;
                    if (userOptions is not null && userOptions.TryGetValue(Literals.AdditionalFileMetadataPrefix + "IncludeInOutput", out var optionValue) && bool.TryParse(optionValue, out var tmpIncludeInOutput))
                    {
                        includeInOutput = tmpIncludeInOutput;
                    }
                    
                    if (!set.Add(name, includeInOutput))
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

                        var services = ProtobufGrpcVersion switch
                        {   // automatically generate services *if* the consumer is referencing either the WCF or gRPC bits
                            not null when WcfVersion is not null => "grpc;wcf",
                            not null => "grpc",
                            null when WcfVersion is not null => "wcf",
                            _ => null,
                        };
                        if (services is not null)
                        {
                            options.Add("services", services);
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
                            AddOption(Literals.AdditionalFileMetadataPrefix + "Bytes", "bytes");
                            AddOption(Literals.AdditionalFileMetadataPrefix + "NullWrappers", "nullwrappers");
                            AddOption(Literals.AdditionalFileMetadataPrefix + "CompatLevel", "compatlevel");
                            AddOption(Literals.AdditionalFileMetadataPrefix + "NullableValueType", "nullablevaluetype");
                            AddOption(Literals.AdditionalFileMetadataPrefix + "RepeatedAsList", "repeatedaslist");

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

                        var files = codeGenerator!.Generate(set, options: options);
                        var root = Directory.GetCurrentDirectory();
                        foreach (var file in files)
                        {
                            // not allowed to use path qualifiers
                            // fix not unique HintName (AddSource method)
                            // like embedded resource path: xxx.xxx.xxx.generated.cs

                            // absolute path issue
                            // oh, you can specify any path, so ...
                            var fullName = Path.GetFullPath(file.Name);
                            var finalName =
                            (
                                // try to fix absolute path (if the file is in the project folder)
                                fullName.StartsWith(root) ? fullName.Substring(root.Length) :
                                // skip drive letter (windows fix)
                                fullName.IndexOf(@":\") is var lpos && lpos != -1 ? fullName.Substring(lpos + 1) :
                                // else
                                fullName
                            )
                            // normalize name
                            .Replace(Path.DirectorySeparatorChar, '.').TrimStart('.');

                            // set 'generated' file extension prefix
                            finalName = Path.ChangeExtension(finalName, $"generated{Path.GetExtension(file.Name)}");

                            log?.Invoke($"Adding: '{finalName}' ({file.Text.Length} characters) [{file.Name}]");
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
                Log($"Exception: '{ex.Message}' ({ex.GetType().Name})");
                Log(ex.StackTrace);
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
