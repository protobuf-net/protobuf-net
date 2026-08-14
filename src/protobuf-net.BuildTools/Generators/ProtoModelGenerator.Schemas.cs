#nullable enable
using Google.Protobuf.Reflection;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.BuildTools.Internal.Aot;
using ProtoBuf.Reflection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace ProtoBuf.BuildTools.Generators
{
    partial class ProtoModelGenerator
    {
        /// <summary>
        /// One <c>.proto</c> additional file, reduced to plain data so it can live in the
        /// incremental pipeline.
        /// </summary>
        /// <remarks>
        /// The CONTENT is part of the cache key, deliberately: editing a schema must rebuild the
        /// model, and comparing paths alone would not notice.
        /// </remarks>
        internal readonly struct SchemaText : IEquatable<SchemaText>
        {
            public SchemaText(string path, string content)
            {
                Path = path;
                Content = content;
            }

            public string Path { get; }

            public string Content { get; }

            public bool Equals(SchemaText other) => Path == other.Path && Content == other.Content;

            public override bool Equals(object? obj) => obj is SchemaText other && Equals(other);

            public override int GetHashCode()
                => ((Path?.GetHashCode() ?? 0) * 397) ^ (Content?.GetHashCode() ?? 0);
        }

        /// <summary>
        /// Folds the contracts derived from each <c>[ProtoSchema]</c> into the parsed model.
        /// </summary>
        /// <remarks>
        /// Returns the input untouched when the model declares no schemas, which is what keeps a
        /// project without any of this exactly as cached as it was before.
        /// </remarks>
        private static ProtoParseResult? AddSchemas(ProtoParseResult? parsed,
            ImmutableArray<SchemaText> schemas, CancellationToken cancellationToken)
        {
            if (parsed?.Plan is not { } plan || parsed.SchemaRequests.Count == 0) return parsed;

            var diagnostics = new List<PlanDiagnostic>();
            for (int i = 0; i < parsed.Diagnostics.Count; i++) diagnostics.Add(parsed.Diagnostics[i]);

            // symbol-derived contracts win a name clash: a consumer who wrote the DTO themselves
            // means it, and the schema copy would describe a type that is not the one in play
            var contracts = new List<ProtoContractPlan>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < plan.Contracts.Count; i++)
            {
                contracts.Add(plan.Contracts[i]);
                seen.Add(plan.Contracts[i].TypeName);
            }

            var paths = schemas.Select(static s => s.Path).ToArray();
            for (int i = 0; i < parsed.SchemaRequests.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var request = parsed.SchemaRequests[i];

                switch (SchemaFileMatcher.TryMatch(request.Path, paths, out var match, out var detail))
                {
                    case SchemaFileMatcher.MatchResult.NotFound:
                        diagnostics.Add(new PlanDiagnostic(ProtoDiagnosticKind.SchemaNotFound,
                            request.Location, request.Path,
                            paths.Length == 0
                                ? "the project has no .proto files in <AdditionalFiles>"
                                : "known schemas: " + string.Join(", ", paths)));
                        continue;
                    case SchemaFileMatcher.MatchResult.Ambiguous:
                        diagnostics.Add(new PlanDiagnostic(ProtoDiagnosticKind.SchemaAmbiguous,
                            request.Location, request.Path, string.Join(", ", detail)));
                        continue;
                }

                var text = schemas.First(s => s.Path == match);
                var set = TryParse(text, schemas, out var parseError);
                if (set is null)
                {
                    diagnostics.Add(new PlanDiagnostic(ProtoDiagnosticKind.SchemaInvalid,
                        request.Location, request.Path, parseError ?? "unknown error"));
                    continue;
                }

                var built = SchemaPlanBuilder.TryBuild(set, NameNormalizer.Default,
                    plan.Namespace, plan.TypeName, out var unsupported);
                if (built is null)
                {
                    diagnostics.Add(new PlanDiagnostic(ProtoDiagnosticKind.SchemaUnsupported,
                        request.Location, request.Path, unsupported ?? "unsupported"));
                    continue;
                }

                for (int j = 0; j < built.Contracts.Count; j++)
                {
                    var contract = built.Contracts[j];
                    if (seen.Add(contract.TypeName)) contracts.Add(contract);
                }
            }

            return new ProtoParseResult(plan.WithContracts(new(contracts.ToArray())),
                new(diagnostics.ToArray()), parsed.SchemaRequests);
        }

        /// <summary>
        /// Resolves <c>import</c> against the compilation's additional files, exactly as the DTO
        /// generator does - the schema being modelled is the same one being turned into DTOs, so
        /// anything it can import, this must be able to import too.
        /// </summary>
        private sealed class SchemaTextFileSystem : IFileSystem
        {
            private readonly ImmutableArray<SchemaText> _schemas;

            public SchemaTextFileSystem(ImmutableArray<SchemaText> schemas) => _schemas = schemas;

            internal static string Normalize(string path) => path?.Replace('/', '\\') ?? "";

            private SchemaText? Find(string path)
            {
                path = Normalize(path);
                foreach (var schema in _schemas)
                {
                    if (Normalize(schema.Path) == path) return schema;
                }
                return null;
            }

            bool IFileSystem.Exists(string path) => Find(path) is not null;

            System.IO.TextReader? IFileSystem.OpenText(string path)
                => Find(path) is { } found ? new System.IO.StringReader(found.Content) : null;
        }

        /// <summary>
        /// Parses one schema, with imports resolved across the whole additional-file set.
        /// </summary>
        private static FileDescriptorSet? TryParse(SchemaText text, ImmutableArray<SchemaText> all,
            out string? error)
        {
            error = null;
            try
            {
                var set = new FileDescriptorSet { FileSystem = new SchemaTextFileSystem(all) };
                var name = System.IO.Path.GetFileName(text.Path);
                var directory = System.IO.Path.GetDirectoryName(text.Path);
                if (!string.IsNullOrEmpty(directory)) set.AddImportPath(directory);

                if (!set.Add(name, includeInOutput: true))
                {
                    error = "the schema could not be added";
                    return null;
                }
                set.Process();
                var errors = set.GetErrors();
                var first = errors.FirstOrDefault(static e => e.IsError);
                if (first is not null)
                {
                    error = first.Message;
                    return null;
                }
                return set;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
        }
    }
}
