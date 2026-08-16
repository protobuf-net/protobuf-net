using BenchmarkDotNet.Attributes;
using ProtoBuf.Meta;
using System;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Nano.Bench;

/// <summary>
/// <c>TypeModel.ThrowUnexpectedSubtype(value)</c> - the widest single call in the generated
/// writer, emitted for EVERY non-sealed reference contract, on EVERY write.
/// </summary>
/// <remarks>
/// <para>
/// It is not elided for <c>.proto</c>-generated DTOs, and cannot be: protogen emits
/// <c>partial class</c> and never <c>sealed</c> - the partial is the consumer's extension point,
/// so sealing would be both a breaking change and a defeat of the design. Structs, sealed types
/// and <c>IgnoreUnknownSubTypes</c> are the only escapes, and a schema-sourced model has none of
/// them. So the entire descriptor tree pays this on every message written.
/// </para>
/// <para>
/// THE HYPOTHESIS, which is about code sharing rather than about the arithmetic. The shipped
/// helper is generic and constrained <c>where T : class</c>, so every reference instantiation
/// shares ONE compiled body over <c>__Canon</c>. In shared code <c>typeof(T)</c> is not a JIT
/// constant - it is a generic-dictionary lookup - and that specifically defeats RyuJIT's
/// optimisation of <c>obj.GetType() == typeof(SomeConstant)</c> into a bare method-table
/// comparison. Emitted at the call site against a concrete type, the same test should fold to a
/// load and a compare with no <c>RuntimeType</c> ever materialised.
/// </para>
/// <para>
/// The counter-hypothesis is just as plausible and is why this is measured rather than argued:
/// the helper is tiny, so if the JIT INLINES it into a caller with a known instantiation it can
/// recover the exact type and fold <c>typeof(T)</c> anyway - in which case the shipped form is
/// already at the floor and there is nothing here.
/// </para>
/// <para>
/// Note both shapes reduce to the same question. In a hierarchy the emitted code is
/// <c>if (IsSubType(value)) { is-chain } else ...</c>, and <c>IsSubType</c> is the very same
/// exact-type test - so for the dominant case (the value IS the declared type) the is-chain is
/// skipped entirely and only the type test is paid. The chain only matters for genuine sub-type
/// instances, which <see cref="SubTypeShare"/> varies.
/// </para>
/// </remarks>
[MemoryDiagnoser(false)]
public class ExactTypeCheckBenchmarks
{
    // --- the shapes under test -------------------------------------------------------------
    // deliberately plain: what is being measured is the type test, not field access

    /// <summary>A contract with no hierarchy - the overwhelmingly common shape.</summary>
    public class Leaf { public int Id; }

    private const int N = 4096;
    private Leaf[] _leaves = [];

    [GlobalSetup]
    public void Setup()
    {
        _leaves = new Leaf[N];
        for (int i = 0; i < N; i++) _leaves[i] = new Leaf { Id = i };
    }

    // NoInlining, and never actually reached: the point is to keep the throw's code out of the
    // hot path exactly as a generated throw helper would, while remaining a side effect the JIT
    // may not delete - which is what stops these loops folding to nothing
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Unexpected(Type expected, Type actual)
        => throw new InvalidOperationException($"unexpected {actual} for {expected}");

    // ==========================================================================================
    // A. no hierarchy: "this must be exactly Leaf"
    // ==========================================================================================

    /// <summary>The shipped call, exactly as the generator emits it today.</summary>
    [Benchmark(Baseline = true)]
    public void Exact_Shipped()
    {
        var values = _leaves;
        for (int i = 0; i < values.Length; i++) TypeModel.ThrowUnexpectedSubtype(values[i]);
    }

    /// <summary>
    /// The proposed emitted form. Null-tolerant like the shipped helper, which returns quietly
    /// for a null rather than throwing - so the guard is part of the comparison, not a cheat.
    /// </summary>
    [Benchmark]
    public void Exact_Inline()
    {
        var values = _leaves;
        for (int i = 0; i < values.Length; i++)
        {
            var value = values[i];
            if (value is not null && value.GetType() != typeof(Leaf))
                Unexpected(typeof(Leaf), value.GetType());
        }
    }

