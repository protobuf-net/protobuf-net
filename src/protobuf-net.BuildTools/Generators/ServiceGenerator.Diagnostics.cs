using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace ProtoBuf.BuildTools.Generators;

partial class ServiceGenerator
{
    internal partial class Diagnostics
    {
        internal static readonly DiagnosticDescriptor
            PartialTypeRequired = new("PBN3001", "Partial type required",
                "Type {0} needs to be 'partial' to allow code-generation", Category.Library, DiagnosticSeverity.Warning, true);

        // be careful moving this because of static field initialization order
        internal static readonly ImmutableArray<DiagnosticDescriptor> All = typeof(Diagnostics)
            .GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            .Select(x => x.GetValue(null)).OfType<DiagnosticDescriptor>().ToImmutableArray();

        static class Category
        {
            public const string Library = nameof(Library);
        }
    }
}
