using ProtoBuf.BuildTools.Internal.Aot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace BuildToolsUnitTests.Aot
{
    /// <summary>
    /// Guards the single most important structural rule of the incremental model: it must not hold
    /// on to Roslyn objects.
    /// </summary>
    /// <remarks>
    /// Storing an <c>ISymbol</c>, <c>Location</c>, <c>SyntaxNode</c> or similar in a cached model
    /// does two kinds of damage at once. Equality becomes reference-based, so the cache never hits;
    /// and the model pins the entire compilation graph alive for as long as the driver keeps it,
    /// which is a serious memory problem in a long-running IDE session. Both failures are silent,
    /// so this is asserted rather than left as a convention.
    ///
    /// Value types from Roslyn (<c>TextSpan</c>, <c>LinePositionSpan</c>) are fine: they are plain
    /// data and reference nothing. That is why <see cref="PlanLocation"/> stores those rather than a
    /// <c>Location</c>.
    /// </remarks>
    public class ProtoModelPlanShapeTests
    {
        [Fact]
        public void ModelTypesHoldNoRoslynReferences()
        {
            var offenders = new List<string>();

            foreach (var type in typeof(ProtoModelPlan).Assembly.GetTypes())
            {
                if (type.Namespace != typeof(ProtoModelPlan).Namespace) continue;

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

        private static bool IsRoslynReference(Type type)
        {
            // generic arguments matter too: EquatableArray<ISymbol> would be just as bad
            if (type.IsGenericType && type.GetGenericArguments().Any(IsRoslynReference)) return true;

            if (type.IsValueType) return false; // TextSpan, LinePositionSpan, enums, ...

            var assembly = type.Assembly.GetName().Name;
            return assembly is not null
                && assembly.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal);
        }
    }
}
