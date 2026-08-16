#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Linq;

namespace ProtoBuf.BuildTools.Internal.Aot
{
    /// <summary>
    /// Why a contract was left out of the model.
    /// </summary>
    internal enum ProtoDiagnosticKind
    {
        UnsupportedMember,
        UnsupportedContract,
        UnsupportedOption,
        OmittedCascade,

        /// <summary>No additional file matches a <c>[ProtoSchema]</c> path.</summary>
        SchemaNotFound,

        /// <summary>More than one additional file matches a <c>[ProtoSchema]</c> path.</summary>
        SchemaAmbiguous,

        /// <summary>A <c>[ProtoSchema]</c> file did not parse.</summary>
        SchemaInvalid,

        /// <summary>A schema shape the front-end cannot project into a plan yet.</summary>
        SchemaUnsupported,
    }

    /// <summary>
    /// An equatable stand-in for <see cref="Location"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Location"/> itself cannot live in an incremental model: it holds a reference to the
    /// syntax tree, so it never compares equal across runs and would defeat caching entirely.
    /// </remarks>
    internal readonly struct PlanLocation : IEquatable<PlanLocation>
    {
        private readonly string? _filePath;
        private readonly TextSpan _span;
        private readonly LinePositionSpan _lineSpan;

        private PlanLocation(string filePath, TextSpan span, LinePositionSpan lineSpan)
        {
            _filePath = filePath;
            _span = span;
            _lineSpan = lineSpan;
        }

        public static PlanLocation From(Location? location)
        {
            if (location is null || !location.IsInSource) return default;
            var mapped = location.GetLineSpan();
            return new PlanLocation(mapped.Path, location.SourceSpan, mapped.Span);
        }

        /// <summary>
        /// The best available location for a symbol, or none for symbols from metadata.
        /// </summary>
        public static PlanLocation From(ISymbol? symbol)
            => From(symbol?.Locations.FirstOrDefault(static x => x.IsInSource));

        public Location ToLocation()
            => _filePath is null ? Location.None : Location.Create(_filePath, _span, _lineSpan);

        public bool Equals(PlanLocation other)
            => _filePath == other._filePath && _span == other._span && _lineSpan.Equals(other._lineSpan);

        public override bool Equals(object? obj) => obj is PlanLocation other && Equals(other);

        public override int GetHashCode()
            => ((_filePath?.GetHashCode() ?? 0) * 397) ^ _span.GetHashCode() ^ _lineSpan.GetHashCode();
    }

    /// <summary>
    /// A diagnostic, in a form that can be cached alongside the model.
    /// </summary>
    internal readonly struct PlanDiagnostic : IEquatable<PlanDiagnostic>
    {
        public PlanDiagnostic(ProtoDiagnosticKind kind, PlanLocation location, params string[] args)
        {
            Kind = kind;
            Location = location;
            Args = new EquatableArray<string>(args);
        }

        public ProtoDiagnosticKind Kind { get; }

        public PlanLocation Location { get; }

        public EquatableArray<string> Args { get; }

        public object[] ToMessageArgs()
        {
            var result = new object[Args.Count];
            for (int i = 0; i < result.Length; i++) result[i] = Args[i];
            return result;
        }

        public bool Equals(PlanDiagnostic other)
            => Kind == other.Kind && Location.Equals(other.Location) && Args.Equals(other.Args);

        public override bool Equals(object? obj) => obj is PlanDiagnostic other && Equals(other);

        public override int GetHashCode()
            => ((int)Kind * 397) ^ Location.GetHashCode() ^ Args.GetHashCode();
    }

    /// <summary>
    /// The result of inspecting one <c>[ProtoModel]</c> declaration.
    /// </summary>
    /// <remarks>
    /// The plan and the diagnostics are kept apart deliberately: diagnostics carry locations, which
    /// change whenever anything above them moves, whereas the plan does not - so the (expensive)
    /// emit step stays cached across edits that only shift line numbers.
    /// </remarks>
    internal sealed class ProtoParseResult : IEquatable<ProtoParseResult>
    {
        public ProtoParseResult(ProtoModelPlan? plan, EquatableArray<PlanDiagnostic> diagnostics,
            EquatableArray<PlanSchemaRequest> schemaRequests = default)
        {
            Plan = plan;
            Diagnostics = diagnostics;
            SchemaRequests = schemaRequests;
        }

        public ProtoModelPlan? Plan { get; }

        public EquatableArray<PlanDiagnostic> Diagnostics { get; }

        /// <summary>
        /// The <c>[ProtoSchema]</c> declarations on the model, unresolved.
        /// </summary>
        /// <remarks>
        /// They are an INPUT to a later step rather than part of the plan: resolving them needs the
        /// compilation's additional files, which the syntax-driven parse does not have. Keeping
        /// them here rather than on the plan also keeps the plan describing only what is emitted.
        /// </remarks>
        public EquatableArray<PlanSchemaRequest> SchemaRequests { get; }

        public bool Equals(ProtoParseResult? other)
            => other is not null && Equals(Plan, other.Plan) && Diagnostics.Equals(other.Diagnostics)
                && SchemaRequests.Equals(other.SchemaRequests);

        public override bool Equals(object? obj) => Equals(obj as ProtoParseResult);

        public override int GetHashCode()
            => (((Plan?.GetHashCode() ?? 0) * 397) ^ Diagnostics.GetHashCode()) * 397
                ^ SchemaRequests.GetHashCode();
    }

    /// <summary>
    /// One <c>[ProtoSchema("...")]</c> declaration: the path as written, and where it was written,
    /// so that a resolution failure can point at the attribute.
    /// </summary>
    internal readonly struct PlanSchemaRequest : IEquatable<PlanSchemaRequest>
    {
        public PlanSchemaRequest(string path, PlanLocation location)
        {
            Path = path;
            Location = location;
        }

        public string Path { get; }

        public PlanLocation Location { get; }

        public bool Equals(PlanSchemaRequest other)
            => Path == other.Path && Location.Equals(other.Location);

        public override bool Equals(object? obj) => obj is PlanSchemaRequest other && Equals(other);

        public override int GetHashCode() => ((Path?.GetHashCode() ?? 0) * 397) ^ Location.GetHashCode();
    }
}
