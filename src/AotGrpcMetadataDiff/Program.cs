extern alias buildtools;

using buildtools::ProtoBuf.BuildTools.Internal.Grpc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ProtoBuf.Grpc.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ProtoBuf.AotGrpcMetadataDiff;

/// <summary>
/// The endpoint-metadata oracle: proves that what the generator would construct at compile time is
/// what <c>ServiceBinder.GetMetadata</c> produces reflectively at run time.
/// </summary>
/// <remarks>
/// <para>
/// This exists before the emit, not after, and that ordering is deliberate: endpoint metadata is how
/// authorization is enforced, and both hard parts (the four-source ordering, and the inheritance rules
/// reflection applies) fail by producing a plausible-but-wrong list. A missing <c>[Authorize]</c> is a
/// more permissive endpoint with no error anywhere, so "it looked right" is not a standard worth
/// shipping against.
/// </para>
/// <para>
/// It compares <em>live instances</em>, not rendered strings: the compile-time side is rendered to
/// source, compiled, executed, and the resulting objects compared property by property against the
/// ones reflection produced. That is the only comparison that tests the renderer and the gather
/// together, and it is the same principle <c>AotDifferential</c> applies to bytes.
/// </para>
/// </remarks>
internal static class Program
{
    private static int Main()
    {
        var fixtures = Path.Combine(AppContext.BaseDirectory, "Fixtures");
        if (!Directory.Exists(fixtures))
        {
            Console.Error.WriteLine($"fixture sources not found at {fixtures}");
            return 2;
        }

        var symbols = SymbolCompilation(fixtures, out var error);
        if (error is not null)
        {
            Console.Error.WriteLine(error);
            return 2;
        }

        var failures = 0;
        var divergences = 0;
        var compared = 0;

        foreach (var (contract, method, service) in Operations(symbols!))
        {
            var label = $"{contract.Name}.{method.Name} / {service?.Name ?? "(no implementation)"}";

            // what we would emit: main's behaviour, i.e. including #369
            var gathered = MetadataGather.Gather(contract, method, service);

            // ...and what the *pinned* package produces, which predates it. Asking the gather for the
            // old behaviour rather than filtering its output here means a wrong model of that behaviour
            // fails the comparison instead of quietly agreeing with itself.
            var expected = MetadataGather.Gather(contract, method, service,
                resolveInheritedImplementation: false);
            var delta = gathered.Count - expected.Count;

            if (!TryMaterialise(symbols!, expected, out var reconstructed, out var why))
            {
                Console.Error.WriteLine($"FAIL {label}: {why}");
                failures++;
                continue;
            }

            var actual = Runtime(contract, method, service);
            compared++;

            var mismatch = Compare(reconstructed!, actual);
            if (mismatch is not null)
            {
                Console.Error.WriteLine($"FAIL {label}: {mismatch}");
                Console.Error.WriteLine($"     reconstructed: {Describe(reconstructed!)}");
                Console.Error.WriteLine($"     reflected:     {Describe(actual)}");
                failures++;
                continue;
            }

            Console.WriteLine($"ok   {label} ({actual.Count} item(s))");
            if (delta > 0)
            {
                divergences++;
                Console.WriteLine($"     ~ diverges from the pinned package by {delta} item(s): "
                    + "implementation-method attributes for an operation declared on a [SubService] "
                    + "base. protobuf-net.Grpc#369 adds these; it is in no released package, and we "
                    + "target main deliberately.");
                // implementation-method attributes sort last, so the old list is a strict prefix of
                // the new one and the difference is simply what follows it
                foreach (var item in gathered.Skip(expected.Count))
                {
                    Console.WriteLine($"       + {item.AttributeClass?.ToDisplayString()}");
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine($"{compared} operation(s) compared, {failures} failing, "
            + $"{divergences} with an explained divergence from the pinned package.");
        return failures == 0 ? 0 : 1;
    }

    /// <summary>
    /// Compiles the fixture sources a second time, for symbols.
    /// </summary>
    /// <remarks>
    /// The harness's own assembly is excluded from the reference set on purpose: it already contains
    /// these types, so leaving it in would make every one of them ambiguous against the copy being
    /// compiled from source.
    /// </remarks>
    private static CSharpCompilation? SymbolCompilation(string fixtures, out string? error)
    {
        error = null;
        var references = References(includeSelf: false);

        var trees = Directory.GetFiles(fixtures, "*.cs", SearchOption.AllDirectories)
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path,
                options: new CSharpParseOptions(LanguageVersion.CSharp12)))
            .ToList();

        var compilation = CSharpCompilation.Create("MetadataFixtureSymbols", trees, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        var errors = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Count != 0)
        {
            error = "the fixture sources did not compile for symbols:" + Environment.NewLine
                + string.Join(Environment.NewLine, errors.Select(d => "  " + d));
            return null;
        }
        return compilation;
    }

    /// <summary>
    /// The reference set for an in-process compilation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// BuildTools is dropped, and that is the same trap the csproj takes an <c>extern alias</c> for,
    /// arriving a second time: it compiles protobuf-net.Core's sources in, so having both it and the
    /// real Core in one reference set makes every contract attribute CS0433. An alias is not available
    /// here, since these references are assembled at run time.
    /// </para>
    /// <para>
    /// The harness's own assembly is excluded when compiling the fixtures for symbols - it already
    /// contains those very types, so leaving it in would make each of them ambiguous with the copy
    /// being compiled from source - and included when compiling the rendered metadata, which has to
    /// name them.
    /// </para>
    /// </remarks>
    private static List<MetadataReference> References(bool includeSelf)
    {
        var self = Path.GetFileName(typeof(Program).Assembly.Location);
        return ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "")
            .Split(Path.PathSeparator)
            .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Where(path => !Path.GetFileName(path).StartsWith("protobuf-net.BuildTools",
                StringComparison.OrdinalIgnoreCase))
            .Where(path => includeSelf
                || !string.Equals(Path.GetFileName(path), self, StringComparison.OrdinalIgnoreCase))
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToList();
    }