    /// <summary>
    /// Marc's shared-helper shape: NON-generic, taking the expected type as an argument, doing
    /// the GetType() and the throw internally. The appeal is honesty - one obvious body, no
    /// generic instantiation per contract, and a call site that says exactly what it means.
    /// </summary>
    /// <remarks>
    /// The reason it might cost is that <c>expected</c> arrives as a runtime ARGUMENT rather
    /// than a <c>typeof</c> the JIT can see, so the method-table fold is only available if the
    /// helper inlines. That is precisely the thing that turned out to save the shipped generic
    /// form, so it is worth pricing rather than assuming either way.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AssertExpectedType(object value, Type expected)
    {
        if (value is not null && value.GetType() != expected)
            Unexpected(expected, value.GetType());
    }

    [Benchmark]
    public void Exact_SharedHelper()
    {
        var values = _leaves;
        for (int i = 0; i < values.Length; i++) AssertExpectedType(values[i], typeof(Leaf));
    }

    /// <summary>The same helper without AggressiveInlining, which is the realistic library shape.</summary>
    private static void AssertExpectedTypeNoInlineHint(object value, Type expected)
    {
        if (value is not null && value.GetType() != expected)
            Unexpected(expected, value.GetType());
    }

    [Benchmark]
    public void Exact_SharedHelperNoHint()
    {
        var values = _leaves;
        for (int i = 0; i < values.Length; i++) AssertExpectedTypeNoInlineHint(values[i], typeof(Leaf));
    }

    /// <summary>
    /// The same without the null guard, to price the guard separately - a write path that has
    /// already tested the member for null could emit this instead.
    /// </summary>
    [Benchmark]
    public void Exact_InlineNoNullCheck()
    {
        var values = _leaves;
        for (int i = 0; i < values.Length; i++)
        {
            var value = values[i];
            if (value.GetType() != typeof(Leaf)) Unexpected(typeof(Leaf), value.GetType());
        }
    }
}

/// <summary>
/// The same question for an inheritance hierarchy. Separate class because the exact case is the
/// one <c>.proto</c> traffic actually takes - proto has no inheritance at all - so it must be
/// runnable on its own without paying for the sub-type distribution below.
/// </summary>
[MemoryDiagnoser(false)]
public class HierarchyCheckBenchmarks
{
    public class Root { public int Id; }
    public class Mid : Root { public int Extra; }
    public class LeafA : Mid { public int More; }
    public class LeafB : Root { public int Other; }

    private const int N = 4096;
    private Root[] _roots = [];

