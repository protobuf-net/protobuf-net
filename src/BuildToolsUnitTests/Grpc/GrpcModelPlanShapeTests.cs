using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ProtoBuf.BuildTools.Internal.Grpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace BuildToolsUnitTests.Grpc
{
    /// <summary>
    /// The gRPC half of the rule <see cref="Aot.ProtoModelPlanShapeTests"/> guards for the serializer
    /// generator: an incremental model must not hold on to Roslyn objects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Storing an <c>ISymbol</c>, <c>SyntaxNode</c> or <c>Compilation</c> in a cached model does two
    /// kinds of damage at once: equality becomes reference-based, so the cache never hits; and the
    /// model pins the whole compilation graph alive for as long as the driver holds it, which is a
    /// serious problem in a long-running IDE session. Both failures are silent.
    /// </para>
    /// <para>
    /// Writing this test is what found that <c>Internal/Grpc</c> was holding two Roslyn objects where
    /// <c>Internal/Aot</c> holds none: a <c>DiagnosticDescriptor</c> and a <c>Location</c>. Neither was
    /// actually harmful - descriptors are static singletons, and the location was detached at
    /// construction - but "harmless for reasons you have to reconstruct" is exactly what drifts. The
    /// model now stores a <c>GrpcDiagnosticKind</c> and a <c>PlanLocation</c>, matching the serializer
    /// generator, so this test needs no exceptions and the rule is enforced as written.
    /// </para>
    /// </remarks>
    public class GrpcModelPlanShapeTests
    {
        [Fact]
        public void ModelTypesHoldNoRoslynReferences()
        {
            var offenders = new List<string>();

            foreach (var type in typeof(GrpcModelPlan).Assembly.GetTypes())
            {
                if (type.Namespace != typeof(GrpcModelPlan).Namespace) continue;

                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (IsRoslynReference(field.FieldType))
                    {
                        offenders.Add($"{type.Name}.{field.Name} ({field.FieldType.Name})");
                    }
                }
            }

            Assert.True(offenders.Count == 0,
                "incremental model types must not hold Roslyn references (breaks caching and pins the "
                + "compilation graph): " + string.Join(", ", offenders));
        }

        /// <summary>
        /// The reflection test above can only see field <em>types</em>, so it cannot tell that the
        /// location data survives the round trip or that it compares by value. Both are asserted here.
        /// </summary>
        [Fact]
        public void DiagnosticInfoKeepsItsLocationAsComparableData()
        {
            var tree = CSharpSyntaxTree.ParseText("class C { }", path: "input.cs");
            var live = Location.Create(tree, tree.GetRoot().FullSpan);
            Assert.NotNull(live.SourceTree); // the location we start from really does root a tree

            const GrpcDiagnosticKind Kind = GrpcDiagnosticKind.NoModelNamed;
            var reported = new DiagnosticInfo(Kind, live, "arg").Location.ToLocation();

            // the location is reconstituted for reporting, and still points where it did - but at no
            // point does the stored form hold the tree
            Assert.Null(reported.SourceTree);
            Assert.Equal("input.cs", reported.GetLineSpan().Path);
            Assert.Equal(live.SourceSpan, reported.SourceSpan);

            // ...and equality is by value: two DiagnosticInfos built from separate parses of the same
            // text must compare equal, or the plan they travel on never caches
            var other = CSharpSyntaxTree.ParseText("class C { }", path: "input.cs");
            var second = new DiagnosticInfo(Kind, Location.Create(other, other.GetRoot().FullSpan), "arg");
            Assert.Equal(new DiagnosticInfo(Kind, live, "arg"), second);
        }

        private static bool IsRoslynReference(Type type)
        {
            // generic arguments matter too: ImmutableArray<ISymbol> would be just as bad
            if (type.IsGenericType && type.GetGenericArguments().Any(IsRoslynReference)) return true;

            if (type.IsValueType) return false; // TextSpan, LinePositionSpan, enums, ...

            var assembly = type.Assembly.GetName().Name;
            return assembly is not null
                && assembly.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal);
        }
    }
}