    /// <summary>
    /// Every (contract, operation, implementation) triple in the fixtures, including operations
    /// inherited from a <c>[SubService]</c> base - which are exactly the interesting ones.
    /// </summary>
    private static IEnumerable<(INamedTypeSymbol Contract, IMethodSymbol Method, INamedTypeSymbol? Service)>
        Operations(CSharpCompilation compilation)
    {
        var types = compilation.GlobalNamespace.GetNamespaceMembers()
            .SelectMany(ns => ns.GetTypeMembers()).ToList();

        foreach (var contract in types.Where(t => t.TypeKind == TypeKind.Interface && Has(t, "ServiceAttribute")))
        {
            var service = types.FirstOrDefault(t => t.TypeKind == TypeKind.Class && !t.IsAbstract
                && t.AllInterfaces.Contains(contract, SymbolEqualityComparer.Default));

            // the contract's own operations, then those of any [SubService] base it inherits; the
            // method is taken from its DECLARING interface, which is what the emitted binding passes
            var declaring = new List<INamedTypeSymbol> { contract };
            declaring.AddRange(contract.AllInterfaces.Where(i => Has(i, "SubServiceAttribute")));

            foreach (var owner in declaring)
            {
                foreach (var method in owner.GetMembers().OfType<IMethodSymbol>()
                    .Where(m => m.MethodKind == MethodKind.Ordinary))
                {
                    yield return (contract, method, service);
                }
            }
        }
    }

    private static bool Has(INamedTypeSymbol type, string attributeName)
        => type.GetAttributes().Any(a => a.AttributeClass?.Name == attributeName);

    /// <summary>
    /// Renders the gathered attributes to source, compiles it, and runs it - so the comparison is
    /// between objects rather than between strings.
    /// </summary>
    private static bool TryMaterialise(CSharpCompilation symbols,
        List<AttributeData> gathered, out IList<object>? result, out string? why)
    {
        result = null;
        why = null;

        var expressions = new List<string>();
        foreach (var item in gathered)
        {
            switch (AttributeRenderer.TryRender(symbols, item, out var expression, out var reason))
            {
                case AttributeRenderKind.Rendered:
                    expressions.Add(expression!);
                    break;
                case AttributeRenderKind.Skipped:
                    break;
                default:
                    why = $"could not render {item.AttributeClass?.ToDisplayString()}: {reason}";
                    return false;
            }
        }

        var source = new StringBuilder()
            .AppendLine("public static class MetadataProbe")
            .AppendLine("{")
            .AppendLine("    public static object[] Get() => new object[]")
            .AppendLine("    {")
            .AppendLine(string.Concat(expressions.Select(e => "        " + e + "," + Environment.NewLine)))
            .AppendLine("    };")
            .AppendLine("}")
            .ToString();

        // this one *does* reference the harness assembly: the rendered expressions name the fixture's
        // own attribute types, and here there is no second copy to be ambiguous with
        var references = References(includeSelf: true);

        var compilation = CSharpCompilation.Create("MetadataProbe",
            new[] { CSharpSyntaxTree.ParseText(source, options: new CSharpParseOptions(LanguageVersion.CSharp12)) },
            references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var peStream = new MemoryStream();
        var emitted = compilation.Emit(peStream);
        if (!emitted.Success)
        {
            why = "the rendered metadata did not compile:" + Environment.NewLine
                + string.Join(Environment.NewLine, emitted.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => "  " + d))
                + Environment.NewLine + source;
            return false;
        }

        var probe = Assembly.Load(peStream.ToArray()).GetType("MetadataProbe")!;
        result = (object[])probe.GetMethod("Get")!.Invoke(null, null)!;
        return true;
    }