    /// <summary>
    /// How much of the traffic is a genuine sub-type rather than the root itself.
    /// 0 is the case the shipped code short-circuits; 100 forces the is-chain every time.
    /// </summary>
    [Params(0, 25, 100)]
    public int SubTypeShare { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // fixed seed: the point is to compare strategies, so every run must see identical data
        var rand = new Random(12345);
        _roots = new Root[N];
        for (int i = 0; i < N; i++)
        {
            _roots[i] = rand.Next(100) >= SubTypeShare
                ? new Root { Id = i }
                : (rand.Next(3) switch
                {
                    0 => new Mid { Id = i },
                    1 => new LeafA { Id = i },
                    _ => new LeafB { Id = i },
                });
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Unexpected(Type expected, Type actual)
        => throw new InvalidOperationException($"unexpected {actual} for {expected}");

    // ==========================================================================================
    // a hierarchy: "this must be one of the known set", and we need to know WHICH
    // ==========================================================================================
    //
    // `is` cannot express the test on its own: it is deliberately subtype-inclusive, so
    // `value is Mid` also matches LeafA. The shipped chain relies on ORDER for correctness
    // (most-derived first) which is easy to get wrong and impossible to see locally. Every
    // alternative below tests the EXACT type instead, so order stops being load-bearing.

    /// <summary>The shipped shape: the generic pre-test, then a subtype-inclusive `is` chain.</summary>
    [Benchmark(Baseline = true)]
    public int Hierarchy_Shipped()
    {
        var values = _roots;
        int acc = 0;
        for (int i = 0; i < values.Length; i++)
        {
            var value = values[i];
            if (!TypeModel.IsSubType(value)) { acc += 1; continue; }
            // order is load-bearing here: LeafA before Mid, or LeafA is swallowed
            if (value is LeafA) acc += 2;
            else if (value is Mid) acc += 3;
            else if (value is LeafB) acc += 4;
            else { Unexpected(typeof(Root), value.GetType()); }
        }
        return acc;
    }

    /// <summary>Exact-type equality chain, short-circuiting.</summary>
    [Benchmark]
    public int Hierarchy_TypeChain()
    {
        var values = _roots;
        int acc = 0;
        for (int i = 0; i < values.Length; i++)
        {
            var value = values[i];
            var t = value.GetType();
            if (t == typeof(Root)) acc += 1;
            else if (t == typeof(LeafA)) acc += 2;
            else if (t == typeof(Mid)) acc += 3;
            else if (t == typeof(LeafB)) acc += 4;
            else { Unexpected(typeof(Root), t); }
        }
        return acc;
    }

    /// <summary>
    /// Marc's non-short-circuit idea, in the form it can actually take: the membership test is
    /// branchless (`|`), and only the classification that follows branches. Worth pricing
    /// separately because on well-predicted data a branch is nearly free and `|` is pure work.
    /// </summary>
    [Benchmark]
    public int Hierarchy_TypeChainBranchless()
    {
        var values = _roots;
        int acc = 0;
        for (int i = 0; i < values.Length; i++)
        {
            var value = values[i];
            var t = value.GetType();
            bool known = (t == typeof(Root)) | (t == typeof(LeafA))
                | (t == typeof(Mid)) | (t == typeof(LeafB));
            if (!known) Unexpected(typeof(Root), t);
            acc += t == typeof(Root) ? 1 : t == typeof(LeafA) ? 2 : t == typeof(Mid) ? 3 : 4;
        }
        return acc;
    }

    /// <summary>
    /// A switch expression carrying the `when` clause that makes `is` exact. This is the form
    /// that reads best in generated code; the question is whether the pattern machinery costs
    /// anything over the plain chain.
    /// </summary>
    [Benchmark]
    public int Hierarchy_SwitchWhen()
    {
        var values = _roots;
        int acc = 0;
        for (int i = 0; i < values.Length; i++)
        {
            var value = values[i];
            acc += value switch
            {
                LeafA v when v.GetType() == typeof(LeafA) => 2,
                Mid v when v.GetType() == typeof(Mid) => 3,
                LeafB v when v.GetType() == typeof(LeafB) => 4,
                Root v when v.GetType() == typeof(Root) => 1,
                _ => Throw(value),
            };
        }
        return acc;

        static int Throw(Root value)
        {
            Unexpected(typeof(Root), value.GetType());
            return 0;
        }
    }

    /// <summary>
    /// Scan a static table. The shape that scales to a wide hierarchy without emitting a chain
    /// per type - and the one most likely to lose, since it trades folded constants for loads.
    /// </summary>
    private static readonly Type[] _known = [typeof(Root), typeof(LeafA), typeof(Mid), typeof(LeafB)];

    [Benchmark]
    public int Hierarchy_TableScan()
    {
        var values = _roots;
        var known = _known;
        int acc = 0;
        for (int i = 0; i < values.Length; i++)
        {
            var t = values[i].GetType();
            int found = -1;
            for (int j = 0; j < known.Length; j++)
            {
                if (ReferenceEquals(known[j], t)) { found = j; break; }
            }
            if (found < 0) Unexpected(typeof(Root), t);
            acc += found + 1;
        }
        return acc;
    }
}