    /// <summary>
    /// The reflective side: the real binder, at whatever version is pinned - filtered by the same
    /// synthesised-attribute rule the renderer applies, since Roslyn never surfaces those at all.
    /// </summary>
    private static IList<object> Runtime(INamedTypeSymbol contract, IMethodSymbol method,
        INamedTypeSymbol? service)
    {
        var liveContract = Live(contract);
        var liveService = service is null ? liveContract : Live(service);
        var liveDeclaring = Live(method.ContainingType);

        var parameters = method.Parameters.Select(p => p.Type.ToDisplayString()).ToArray();
        var liveMethod = liveDeclaring.GetMethods().Single(m => m.Name == method.Name
            && m.GetParameters().Select(p => Normalise(p.ParameterType)).SequenceEqual(parameters));

        var all = ServiceBinder.Default.GetMetadata(liveMethod, liveContract, liveService);
        var kept = all.Where(item => !AttributeRenderer.IsSynthesised(item.GetType().FullName ?? ""))
            .ToList();

        // Reported rather than merely filtered. The emit site long carried the claim that this list
        // "contains compiler-synthesized attributes (NullableContextAttribute) whose types are internal
        // to the declaring assembly, so it cannot be reproduced exactly" - which was the reason
        // compile-time metadata was once closed as impossible. If this count is zero, that premise was
        // wrong on its own terms, and saying so out loud is cheaper than re-deriving it later.
        if (all.Count != kept.Count)
        {
            Console.WriteLine($"     ~ filtered {all.Count - kept.Count} compiler-synthesised item(s): "
                + string.Join(", ", all.Where(x => AttributeRenderer.IsSynthesised(x.GetType().FullName ?? ""))
                    .Select(x => x.GetType().Name).Distinct()));
        }
        return kept;
    }

    private static Type Live(INamedTypeSymbol symbol)
        => typeof(Program).Assembly.GetType(symbol.ToDisplayString())
            ?? throw new InvalidOperationException($"no live type for {symbol.ToDisplayString()}");

    /// <summary>Renders a live parameter type the way Roslyn renders the symbol, so the two compare.</summary>
    private static string Normalise(Type type)
        => type.FullName?.Replace('+', '.') ?? type.Name;

    /// <summary>
    /// Compares two metadata lists positionally - order is semantic here, since the consumer treats
    /// later as higher priority, so a set comparison would pass on a list that authorizes differently.
    /// </summary>
    private static string? Compare(IList<object> reconstructed, IList<object> reflected)
    {
        if (reconstructed.Count != reflected.Count)
        {
            return $"expected {reflected.Count} item(s), reconstructed {reconstructed.Count}";
        }

        for (int i = 0; i < reconstructed.Count; i++)
        {
            var left = reconstructed[i];
            var right = reflected[i];
            if (left.GetType() != right.GetType())
            {
                return $"item {i} is {left.GetType().Name}, expected {right.GetType().Name}";
            }

            foreach (var member in Values(left.GetType()))
            {
                var a = member.Read(left);
                var b = member.Read(right);
                if (!ValueEquals(a, b))
                {
                    return $"item {i} ({left.GetType().Name}.{member.Name}): "
                        + $"reconstructed {Format(a)}, expected {Format(b)}";
                }
            }
        }
        return null;
    }

    private readonly record struct Member(string Name, Func<object, object?> Read);

    /// <summary>
    /// The readable state of an attribute instance. Equality has to be structural: the two sides are
    /// different objects by construction, and attribute types rarely override <c>Equals</c>.
    /// </summary>
    private static IEnumerable<Member> Values(Type type)
    {
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length != 0 || !property.CanRead) continue;
            if (property.DeclaringType == typeof(Attribute)) continue; // TypeId, and nothing else useful
            var captured = property;
            yield return new Member(property.Name, instance => captured.GetValue(instance));
        }
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            var captured = field;
            yield return new Member(field.Name, instance => captured.GetValue(instance));
        }
    }

    private static bool ValueEquals(object? left, object? right)
    {
        if (left is null || right is null) return left is null && right is null;
        if (left is string || left is not IEnumerable) return Equals(left, right);

        if (right is not IEnumerable) return false;
        var a = ((IEnumerable)left).Cast<object?>().ToList();
        var b = ((IEnumerable)right).Cast<object?>().ToList();
        return a.Count == b.Count && a.Zip(b, ValueEquals).All(x => x);
    }

    private static string Describe(IList<object> items)
        => items.Count == 0 ? "(empty)" : string.Join(", ", items.Select(x => x.GetType().Name));

    private static string Format(object? value) => value switch
    {
        null => "null",
        string s => "\"" + s + "\"",
        IEnumerable sequence => "[" + string.Join(", ", sequence.Cast<object?>().Select(Format)) + "]",
        _ => value.ToString() ?? "",
    };
}
